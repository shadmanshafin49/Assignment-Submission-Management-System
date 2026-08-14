using Ams.Application.Services;
using Ams.Domain.Entities;
using Ams.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ams.UnitTests.Infrastructure;

/// <summary>
/// A small but real slice of the school, populated into the test database so each test can state
/// only the thing it is actually about.
///
/// <code>
///   ষষ্ঠ শ্রেণি  ── C06-109 গণিত                ── রেজাউল     রোল ১ সাদমান (ইসলাম)
///                ── C06-101 বাংলা ১ম পত্র        ── আফসার      রোল ২ তানভীর (ইসলাম)
///                ── C06-111 ইসলাম ও নৈতিক শিক্ষা ── মুকিম       রোল ৩ অর্ণব   (হিন্দু)
///                ── C06-112 হিন্দুধর্ম ও নৈতিক শিক্ষা ── পূর্ণিমা
///   সপ্তম শ্রেণি ── C07-109 গণিত                ── রেজাউল     রোল ১ ইশতিয়াক (ইসলাম)
/// </code>
///
/// আফসার deliberately does not teach maths, which is what the "a teacher may only set work on
/// their own course" tests turn on. অর্ণব is the one Hindu student, which is what the
/// faith-group visibility tests turn on.
/// </summary>
public sealed class TestWorld
{
    private readonly TestContext _ctx;

    public User Admin { get; }

    /// <summary>মোঃ রেজাউল করিম — teaches maths to both classes.</summary>
    public User Rejaul { get; }

    /// <summary>গাজী মোঃ আফসার উদ্দিন — teaches Bangla only; holds no maths course.</summary>
    public User Afsar { get; }

    /// <summary>মোঃ মুকিম বিল্লাহ — ইসলাম ও নৈতিক শিক্ষা.</summary>
    public User Mukim { get; }

    /// <summary>পূর্ণিমা সরকার — হিন্দুধর্ম ও নৈতিক শিক্ষা.</summary>
    public User Purnima { get; }

    public User Sadman { get; }
    public User Tanvir { get; }

    /// <summary>The one Hindu student in class 6.</summary>
    public User Arnab { get; }

    /// <summary>A class 7 student — used to check cross-class isolation.</summary>
    public User Ishtiaq { get; }

    public ClassRoom ClassSix { get; }
    public ClassRoom ClassSeven { get; }

    public Subject Math { get; }
    public Subject Bangla { get; }
    public Subject IslamStudies { get; }
    public Subject HinduStudies { get; }

    public Course SixMath { get; }
    public Course SixBangla { get; }
    public Course SixIslam { get; }
    public Course SixHindu { get; }
    public Course SevenMath { get; }

    public FakeFileStorage Storage { get; } = new();

    public TestWorld(TestContext ctx)
    {
        _ctx = ctx;
        var now = TestContext.Now;

        Admin = NewUser("প্রধান শিক্ষক", "admin@gcbhs.edu.bd", UserRole.Admin, now);
        Rejaul = NewUser("মোঃ রেজাউল করিম", "rejaul@gcbhs.edu.bd", UserRole.Teacher, now);
        Afsar = NewUser("গাজী মোঃ আফসার উদ্দিন", "afsar@gcbhs.edu.bd", UserRole.Teacher, now);
        Mukim = NewUser("মোঃ মুকিম বিল্লাহ", "mukim@gcbhs.edu.bd", UserRole.Teacher, now);
        Purnima = NewUser("পূর্ণিমা সরকার", "purnima@gcbhs.edu.bd", UserRole.Teacher, now);

        Sadman = NewUser("মোঃ সাদমান সাকিব", "c6r01@student.gcbhs.edu.bd",
            UserRole.Student, now, FaithGroup.Islam);
        Tanvir = NewUser("মোঃ তানভীর হাসান", "c6r02@student.gcbhs.edu.bd",
            UserRole.Student, now, FaithGroup.Islam);
        Arnab = NewUser("অর্ণব চক্রবর্তী", "c6r03@student.gcbhs.edu.bd",
            UserRole.Student, now, FaithGroup.Hindu);
        Ishtiaq = NewUser("মোঃ ইশতিয়াক আহমেদ", "c7r01@student.gcbhs.edu.bd",
            UserRole.Student, now, FaithGroup.Islam);

        ClassSix = NewClass("ষষ্ঠ শ্রেণি", "Class 6", "C06", 6, now);
        ClassSeven = NewClass("সপ্তম শ্রেণি", "Class 7", "C07", 7, now);

        Math = NewSubject("গণিত", "Mathematics", "109", 100, 5, null, now,
            AssignmentType.MathProblem, AssignmentType.CreativeQuestion,
            AssignmentType.ShortAnswer, AssignmentType.MultipleChoice);

        Bangla = NewSubject("বাংলা ১ম পত্র", "Bangla 1st Paper", "101", 100, 3, null, now,
            AssignmentType.CreativeQuestion, AssignmentType.MultipleChoice);

        IslamStudies = NewSubject("ইসলাম ও নৈতিক শিক্ষা", "Islam and Moral Education", "111",
            100, 3, FaithGroup.Islam, now,
            AssignmentType.CreativeQuestion, AssignmentType.ShortAnswer);

        HinduStudies = NewSubject("হিন্দুধর্ম ও নৈতিক শিক্ষা", "Hindu Religion and Moral Education",
            "112", 100, 3, FaithGroup.Hindu, now,
            AssignmentType.CreativeQuestion, AssignmentType.ShortAnswer);

        ctx.Db.Users.AddRange(Admin, Rejaul, Afsar, Mukim, Purnima, Sadman, Tanvir, Arnab, Ishtiaq);
        ctx.Db.ClassRooms.AddRange(ClassSix, ClassSeven);
        ctx.Db.Subjects.AddRange(Math, Bangla, IslamStudies, HinduStudies);

        SixMath = NewCourse(ClassSix, Math, Rejaul, now);
        SixBangla = NewCourse(ClassSix, Bangla, Afsar, now);
        SixIslam = NewCourse(ClassSix, IslamStudies, Mukim, now);
        SixHindu = NewCourse(ClassSix, HinduStudies, Purnima, now);
        SevenMath = NewCourse(ClassSeven, Math, Rejaul, now);

        ctx.Db.Courses.AddRange(SixMath, SixBangla, SixIslam, SixHindu, SevenMath);

        ctx.Db.Enrollments.AddRange(
            new Enrollment { Student = Sadman, ClassRoom = ClassSix, RollNumber = 1, CreatedAt = now },
            new Enrollment { Student = Tanvir, ClassRoom = ClassSix, RollNumber = 2, CreatedAt = now },
            new Enrollment { Student = Arnab, ClassRoom = ClassSix, RollNumber = 3, CreatedAt = now },
            new Enrollment { Student = Ishtiaq, ClassRoom = ClassSeven, RollNumber = 1, CreatedAt = now });

        ctx.Db.SaveChanges();
        ctx.ClearTracking();
    }

    // ------------------------------------------------------------- entity helpers

    /// <summary>Adds an assignment directly, bypassing the service, to set up a scenario.</summary>
    public Assignment GivenAssignment(
        AssignmentStatus status = AssignmentStatus.Published,
        Course? course = null,
        User? teacher = null,
        DateTimeOffset? deadline = null,
        int maxMarks = 100,
        AssignmentType type = AssignmentType.MathProblem,
        bool allowLate = false,
        bool allowResubmission = true,
        bool allowComments = true)
    {
        var target = course ?? SixMath;

        var assignment = new Assignment
        {
            Title = "অনুশীলনী সমাধান",
            Description = "নির্ধারিত সমস্যাগুলোর সমাধান করো।",
            CourseId = target.Id,
            Type = type,
            ChapterOrLesson = "প্রথম অধ্যায়",
            CreatedByTeacherId = (teacher ?? Rejaul).Id,
            WeekNumber = 11,
            AssignedOn = TestContext.Now.AddDays(-2),
            Deadline = deadline ?? TestContext.Now.AddDays(5),
            MaxMarks = maxMarks,
            Status = status,
            AllowLateSubmission = allowLate,
            AllowResubmission = allowResubmission,
            AllowComments = allowComments,
            PublishedAt = status == AssignmentStatus.Published ? TestContext.Now.AddDays(-1) : null,
            CreatedAt = TestContext.Now.AddDays(-2)
        };

        _ctx.Db.Assignments.Add(assignment);
        _ctx.Db.SaveChanges();
        _ctx.ClearTracking();

        return assignment;
    }

    public Submission GivenSubmission(
        Assignment assignment,
        User student,
        SubmissionStatus status = SubmissionStatus.Submitted,
        int? marks = null,
        string answer = "আমার উত্তর।")
    {
        var submission = new Submission
        {
            AssignmentId = assignment.Id,
            StudentId = student.Id,
            AnswerText = answer,
            Status = status,
            SubmittedAt = TestContext.Now.AddHours(-1),
            Marks = marks,
            GradedByTeacherId = marks is null ? null : assignment.CreatedByTeacherId,
            GradedAt = marks is null ? null : TestContext.Now.AddMinutes(-30)
        };

        _ctx.Db.Submissions.Add(submission);
        _ctx.Db.SaveChanges();
        _ctx.ClearTracking();

        return submission;
    }

    public SubmissionAttachment GivenSubmissionAttachment(Submission submission)
    {
        var attachment = new SubmissionAttachment
        {
            SubmissionId = submission.Id,
            FileName = "answer.pdf",
            StorageKey = $"submissions/{Guid.NewGuid():N}.pdf",
            ContentType = "application/pdf",
            SizeBytes = 2048,
            UploadedById = submission.StudentId,
            UploadedAt = TestContext.Now.AddHours(-1)
        };

        _ctx.Db.SubmissionAttachments.Add(attachment);
        _ctx.Db.SaveChanges();
        _ctx.ClearTracking();

        return attachment;
    }

    // ------------------------------------------------------------ service factories

    public AssignmentService AssignmentsAs(User user) =>
        new(_ctx.Context,
            TestContext.As(user.Id, user.Role, user.Email),
            SettingsAs(user),
            Storage,
            _ctx.Clock,
            NullLogger<AssignmentService>.Instance);

    public SubmissionService SubmissionsAs(User user) =>
        new(_ctx.Context,
            TestContext.As(user.Id, user.Role, user.Email),
            SettingsAs(user),
            Storage,
            _ctx.Clock,
            NullLogger<SubmissionService>.Instance);

    public UserService UsersAs(User user) =>
        new(_ctx.Context,
            TestContext.As(user.Id, user.Role, user.Email),
            new Ams.Infrastructure.Security.PasswordHasher(),
            _ctx.Clock,
            NullLogger<UserService>.Instance);

    public AcademicService AcademicAs(User user) =>
        new(_ctx.Context,
            TestContext.As(user.Id, user.Role, user.Email),
            _ctx.Clock,
            NullLogger<AcademicService>.Instance);

    public SettingsService SettingsAs(User user) =>
        new(_ctx.Context,
            TestContext.As(user.Id, user.Role, user.Email),
            _ctx.Clock,
            NullLogger<SettingsService>.Instance);

    // ------------------------------------------------------------------ factories

    private static User NewUser(
        string name, string email, UserRole role, DateTimeOffset now, FaithGroup? faith = null)
        => new()
        {
            FullName = name,
            FullNameEn = email.Split('@')[0],
            Email = email,
            // The hash is irrelevant to these tests; the auth tests set their own.
            PasswordHash = "test-hash",
            Role = role,
            Faith = faith,
            IsActive = true,
            CreatedAt = now
        };

    private static ClassRoom NewClass(
        string name, string nameEn, string code, int level, DateTimeOffset now)
        => new()
        {
            Name = name,
            NameEn = nameEn,
            Code = code,
            Level = level,
            AcademicYear = "2026",
            IsActive = true,
            CreatedAt = now
        };

    private static Subject NewSubject(
        string name, string nameEn, string code, int fullMarks, int weeklyPeriods,
        FaithGroup? faith, DateTimeOffset now, params AssignmentType[] allowedTypes)
    {
        var subject = new Subject
        {
            Name = name,
            NameEn = nameEn,
            Code = code,
            FullMarks = fullMarks,
            WeeklyPeriods = weeklyPeriods,
            FaithGroup = faith,
            IsActive = true,
            CreatedAt = now
        };

        foreach (var type in allowedTypes)
            subject.AllowedAssignmentTypes.Add(new SubjectAssignmentType { Type = type });

        return subject;
    }

    private static Course NewCourse(
        ClassRoom classRoom, Subject subject, User teacher, DateTimeOffset now)
        => new()
        {
            Code = Course.BuildCode(classRoom.Level, subject.Code),
            ClassRoom = classRoom,
            Subject = subject,
            Teacher = teacher,
            AcademicYear = "2026",
            IsActive = true,
            CreatedAt = now
        };
}
