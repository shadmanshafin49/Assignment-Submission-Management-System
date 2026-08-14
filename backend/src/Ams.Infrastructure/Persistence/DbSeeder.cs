using System.Globalization;
using System.Text;
using Ams.Application.Abstractions;
using Ams.Domain.Entities;
using Ams.Domain.Enums;
using Ams.Infrastructure.Persistence.SeedData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ams.Infrastructure.Persistence;

/// <summary>
/// Builds one school year of Gazipur Cantonment Board High School so an evaluator can sign in
/// and see populated screens immediately.
/// <para>
/// The data is real rather than illustrative: NCTB subjects with their board codes and weekly
/// period counts, the 2026 textbooks and their chapters, assignment types NCTB actually
/// prescribes, and a 36-period routine that is solved rather than typed. What it produces is
/// three classes of thirty boys, thirteen teachers, 42 courses, a conflict-free weekly routine,
/// roughly a hundred assignments and the submissions against them.
/// </para>
/// <para>
/// Idempotent: it no-ops when users already exist, so restarting the API does not duplicate
/// anything. Every random choice comes from a fixed seed, so the same code produces the same
/// database every time.
/// </para>
/// </summary>
public class DbSeeder(
    AppDbContext db,
    IPasswordHasher passwordHasher,
    IFileStorage storage,
    TimeProvider timeProvider,
    ILogger<DbSeeder> logger)
{
    private const string SchoolDomain = "gcbhs.edu.bd";
    private const string StudentDomain = "student.gcbhs.edu.bd";

    /// <summary>
    /// Fixed so the seeded database is reproducible — the same marks, the same students who
    /// forgot to submit, run after run. A demo that shuffles on every boot is impossible to
    /// write documentation against.
    /// </summary>
    private readonly Random _rng = new(20260813);

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await db.Users.AnyAsync(ct))
        {
            logger.LogInformation("Database already seeded — skipping");
            return;
        }

        logger.LogInformation("Seeding Gazipur Cantonment Board High School...");
        var now = timeProvider.GetUtcNow();

        // The whole seed is one transaction, because "has this been seeded?" is answered by
        // looking for users — the very first thing written. Without this, a failure partway
        // through (an unwritable upload directory, say) commits the users and leaves the
        // assignments behind, and every restart afterwards reports "already seeded" over a
        // database that is missing half its contents. Rolling back keeps that check honest.
        //
        // Files written to storage are not covered: a rollback can orphan a worksheet or two,
        // which is harmless, since the next attempt writes fresh keys.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        SeedSettings(now);
        var subjects = SeedSubjects(now);
        var classes = SeedClasses(now);
        var admin = SeedAdmin(now);
        var teachers = SeedTeachers(now);
        var students = SeedStudents(classes, now);

        await db.SaveChangesAsync(ct);

        var courses = SeedCourses(classes, subjects, teachers, now);
        await db.SaveChangesAsync(ct);

        SeedRoutine(classes, subjects, courses, now);
        await db.SaveChangesAsync(ct);

        var assignments = await SeedAssignmentsAsync(classes, subjects, courses, now, ct);
        await db.SaveChangesAsync(ct);

        SeedSubmissions(assignments, students, now);
        SeedComments(assignments, students, now);
        await db.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);

        logger.LogInformation(
            "Seeded {Teachers} teachers, {Students} students, {Courses} courses, "
            + "{Assignments} assignments",
            teachers.Count, students.Values.Sum(s => s.Count), courses.Count, assignments.Count);

        _ = admin;
    }

    // --------------------------------------------------------------- settings

    private void SeedSettings(DateTimeOffset now)
    {
        void Add(string key, string value, string description, string type, string category,
            int order, bool editable = true)
            => db.AppSettings.Add(new AppSetting
            {
                Key = key,
                Value = value,
                Description = description,
                ValueType = type,
                Category = category,
                IsEditable = editable,
                DisplayOrder = order,
                UpdatedAt = now
            });

        const string institution = "প্রতিষ্ঠান";
        const string academic = "শিক্ষাবর্ষ ও রুটিন";
        const string assignment = "অ্যাসাইনমেন্টের ডিফল্ট";
        const string uploads = "ফাইল আপলোড";

        Add(AppSettingKeys.SchoolName, "গাজীপুর ক্যান্টনমেন্ট বোর্ড উচ্চ বিদ্যালয়",
            "প্রতিষ্ঠানের নাম", "string", institution, 1);
        Add(AppSettingKeys.SchoolNameEn, "Gazipur Cantonment Board High School",
            "প্রতিষ্ঠানের নাম (ইংরেজি)", "string", institution, 2);
        Add(AppSettingKeys.SchoolEiin, "108957",
            "ইআইআইএন নম্বর", "string", institution, 3, editable: false);
        Add(AppSettingKeys.SchoolAddress, "বিওএফ, গাজীপুর ক্যান্টনমেন্ট, গাজীপুর",
            "ঠিকানা", "string", institution, 4);
        Add(AppSettingKeys.AcademicYear, Curriculum.AcademicYear,
            "চলতি শিক্ষাবর্ষ", "string", academic, 5);

        Add(AppSettingKeys.TeachingDaysPerWeek,
            PeriodSchedule.TeachingDaysPerWeek.ToString(CultureInfo.InvariantCulture),
            "সপ্তাহে ক্লাসের দিন (শনি–বৃহস্পতি)", "int", academic, 6, editable: false);
        Add(AppSettingKeys.PeriodsPerDay,
            PeriodSchedule.PeriodsPerDay.ToString(CultureInfo.InvariantCulture),
            "প্রতিদিন পিরিয়ড সংখ্যা", "int", academic, 7, editable: false);

        Add(AppSettingKeys.DefaultDeadlineDays, "7",
            "অ্যাসাইনমেন্ট দেওয়ার কত দিন পর সময়সীমা", "int", assignment, 8);
        Add(AppSettingKeys.DefaultMaxMarks, "10",
            "নতুন অ্যাসাইনমেন্টের ডিফল্ট পূর্ণ নম্বর", "int", assignment, 9);
        Add(AppSettingKeys.DefaultAllowLateSubmission, "false",
            "ডিফল্টভাবে বিলম্বে জমা গ্রহণ করা হবে কি না", "bool", assignment, 10);
        Add(AppSettingKeys.DefaultAllowResubmission, "true",
            "ডিফল্টভাবে জমা সম্পাদনা করা যাবে কি না", "bool", assignment, 11);
        Add(AppSettingKeys.DefaultAllowComments, "true",
            "ডিফল্টভাবে শ্রেণি মন্তব্য চালু থাকবে কি না", "bool", assignment, 12);
        Add(AppSettingKeys.MaxAttachmentsPerAssignment, "5",
            "একটি অ্যাসাইনমেন্টে সর্বোচ্চ সংযুক্তি", "int", assignment, 13);

        Add(AppSettingKeys.MaxAttachmentsPerSubmission, "3",
            "একটি জমায় সর্বোচ্চ সংযুক্তি", "int", uploads, 14);
        Add(AppSettingKeys.MaxUploadSizeMb, "10",
            "সর্বোচ্চ ফাইল সাইজ (মেগাবাইট)", "int", uploads, 15);
        Add(AppSettingKeys.AllowedUploadExtensions, "pdf,doc,docx,txt,jpg,jpeg,png",
            "অনুমোদিত ফাইলের ধরন", "string", uploads, 16);
    }

    // -------------------------------------------------------- academic structure

    private Dictionary<string, Subject> SeedSubjects(DateTimeOffset now)
    {
        var result = new Dictionary<string, Subject>();

        foreach (var seed in Curriculum.Subjects)
        {
            var subject = new Subject
            {
                Name = seed.Name,
                NameEn = seed.NameEn,
                Code = seed.Code,
                // Class 6, 7 and 8 use different books for বাংলা ১ম পত্র; everything else shares
                // a title across the three, so the class-6 entry is the representative one.
                TextbookName = seed.TextbookByClass.GetValueOrDefault(6),
                FullMarks = seed.FullMarks,
                WeeklyPeriods = seed.WeeklyPeriods,
                FaithGroup = seed.FaithGroup,
                IsOptionalGroup = seed.IsOptionalGroup,
                DisplayOrder = seed.DisplayOrder,
                IsActive = true,
                CreatedAt = now
            };

            foreach (var type in seed.AllowedTypes)
                subject.AllowedAssignmentTypes.Add(new SubjectAssignmentType { Type = type });

            db.Subjects.Add(subject);
            result[seed.Code] = subject;
        }

        return result;
    }

    private Dictionary<int, ClassRoom> SeedClasses(DateTimeOffset now)
    {
        var names = new Dictionary<int, (string Bn, string En)>
        {
            [6] = ("ষষ্ঠ শ্রেণি", "Class 6"),
            [7] = ("সপ্তম শ্রেণি", "Class 7"),
            [8] = ("অষ্টম শ্রেণি", "Class 8")
        };

        var result = new Dictionary<int, ClassRoom>();

        foreach (var (level, (bn, en)) in names)
        {
            var classRoom = new ClassRoom
            {
                Name = bn,
                NameEn = en,
                Code = $"C{level:D2}",
                Level = level,
                Section = null,
                AcademicYear = Curriculum.AcademicYear,
                IsActive = true,
                CreatedAt = now
            };

            db.ClassRooms.Add(classRoom);
            result[level] = classRoom;
        }

        return result;
    }

    // ------------------------------------------------------------------ people

    private User SeedAdmin(DateTimeOffset now)
    {
        var admin = new User
        {
            FullName = "মোঃ শাহাদাত হোসেন",
            FullNameEn = "Md Shahadat Hossain",
            Email = $"admin@{SchoolDomain}",
            PasswordHash = passwordHasher.Hash(People.DefaultAdminPassword),
            Role = UserRole.Admin,
            Designation = "প্রধান শিক্ষক",
            IsActive = true,
            CreatedAt = now
        };

        db.Users.Add(admin);
        return admin;
    }

    private Dictionary<string, User> SeedTeachers(DateTimeOffset now)
    {
        var result = new Dictionary<string, User>();

        foreach (var seed in People.Teachers)
        {
            var teacher = new User
            {
                FullName = seed.Name,
                FullNameEn = seed.NameEn,
                Email = seed.Email,
                PasswordHash = passwordHasher.Hash(People.DefaultTeacherPassword),
                Role = UserRole.Teacher,
                Designation = seed.Designation,
                IsActive = true,
                CreatedAt = now
            };

            db.Users.Add(teacher);
            result[seed.Key] = teacher;
        }

        return result;
    }

    /// <summary>
    /// Thirty students per class with roll numbers 1–30, enrolled as they are created. Logins
    /// are roll-based (<c>c6r01@student.gcbhs.edu.bd</c>) so any student in the school can be
    /// signed in as without looking anything up.
    /// </summary>
    private Dictionary<int, List<User>> SeedStudents(
        Dictionary<int, ClassRoom> classes, DateTimeOffset now)
    {
        var result = new Dictionary<int, List<User>>();

        foreach (var (level, classRoom) in classes)
        {
            var roster = new List<User>();
            var roll = 1;

            foreach (var (name, nameEn, faith) in People.StudentsFor(level))
            {
                var student = new User
                {
                    FullName = name,
                    FullNameEn = nameEn,
                    Email = $"c{level}r{roll:D2}@{StudentDomain}",
                    PasswordHash = passwordHasher.Hash(People.DefaultStudentPassword),
                    Role = UserRole.Student,
                    Faith = faith,
                    IsActive = true,
                    CreatedAt = now
                };

                db.Users.Add(student);
                db.Enrollments.Add(new Enrollment
                {
                    Student = student,
                    ClassRoom = classRoom,
                    RollNumber = roll,
                    CreatedAt = now
                });

                roster.Add(student);
                roll++;
            }

            result[level] = roster;
        }

        return result;
    }

    // ----------------------------------------------------------------- courses

    private Dictionary<(int Level, string SubjectCode), Course> SeedCourses(
        Dictionary<int, ClassRoom> classes,
        Dictionary<string, Subject> subjects,
        Dictionary<string, User> teachers,
        DateTimeOffset now)
    {
        var result = new Dictionary<(int, string), Course>();

        foreach (var staffing in People.Staffing)
        {
            var classRoom = classes[staffing.Level];
            var subject = subjects[staffing.SubjectCode];

            var course = new Course
            {
                Code = Course.BuildCode(staffing.Level, subject.Code),
                ClassRoom = classRoom,
                Subject = subject,
                Teacher = teachers[staffing.TeacherKey],
                AcademicYear = Curriculum.AcademicYear,
                IsActive = true,
                CreatedAt = now
            };

            db.Courses.Add(course);
            result[(staffing.Level, staffing.SubjectCode)] = course;
        }

        return result;
    }

    // ----------------------------------------------------------------- routine

    private void SeedRoutine(
        Dictionary<int, ClassRoom> classes,
        Dictionary<string, Subject> subjects,
        Dictionary<(int Level, string SubjectCode), Course> courses,
        DateTimeOffset now)
    {
        var slots = courses
            .Select(kv => new RoutineBuilder.CourseSlot(
                kv.Value.Id,
                classes[kv.Key.Level].Id,
                kv.Key.SubjectCode,
                kv.Value.Teacher?.Id,
                subjects[kv.Key.SubjectCode].WeeklyPeriods,
                subjects[kv.Key.SubjectCode].FaithGroup))
            .ToList();

        foreach (var placement in RoutineBuilder.Build(slots))
        {
            db.RoutinePeriods.Add(new RoutinePeriod
            {
                ClassRoomId = placement.ClassRoomId,
                CourseId = placement.CourseId,
                Day = placement.Day,
                PeriodIndex = placement.PeriodIndex,
                CreatedAt = now
            });
        }
    }

    // ------------------------------------------------------------- assignments

    /// <summary>
    /// Two published assignments per course — last week's, whose deadline has passed and which
    /// has submissions to mark, and this week's, which is still open — plus a draft on every
    /// third course so the draft/published split is visible without hunting for it.
    /// </summary>
    private async Task<List<Assignment>> SeedAssignmentsAsync(
        Dictionary<int, ClassRoom> classes,
        Dictionary<string, Subject> subjects,
        Dictionary<(int Level, string SubjectCode), Course> courses,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var assignments = new List<Assignment>();
        var index = 0;

        foreach (var ((level, subjectCode), course) in courses.OrderBy(c => c.Key.Level)
                     .ThenBy(c => subjects[c.Key.SubjectCode].DisplayOrder))
        {
            var templates = AssignmentTemplates.For(subjectCode, level);
            if (templates.Count == 0)
                continue;

            var chapters = Curriculum.Chapters.GetValueOrDefault((level, subjectCode), []);
            if (chapters.Length == 0)
                continue;

            // Last week's work: set nine days ago, due two days ago.
            var previous = BuildAssignment(
                course, templates[index % templates.Count],
                chapters[index % chapters.Length],
                assignedOn: now.AddDays(-9),
                publish: true,
                now);

            // This week's work: set two days ago, due in five.
            var current = BuildAssignment(
                course, templates[(index + 1) % templates.Count],
                chapters[(index + 1) % chapters.Length],
                assignedOn: now.AddDays(-2),
                publish: true,
                now);

            assignments.Add(previous);
            assignments.Add(current);

            if (index % 3 == 0)
            {
                var draft = BuildAssignment(
                    course, templates[(index + 2) % templates.Count],
                    chapters[(index + 2) % chapters.Length],
                    assignedOn: now,
                    publish: false,
                    now);

                assignments.Add(draft);
            }

            index++;
        }

        db.Assignments.AddRange(assignments);
        await AttachWorksheetsAsync(assignments, ct);

        _ = classes;
        return assignments;
    }

    private Assignment BuildAssignment(
        Course course,
        AssignmentTemplates.Template template,
        string chapter,
        DateTimeOffset assignedOn,
        bool publish,
        DateTimeOffset now)
    {
        // Every assignment is due exactly one week after it is set — the school's standard
        // window, and the default the API applies when a teacher does not override it.
        var deadline = assignedOn.AddDays(7);

        return new Assignment
        {
            Title = template.Title.Replace("{chapter}", ShortChapter(chapter)),
            Description = template.Description.Replace("{chapter}", chapter),
            Course = course,
            Type = template.Type,
            ChapterOrLesson = chapter,
            CreatedByTeacherId = course.Teacher!.Id,
            WeekNumber = ISOWeek.GetWeekOfYear(assignedOn.UtcDateTime),
            AssignedOn = assignedOn,
            Deadline = deadline,
            MaxMarks = template.MaxMarks,
            Status = publish ? AssignmentStatus.Published : AssignmentStatus.Draft,
            PublishedAt = publish ? assignedOn : null,

            // A handful of assignments accept late work, so both branches of the deadline rule
            // are visible in the demo rather than only the strict one.
            AllowLateSubmission = template.Type is AssignmentType.Project
                or AssignmentType.PracticalWork or AssignmentType.Drawing,
            AllowResubmission = true,
            AllowComments = true,
            CreatedAt = assignedOn
        };
    }

    /// <summary>
    /// Writes a small worksheet next to the assignments whose text refers to "সংযুক্ত ফাইল", so
    /// the attachment feature has something real behind it rather than an empty list.
    /// </summary>
    private async Task AttachWorksheetsAsync(
        IReadOnlyList<Assignment> assignments, CancellationToken ct)
    {
        foreach (var assignment in assignments.Where(a => a.Description.Contains("সংযুক্ত")))
        {
            var body = new StringBuilder()
                .AppendLine("গাজীপুর ক্যান্টনমেন্ট বোর্ড উচ্চ বিদ্যালয়")
                .AppendLine($"বিষয়: {assignment.Course.Subject.Name}")
                .AppendLine($"কোর্স কোড: {assignment.Course.Code}")
                .AppendLine($"অধ্যায়: {assignment.ChapterOrLesson}")
                .AppendLine($"পূর্ণমান: {assignment.MaxMarks}")
                .AppendLine()
                .AppendLine(assignment.Title)
                .AppendLine(new string('-', 40))
                .AppendLine(assignment.Description)
                .ToString();

            var bytes = Encoding.UTF8.GetBytes(body);
            await using var stream = new MemoryStream(bytes);

            var storageKey = await storage.SaveAsync(
                "assignments",
                new FileUpload("worksheet.txt", "text/plain; charset=utf-8", bytes.Length, stream),
                ct);

            assignment.Attachments.Add(new AssignmentAttachment
            {
                FileName = $"{assignment.Course.Code}-worksheet.txt",
                StorageKey = storageKey,
                ContentType = "text/plain; charset=utf-8",
                SizeBytes = bytes.Length,
                UploadedById = assignment.CreatedByTeacherId,
                UploadedAt = assignment.AssignedOn
            });
        }
    }

    // ------------------------------------------------------------- submissions

    /// <summary>
    /// Fills in student work so every teacher has something to mark and every student has a
    /// mixed history. Roughly four in five students answer last week's work and most of that is
    /// graded; a couple submit late where the assignment allows it, and one piece per class is
    /// returned for revision so that path is visible too.
    /// </summary>
    private void SeedSubmissions(
        IReadOnlyList<Assignment> assignments,
        Dictionary<int, List<User>> students,
        DateTimeOffset now)
    {
        var returnedForRevisionByClass = new HashSet<int>();

        foreach (var assignment in assignments.Where(a => a.Status == AssignmentStatus.Published))
        {
            var level = assignment.Course.ClassRoom.Level;
            var faith = assignment.Course.Subject.FaithGroup;

            // Only the students who actually take this course — a Hindu student never appears
            // in the Islam course's submission list.
            var cohort = students[level]
                .Where(s => faith is null || s.Faith == faith)
                .ToList();

            var isPast = assignment.Deadline < now;
            var submitRate = isPast ? 0.8 : 0.3;

            foreach (var student in cohort)
            {
                if (_rng.NextDouble() > submitRate)
                    continue;

                var submittedAt = isPast
                    ? assignment.AssignedOn.AddDays(1 + _rng.Next(0, 6)).AddHours(_rng.Next(8, 21))
                    : assignment.AssignedOn.AddHours(_rng.Next(4, 40));

                var isLate = submittedAt > assignment.Deadline;

                var submission = new Submission
                {
                    Assignment = assignment,
                    Student = student,
                    AnswerText = AnswerFor(assignment, student),
                    Status = isLate ? SubmissionStatus.Late : SubmissionStatus.Submitted,
                    SubmittedAt = submittedAt
                };

                // Past work is mostly marked; current work is not marked yet.
                if (isPast && _rng.NextDouble() < 0.75)
                {
                    // Marks cluster in the upper half, as they do in a real class, but the whole
                    // range is exercised so the grading UI has both ends of it.
                    var marks = Math.Clamp(
                        (int)Math.Round(assignment.MaxMarks * (0.45 + _rng.NextDouble() * 0.55)),
                        0, assignment.MaxMarks);

                    submission.Marks = marks;
                    submission.Feedback = FeedbackFor(marks, assignment.MaxMarks);
                    submission.Status = SubmissionStatus.Graded;
                    submission.GradedByTeacherId = assignment.CreatedByTeacherId;
                    submission.GradedAt = assignment.Deadline.AddDays(1);
                    submission.UpdatedAt = submission.GradedAt;
                }
                else if (isPast && returnedForRevisionByClass.Add(level))
                {
                    // One per class, so a student can see work that has been reopened for them.
                    submission.Status = SubmissionStatus.ReturnedForRevision;
                    submission.Feedback =
                        "উত্তরটি অসম্পূর্ণ — ‘গ’ ও ‘ঘ’ অংশ বাদ পড়েছে। সম্পূর্ণ করে আবার জমা দাও।";
                    submission.UpdatedAt = assignment.Deadline.AddDays(1);
                }

                db.Submissions.Add(submission);
            }
        }
    }

    private string AnswerFor(Assignment assignment, User student)
    {
        // Short, plausible answers keyed to the kind of work. Realistic length matters less than
        // the answer visibly belonging to its assignment type.
        var openings = assignment.Type switch
        {
            AssignmentType.MultipleChoice =>
            [
                "১-খ, ২-ক, ৩-গ, ৪-খ, ৫-ঘ, ৬-ক, ৭-গ, ৮-খ, ৯-ক, ১০-ঘ",
                "১-ক, ২-গ, ৩-খ, ৪-ঘ, ৫-ক, ৬-খ, ৭-ঘ, ৮-গ, ৯-খ, ১০-ক"
            ],
            AssignmentType.MathProblem => new[]
            {
                "সমাধান খাতায় করে ছবি তুলে সংযুক্ত করেছি। ১ থেকে ১০ নম্বর সবগুলো করেছি, "
                + "৭ নম্বরে একটু সন্দেহ আছে স্যার।",
                "সবগুলো সমস্যার সমাধান ধাপে ধাপে দেখিয়েছি। শেষ দুইটি সমস্যা বাড়িতে "
                + "আবার মিলিয়ে দেখেছি।"
            },
            AssignmentType.Drawing or AssignmentType.PracticalWork => new[]
            {
                "কাজটি সম্পন্ন করে ছবি সংযুক্ত করেছি। ব্যবহৃত উপকরণ: রং পেন্সিল, এ-৪ কাগজ, আঠা।",
                "নির্দেশনা অনুযায়ী কাজটি করেছি এবং প্রতিটি ধাপের ছবি তুলে রেখেছি।"
            },
            AssignmentType.ReadingTest or AssignmentType.WritingTest or AssignmentType.Grammar =>
            [
                "I have completed all the parts. My answers for the reading section are written "
                + "in full sentences as instructed.",
                "Sir, I have answered all the questions. For the last part I have written a "
                + "paragraph of about 150 words."
            ],
            _ =>
            [
                "প্রশ্নের ক, খ, গ ও ঘ — চারটি অংশেরই উত্তর লিখেছি। পাঠ্যবই ও শ্রেণিতে "
                + "আলোচিত বিষয়গুলো অনুসরণ করেছি।",
                "উত্তরগুলো নিজের ভাষায় লিখেছি এবং প্রয়োজনীয় জায়গায় উদাহরণ দিয়েছি।"
            ]
        };

        return openings[_rng.Next(openings.Length)] + $"\n\n— {student.FullName}";
    }

    private static string FeedbackFor(int marks, int maxMarks)
    {
        var ratio = maxMarks == 0 ? 0 : (double)marks / maxMarks;

        return ratio switch
        {
            >= 0.9 => "চমৎকার হয়েছে। উপস্থাপনা ও হাতের লেখা দুটোই ভালো।",
            >= 0.75 => "ভালো হয়েছে। উত্তরে আরও উদাহরণ যোগ করলে পূর্ণ নম্বর পেতে।",
            >= 0.6 => "মোটামুটি হয়েছে। ‘ঘ’ অংশে ব্যাখ্যা আরও বিস্তারিত হওয়া দরকার।",
            >= 0.4 => "আরও মনোযোগ দিতে হবে। পাঠটি আবার পড়ে অনুশীলন করো।",
            _ => "উত্তর অসম্পূর্ণ। শ্রেণিতে আলোচনার পর আবার চেষ্টা করো।"
        };
    }

    // ---------------------------------------------------------------- comments

    /// <summary>
    /// A few class threads, so the comment feature is visibly a conversation rather than an
    /// empty box: a student asks, the teacher answers everyone at once.
    /// </summary>
    private void SeedComments(
        IReadOnlyList<Assignment> assignments,
        Dictionary<int, List<User>> students,
        DateTimeOffset now)
    {
        var conversations = new[]
        {
            ("স্যার, উত্তর কি খাতায় লিখে ছবি তুলে দিলে হবে?",
                "হ্যাঁ, খাতায় লিখে স্পষ্ট ছবি তুলে আপলোড করলেই হবে। লেখা যেন পড়া যায় সেদিকে খেয়াল রেখো।"),
            ("সংযুক্ত ফাইলটি খুলতে পারছি না, স্যার।",
                "ফাইলটি আবার আপলোড করে দিয়েছি। এখন দেখো, খুলতে পারবে।"),
            ("স্যার, সময়সীমা কি বাড়ানো যাবে?",
                "না, নির্ধারিত সময়েই জমা দিতে হবে। সমস্যা হলে আগে থেকে জানিও।"),
            ("অনুশীলনীর কোন প্রশ্নগুলো করতে হবে স্যার?",
                "সংযুক্ত ফাইলে যেগুলোর নম্বর দেওয়া আছে, শুধু সেগুলোই করবে।")
        };

        var targets = assignments
            .Where(a => a.Status == AssignmentStatus.Published)
            .Where((_, i) => i % 7 == 0)
            .Take(conversations.Length * 3)
            .ToList();

        for (var i = 0; i < targets.Count; i++)
        {
            var assignment = targets[i];
            var (question, answer) = conversations[i % conversations.Length];

            var level = assignment.Course.ClassRoom.Level;
            var faith = assignment.Course.Subject.FaithGroup;

            var asker = students[level].FirstOrDefault(s => faith is null || s.Faith == faith);
            if (asker is null)
                continue;

            var askedAt = assignment.AssignedOn.AddHours(6 + i);

            db.AssignmentComments.Add(new AssignmentComment
            {
                Assignment = assignment,
                Author = asker,
                Body = question,
                CreatedAt = askedAt
            });

            db.AssignmentComments.Add(new AssignmentComment
            {
                Assignment = assignment,
                AuthorId = assignment.CreatedByTeacherId,
                Body = answer,
                CreatedAt = askedAt.AddHours(2)
            });
        }

        _ = now;
    }

    /// <summary>Trims a chapter label down to what fits an assignment title.</summary>
    private static string ShortChapter(string chapter)
    {
        var withoutAuthor = chapter.Split('—')[0].Trim();
        var withoutPrefix = withoutAuthor.Contains(':')
            ? withoutAuthor[(withoutAuthor.IndexOf(':') + 1)..].Trim()
            : withoutAuthor;

        return withoutPrefix.Length == 0 ? chapter : withoutPrefix;
    }
}
