using Ams.Application.Abstractions;
using Ams.Application.Common;
using Ams.Application.Dtos;
using Ams.Domain.Entities;
using Ams.Domain.Enums;
using Ams.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ams.Application.Services;

public class AcademicService(
    IAppDbContext db,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    ILogger<AcademicService> logger) : IAcademicService
{
    // ---------------------------------------------------------------- classes

    public async Task<PagedResult<ClassRoomDto>> ListClassRoomsAsync(
        PagedQueryRequest query, CancellationToken ct = default)
    {
        // Teachers and students need to read the class list to fill dropdowns and labels;
        // only mutation is admin-only.
        var q = db.ClassRooms.AsQueryable();

        if (query.IsActive is { } isActive)
            q = q.Where(c => c.IsActive == isActive);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            q = q.Where(c => c.Name.ToLower().Contains(term)
                             || c.NameEn.ToLower().Contains(term)
                             || c.Code.ToLower().Contains(term));
        }

        var paged = await q
            .OrderBy(c => c.Level)
            .ThenBy(c => c.Section)
            .Select(c => new
            {
                ClassRoom = c,
                StudentCount = c.Enrollments.Count,
                CourseCount = c.Courses.Count(x => x.IsActive)
            })
            .ToPagedResultAsync(query.Page, query.PageSize, ct);

        var items = paged.Items.Select(x => x.ClassRoom.ToDto(x.StudentCount, x.CourseCount)).ToList();
        return PagedResult<ClassRoomDto>.Create(items, paged.Page, paged.PageSize, paged.TotalCount);
    }

    public async Task<ClassRoomDto> CreateClassRoomAsync(
        CreateClassRoomRequest request, CancellationToken ct = default)
    {
        EnsureAdmin();

        var code = request.Code.Trim().ToUpperInvariant();
        if (await db.ClassRooms.AnyAsync(c => c.Code == code, ct))
            throw new BusinessRuleException($"'{code}' কোডে একটি শ্রেণি ইতিমধ্যে রয়েছে।");

        var entity = new ClassRoom
        {
            Name = request.Name.Trim(),
            NameEn = request.NameEn.Trim(),
            Code = code,
            Level = request.Level,
            Section = string.IsNullOrWhiteSpace(request.Section) ? null : request.Section.Trim(),
            AcademicYear = request.AcademicYear.Trim(),
            IsActive = true,
            CreatedAt = timeProvider.GetUtcNow()
        };

        db.ClassRooms.Add(entity);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Admin {AdminId} created class {ClassId}", currentUser.UserId, entity.Id);

        return entity.ToDto(0, 0);
    }

    public async Task<ClassRoomDto> UpdateClassRoomAsync(
        Guid id, UpdateClassRoomRequest request, CancellationToken ct = default)
    {
        EnsureAdmin();

        var entity = await db.ClassRooms.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException($"শ্রেণি {id} পাওয়া যায়নি।");

        var code = request.Code.Trim().ToUpperInvariant();
        if (code != entity.Code && await db.ClassRooms.AnyAsync(c => c.Code == code, ct))
            throw new BusinessRuleException($"'{code}' কোডে একটি শ্রেণি ইতিমধ্যে রয়েছে।");

        entity.Name = request.Name.Trim();
        entity.NameEn = request.NameEn.Trim();
        entity.Code = code;
        entity.Level = request.Level;
        entity.Section = string.IsNullOrWhiteSpace(request.Section) ? null : request.Section.Trim();
        entity.AcademicYear = request.AcademicYear.Trim();
        entity.IsActive = request.IsActive;

        await db.SaveChangesAsync(ct);

        var studentCount = await db.Enrollments.CountAsync(e => e.ClassRoomId == id, ct);
        var courseCount = await db.Courses.CountAsync(c => c.ClassRoomId == id && c.IsActive, ct);
        return entity.ToDto(studentCount, courseCount);
    }

    public async Task DeleteClassRoomAsync(Guid id, CancellationToken ct = default)
    {
        EnsureAdmin();

        var entity = await db.ClassRooms.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException($"শ্রেণি {id} পাওয়া যায়নি।");

        // RULE: deleting a class that has courses would cascade away routine rows and orphan
        // every assignment set on them.
        if (await db.Courses.AnyAsync(c => c.ClassRoomId == id, ct))
            throw new BusinessRuleException(
                "এই শ্রেণিতে কোর্স রয়েছে, তাই এটি মুছে ফেলা যাবে না। পরিবর্তে নিষ্ক্রিয় করুন।");

        db.ClassRooms.Remove(entity);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Admin {AdminId} deleted class {ClassId}", currentUser.UserId, id);
    }

    // --------------------------------------------------------------- subjects

    public async Task<PagedResult<SubjectDto>> ListSubjectsAsync(
        PagedQueryRequest query, CancellationToken ct = default)
    {
        var q = db.Subjects.Include(s => s.AllowedAssignmentTypes).AsQueryable();

        if (query.IsActive is { } isActive)
            q = q.Where(s => s.IsActive == isActive);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            q = q.Where(s => s.Name.ToLower().Contains(term)
                             || s.NameEn.ToLower().Contains(term)
                             || s.Code.Contains(term));
        }

        var paged = await q
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.Code)
            .ToPagedResultAsync(query.Page, query.PageSize, ct);

        var items = paged.Items.Select(s => s.ToDto()).ToList();
        return PagedResult<SubjectDto>.Create(items, paged.Page, paged.PageSize, paged.TotalCount);
    }

    public async Task<SubjectDto> CreateSubjectAsync(
        CreateSubjectRequest request, CancellationToken ct = default)
    {
        EnsureAdmin();

        var code = request.Code.Trim();
        if (await db.Subjects.AnyAsync(s => s.Code == code, ct))
            throw new BusinessRuleException($"'{code}' বিষয় কোডটি ইতিমধ্যে ব্যবহৃত হয়েছে।");

        var entity = new Subject
        {
            Name = request.Name.Trim(),
            NameEn = request.NameEn.Trim(),
            Code = code,
            TextbookName = string.IsNullOrWhiteSpace(request.TextbookName)
                ? null
                : request.TextbookName.Trim(),
            FullMarks = request.FullMarks,
            WeeklyPeriods = request.WeeklyPeriods,
            FaithGroup = request.FaithGroup,
            IsOptionalGroup = request.IsOptionalGroup,
            DisplayOrder = request.DisplayOrder,
            IsActive = true,
            CreatedAt = timeProvider.GetUtcNow()
        };

        foreach (var type in request.AllowedAssignmentTypes.Distinct())
            entity.AllowedAssignmentTypes.Add(new SubjectAssignmentType { Type = type });

        db.Subjects.Add(entity);
        await db.SaveChangesAsync(ct);

        return entity.ToDto();
    }

    public async Task<SubjectDto> UpdateSubjectAsync(
        Guid id, UpdateSubjectRequest request, CancellationToken ct = default)
    {
        EnsureAdmin();

        var entity = await db.Subjects
            .Include(s => s.AllowedAssignmentTypes)
            .FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException($"বিষয় {id} পাওয়া যায়নি।");

        var code = request.Code.Trim();
        if (code != entity.Code && await db.Subjects.AnyAsync(s => s.Code == code, ct))
            throw new BusinessRuleException($"'{code}' বিষয় কোডটি ইতিমধ্যে ব্যবহৃত হয়েছে।");

        entity.Name = request.Name.Trim();
        entity.NameEn = request.NameEn.Trim();
        entity.Code = code;
        entity.TextbookName = string.IsNullOrWhiteSpace(request.TextbookName)
            ? null
            : request.TextbookName.Trim();
        entity.FullMarks = request.FullMarks;
        entity.WeeklyPeriods = request.WeeklyPeriods;
        entity.FaithGroup = request.FaithGroup;
        entity.IsOptionalGroup = request.IsOptionalGroup;
        entity.DisplayOrder = request.DisplayOrder;
        entity.IsActive = request.IsActive;

        // RULE: an allowed type cannot be withdrawn while assignments of that type exist —
        // removing it would leave published work whose type the subject no longer permits.
        var requested = request.AllowedAssignmentTypes.Distinct().ToHashSet();
        var removed = entity.AllowedAssignmentTypes.Where(t => !requested.Contains(t.Type)).ToList();

        foreach (var gone in removed)
        {
            var inUse = await db.Assignments.AnyAsync(
                a => a.Course.SubjectId == id && a.Type == gone.Type, ct);

            if (inUse)
                throw new BusinessRuleException(
                    $"'{gone.Type}' ধরনের অ্যাসাইনমেন্ট এই বিষয়ে ইতিমধ্যে রয়েছে, তাই ধরনটি বাদ দেওয়া যাবে না।");

            entity.AllowedAssignmentTypes.Remove(gone);
        }

        foreach (var type in requested.Except(entity.AllowedAssignmentTypes.Select(t => t.Type)).ToList())
            entity.AllowedAssignmentTypes.Add(new SubjectAssignmentType { SubjectId = id, Type = type });

        await db.SaveChangesAsync(ct);
        return entity.ToDto();
    }

    public async Task DeleteSubjectAsync(Guid id, CancellationToken ct = default)
    {
        EnsureAdmin();

        var entity = await db.Subjects.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException($"বিষয় {id} পাওয়া যায়নি।");

        if (await db.Courses.AnyAsync(c => c.SubjectId == id, ct))
            throw new BusinessRuleException(
                "এই বিষয়ে কোর্স রয়েছে, তাই এটি মুছে ফেলা যাবে না। পরিবর্তে নিষ্ক্রিয় করুন।");

        db.Subjects.Remove(entity);
        await db.SaveChangesAsync(ct);
    }

    // ---------------------------------------------------------------- courses

    public async Task<PagedResult<CourseDto>> ListCoursesAsync(
        CourseListQuery query, CancellationToken ct = default)
    {
        var q = CourseQuery();

        // A teacher browsing courses sees their own; admins see everything. Students use
        // GetMyEnrolledCoursesAsync, which applies the faith-group filter.
        if (currentUser.Role == UserRole.Teacher)
            q = q.Where(c => c.TeacherId == currentUser.UserId);
        else if (currentUser.Role != UserRole.Admin)
            throw new ForbiddenException("এই তালিকা দেখার অনুমতি নেই।");

        if (query.ClassRoomId is { } classId)
            q = q.Where(c => c.ClassRoomId == classId);

        if (query.SubjectId is { } subjectId)
            q = q.Where(c => c.SubjectId == subjectId);

        if (query.TeacherId is { } teacherId && currentUser.Role == UserRole.Admin)
            q = q.Where(c => c.TeacherId == teacherId);

        if (query.Unstaffed == true)
            q = q.Where(c => c.TeacherId == null);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            q = q.Where(c => c.Code.ToLower().Contains(term)
                             || c.Subject.Name.ToLower().Contains(term)
                             || c.Subject.NameEn.ToLower().Contains(term));
        }

        var paged = await q
            .OrderBy(c => c.ClassRoom.Level)
            .ThenBy(c => c.Subject.DisplayOrder)
            .Select(c => new
            {
                Course = c,
                StudentCount = c.ClassRoom.Enrollments.Count(e =>
                    c.Subject.FaithGroup == null || e.Student.Faith == c.Subject.FaithGroup),
                AssignmentCount = c.Assignments.Count,
                PublishedCount = c.Assignments.Count(a => a.Status == AssignmentStatus.Published)
            })
            .ToPagedResultAsync(query.Page, query.PageSize, ct);

        var items = paged.Items
            .Select(x => x.Course.ToDto(x.StudentCount, x.AssignmentCount, x.PublishedCount))
            .ToList();

        return PagedResult<CourseDto>.Create(items, paged.Page, paged.PageSize, paged.TotalCount);
    }

    public async Task<CourseDto> GetCourseAsync(Guid id, CancellationToken ct = default)
    {
        var course = await CourseQuery().FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException($"কোর্স {id} পাওয়া যায়নি।");

        await EnsureCanSeeCourseAsync(course, ct);
        return await ProjectCourseAsync(course, ct);
    }

    public async Task<CourseDto> CreateCourseAsync(
        CreateCourseRequest request, CancellationToken ct = default)
    {
        EnsureAdmin();

        var classRoom = await db.ClassRooms.FirstOrDefaultAsync(c => c.Id == request.ClassRoomId, ct)
            ?? throw new NotFoundException($"শ্রেণি {request.ClassRoomId} পাওয়া যায়নি।");

        var subject = await db.Subjects.FirstOrDefaultAsync(s => s.Id == request.SubjectId, ct)
            ?? throw new NotFoundException($"বিষয় {request.SubjectId} পাওয়া যায়নি।");

        // RULE: one offering of a subject per class per year — the routine and the assignment
        // feed both assume a student has exactly one course for a given subject.
        var duplicate = await db.Courses.AnyAsync(
            c => c.ClassRoomId == request.ClassRoomId
                 && c.SubjectId == request.SubjectId
                 && c.AcademicYear == classRoom.AcademicYear, ct);

        if (duplicate)
            throw new BusinessRuleException("এই শ্রেণিতে এই বিষয়ের কোর্স ইতিমধ্যে রয়েছে।");

        if (request.TeacherId is { } teacherId)
            await EnsureIsActiveTeacherAsync(teacherId, ct);

        var entity = new Course
        {
            Code = Course.BuildCode(classRoom.Level, subject.Code),
            ClassRoomId = request.ClassRoomId,
            SubjectId = request.SubjectId,
            TeacherId = request.TeacherId,
            AcademicYear = classRoom.AcademicYear,
            IsActive = true,
            CreatedAt = timeProvider.GetUtcNow()
        };

        db.Courses.Add(entity);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Admin {AdminId} created course {CourseCode}", currentUser.UserId, entity.Code);

        return await GetCourseAsync(entity.Id, ct);
    }

    public async Task<CourseDto> AssignCourseTeacherAsync(
        Guid id, AssignCourseTeacherRequest request, CancellationToken ct = default)
    {
        EnsureAdmin();

        var course = await db.Courses.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException($"কোর্স {id} পাওয়া যায়নি।");

        if (request.TeacherId is { } teacherId)
            await EnsureIsActiveTeacherAsync(teacherId, ct);

        // RULE: a course cannot be handed over while the outgoing teacher still has work on it.
        // Their assignments would become unmanageable — they could no longer grade the
        // submissions they are responsible for, and the incoming teacher does not own them.
        if (course.TeacherId is { } outgoing && outgoing != request.TeacherId)
        {
            var hasWork = await db.Assignments.AnyAsync(
                a => a.CourseId == id && a.CreatedByTeacherId == outgoing, ct);

            if (hasWork)
                throw new BusinessRuleException(
                    "বর্তমান শিক্ষক এই কোর্সে অ্যাসাইনমেন্ট দিয়েছেন, তাই শিক্ষক পরিবর্তন করা যাবে না।");
        }

        course.TeacherId = request.TeacherId;
        course.UpdatedAt = timeProvider.GetUtcNow();

        await db.SaveChangesAsync(ct);
        logger.LogInformation(
            "Admin {AdminId} set teacher {TeacherId} on course {CourseId}",
            currentUser.UserId, request.TeacherId, id);

        return await GetCourseAsync(id, ct);
    }

    public async Task DeleteCourseAsync(Guid id, CancellationToken ct = default)
    {
        EnsureAdmin();

        var course = await db.Courses.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException($"কোর্স {id} পাওয়া যায়নি।");

        if (await db.Assignments.AnyAsync(a => a.CourseId == id, ct))
            throw new BusinessRuleException(
                "এই কোর্সে অ্যাসাইনমেন্ট রয়েছে, তাই এটি মুছে ফেলা যাবে না। পরিবর্তে নিষ্ক্রিয় করুন।");

        db.Courses.Remove(course);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<CourseDto>> GetMyCoursesAsync(CancellationToken ct = default)
    {
        if (currentUser.Role != UserRole.Teacher)
            throw new ForbiddenException("কেবল শিক্ষকদের নিজস্ব কোর্স তালিকা রয়েছে।");

        var rows = await CourseQuery()
            .Where(c => c.TeacherId == currentUser.UserId && c.IsActive)
            .OrderBy(c => c.ClassRoom.Level)
            .ThenBy(c => c.Subject.DisplayOrder)
            .Select(c => new
            {
                Course = c,
                StudentCount = c.ClassRoom.Enrollments.Count(e =>
                    c.Subject.FaithGroup == null || e.Student.Faith == c.Subject.FaithGroup),
                AssignmentCount = c.Assignments.Count,
                PublishedCount = c.Assignments.Count(a => a.Status == AssignmentStatus.Published)
            })
            .ToListAsync(ct);

        return rows
            .Select(x => x.Course.ToDto(x.StudentCount, x.AssignmentCount, x.PublishedCount))
            .ToList();
    }

    public async Task<IReadOnlyList<CourseDto>> GetMyEnrolledCoursesAsync(
        CancellationToken ct = default)
    {
        if (currentUser.Role != UserRole.Student)
            throw new ForbiddenException("কেবল শিক্ষার্থীদের ভর্তিকৃত কোর্স তালিকা রয়েছে।");

        var courses = await StudentCourseQuery(currentUser.UserId)
            .OrderBy(c => c.Subject.DisplayOrder)
            .Select(c => new
            {
                Course = c,
                StudentCount = c.ClassRoom.Enrollments.Count(e =>
                    c.Subject.FaithGroup == null || e.Student.Faith == c.Subject.FaithGroup),
                PublishedCount = c.Assignments.Count(a => a.Status == AssignmentStatus.Published)
            })
            .ToListAsync(ct);

        // A student is shown published counts only — a draft must not even leak as a number.
        return courses
            .Select(x => x.Course.ToDto(x.StudentCount, x.PublishedCount, x.PublishedCount))
            .ToList();
    }

    // ------------------------------------------------------------ enrollments

    public async Task<PagedResult<EnrollmentDto>> ListEnrollmentsAsync(
        EnrollmentListQuery query, CancellationToken ct = default)
    {
        EnsureAdmin();

        var q = db.Enrollments
            .Include(e => e.Student)
            .Include(e => e.ClassRoom)
            .AsQueryable();

        if (query.ClassRoomId is { } classId)
            q = q.Where(e => e.ClassRoomId == classId);

        if (query.StudentId is { } studentId)
            q = q.Where(e => e.StudentId == studentId);

        var paged = await q
            .OrderBy(e => e.ClassRoom.Level)
            .ThenBy(e => e.RollNumber)
            .ToPagedResultAsync(query.Page, query.PageSize, ct);

        var items = paged.Items.Select(e => e.ToDto()).ToList();
        return PagedResult<EnrollmentDto>.Create(items, paged.Page, paged.PageSize, paged.TotalCount);
    }

    public async Task<EnrollmentDto> CreateEnrollmentAsync(
        CreateEnrollmentRequest request, CancellationToken ct = default)
    {
        EnsureAdmin();

        var student = await db.Users.FirstOrDefaultAsync(u => u.Id == request.StudentId, ct)
            ?? throw new NotFoundException($"ব্যবহারকারী {request.StudentId} পাওয়া যায়নি।");

        // RULE: only student accounts can be enrolled in a class.
        if (student.Role != UserRole.Student)
            throw new ValidationFailedException("কেবল শিক্ষার্থীকে শ্রেণিতে ভর্তি করা যায়।");

        // RULE: a student must declare a faith group before enrolment, because ধর্ম ও নৈতিক
        // শিক্ষা is compulsory and the system cannot decide which of the four they take.
        if (student.Faith is null)
            throw new ValidationFailedException(
                "শিক্ষার্থীর ধর্ম নির্ধারণ না করে শ্রেণিতে ভর্তি করা যাবে না।");

        if (!await db.ClassRooms.AnyAsync(c => c.Id == request.ClassRoomId, ct))
            throw new NotFoundException($"শ্রেণি {request.ClassRoomId} পাওয়া যায়নি।");

        if (request.RollNumber <= 0)
            throw new ValidationFailedException("রোল নম্বর অবশ্যই ধনাত্মক হতে হবে।");

        if (await db.Enrollments.AnyAsync(
                e => e.StudentId == request.StudentId && e.ClassRoomId == request.ClassRoomId, ct))
            throw new BusinessRuleException("এই শিক্ষার্থী ইতিমধ্যে এই শ্রেণিতে ভর্তি আছে।");

        if (await db.Enrollments.AnyAsync(
                e => e.ClassRoomId == request.ClassRoomId && e.RollNumber == request.RollNumber, ct))
            throw new BusinessRuleException($"এই শ্রেণিতে রোল নম্বর {request.RollNumber} ইতিমধ্যে ব্যবহৃত।");

        var entity = new Enrollment
        {
            StudentId = request.StudentId,
            ClassRoomId = request.ClassRoomId,
            RollNumber = request.RollNumber,
            CreatedAt = timeProvider.GetUtcNow()
        };

        db.Enrollments.Add(entity);
        await db.SaveChangesAsync(ct);

        var created = await db.Enrollments
            .Include(e => e.Student)
            .Include(e => e.ClassRoom)
            .FirstAsync(e => e.Id == entity.Id, ct);

        return created.ToDto();
    }

    public async Task DeleteEnrollmentAsync(Guid id, CancellationToken ct = default)
    {
        EnsureAdmin();

        var entity = await db.Enrollments.FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new NotFoundException($"ভর্তি {id} পাওয়া যায়নি।");

        // RULE: un-enrolling a student who has already submitted work for that class would
        // strip them of access to their own graded history.
        var hasSubmissions = await db.Submissions.AnyAsync(
            s => s.StudentId == entity.StudentId
                 && s.Assignment.Course.ClassRoomId == entity.ClassRoomId, ct);

        if (hasSubmissions)
            throw new BusinessRuleException(
                "এই শিক্ষার্থী এই শ্রেণিতে কাজ জমা দিয়েছে, তাই ভর্তি বাতিল করা যাবে না।");

        db.Enrollments.Remove(entity);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<EnrollmentDto>> GetClassRosterAsync(
        Guid classRoomId, CancellationToken ct = default)
    {
        // A teacher may read the roster of any class they teach; admins read any.
        if (currentUser.Role == UserRole.Teacher)
        {
            var teaches = await db.Courses.AnyAsync(
                c => c.ClassRoomId == classRoomId && c.TeacherId == currentUser.UserId, ct);

            if (!teaches)
                throw new ForbiddenException("আপনি এই শ্রেণিতে পাঠদান করেন না।");
        }
        else if (currentUser.Role != UserRole.Admin)
        {
            throw new ForbiddenException("এই তালিকা দেখার অনুমতি নেই।");
        }

        var roster = await db.Enrollments
            .Include(e => e.Student)
            .Include(e => e.ClassRoom)
            .Where(e => e.ClassRoomId == classRoomId)
            .OrderBy(e => e.RollNumber)
            .ToListAsync(ct);

        return roster.Select(e => e.ToDto()).ToList();
    }

    // ---------------------------------------------------------------- routine

    public async Task<ClassRoutineDto> GetClassRoutineAsync(
        Guid classRoomId, CancellationToken ct = default)
    {
        var classRoom = await db.ClassRooms.FirstOrDefaultAsync(c => c.Id == classRoomId, ct)
            ?? throw new NotFoundException($"শ্রেণি {classRoomId} পাওয়া যায়নি।");

        // A student may only read their own class's routine; teachers and admins read any.
        if (currentUser.Role == UserRole.Student)
        {
            var enrolled = await db.Enrollments.AnyAsync(
                e => e.StudentId == currentUser.UserId && e.ClassRoomId == classRoomId, ct);

            if (!enrolled)
                throw new ForbiddenException("আপনি এই শ্রেণির শিক্ষার্থী নন।");
        }

        var periods = await db.RoutinePeriods
            .Include(r => r.Course).ThenInclude(c => c.Subject)
            .Include(r => r.Course).ThenInclude(c => c.Teacher)
            .Where(r => r.ClassRoomId == classRoomId)
            .ToListAsync(ct);

        var byDay = periods.ToLookup(r => r.Day);

        var days = Enum.GetValues<WeekDay>()
            .Select(day => new RoutineDayDto(
                day,
                BanglaNames.Day(day),
                Enumerable.Range(1, PeriodSchedule.PeriodsPerDay)
                    .Select(index =>
                    {
                        var (start, end) = PeriodSchedule.SlotFor(index);

                        var entries = byDay[day]
                            .Where(r => r.PeriodIndex == index)
                            .OrderBy(r => r.Course.Subject.DisplayOrder)
                            .Select(r => new RoutineEntryDto(
                                r.CourseId,
                                r.Course.Code,
                                r.Course.Subject.Name,
                                r.Course.Teacher?.FullName,
                                r.Course.Subject.FaithGroup))
                            .ToList();

                        return new RoutineSlotDto(
                            index, start.ToString("HH:mm"), end.ToString("HH:mm"), entries);
                    })
                    .ToList()))
            .ToList();

        return new ClassRoutineDto(
            classRoom.Id,
            classRoom.Name,
            classRoom.AcademicYear,
            PeriodSchedule.AssemblyStart.ToString("HH:mm"),
            PeriodSchedule.AssemblyEnd.ToString("HH:mm"),
            PeriodSchedule.BreakAfterPeriod,
            PeriodSchedule.BreakStart.ToString("HH:mm"),
            PeriodSchedule.BreakEnd.ToString("HH:mm"),
            days);
    }

    public async Task<IReadOnlyList<TeacherRoutineSlotDto>> GetMyTeachingRoutineAsync(
        CancellationToken ct = default)
    {
        if (currentUser.Role != UserRole.Teacher)
            throw new ForbiddenException("কেবল শিক্ষকদের পাঠদান রুটিন রয়েছে।");

        var periods = await db.RoutinePeriods
            .Include(r => r.Course).ThenInclude(c => c.Subject)
            .Include(r => r.Course).ThenInclude(c => c.ClassRoom)
            .Where(r => r.Course.TeacherId == currentUser.UserId)
            .OrderBy(r => r.Day)
            .ThenBy(r => r.PeriodIndex)
            .ToListAsync(ct);

        return periods.Select(r =>
        {
            var (start, end) = PeriodSchedule.SlotFor(r.PeriodIndex);
            return new TeacherRoutineSlotDto(
                r.Day,
                r.PeriodIndex,
                start.ToString("HH:mm"),
                end.ToString("HH:mm"),
                r.CourseId,
                r.Course.Code,
                r.Course.ClassRoom.Name,
                r.Course.Subject.Name);
        }).ToList();
    }

    public async Task<ClassRoutineDto> SetRoutinePeriodAsync(
        SetRoutinePeriodRequest request, CancellationToken ct = default)
    {
        EnsureAdmin();

        if (!PeriodSchedule.IsValidPeriod(request.PeriodIndex))
            throw new ValidationFailedException(
                $"পিরিয়ড ১ থেকে {PeriodSchedule.PeriodsPerDay} এর মধ্যে হতে হবে।");

        var course = await db.Courses
            .Include(c => c.Subject)
            .FirstOrDefaultAsync(c => c.Id == request.CourseId, ct)
            ?? throw new NotFoundException($"কোর্স {request.CourseId} পাওয়া যায়নি।");

        // RULE: a routine slot for class 6 can only hold a course that belongs to class 6.
        if (course.ClassRoomId != request.ClassRoomId)
            throw new ValidationFailedException("কোর্সটি এই শ্রেণির নয়।");

        // RULE: a teacher cannot be in two classrooms in the same period. The unique index
        // stops a class being double-booked; this stops a *teacher* being double-booked, which
        // no index can express because it spans classes.
        if (course.TeacherId is { } teacherId)
        {
            var clash = await db.RoutinePeriods.AnyAsync(
                r => r.Day == request.Day
                     && r.PeriodIndex == request.PeriodIndex
                     && r.ClassRoomId != request.ClassRoomId
                     && r.Course.TeacherId == teacherId, ct);

            if (clash)
                throw new BusinessRuleException(
                    "এই শিক্ষকের ঐ সময়ে অন্য শ্রেণিতে ক্লাস রয়েছে।");
        }

        var occupants = await db.RoutinePeriods
            .Include(r => r.Course).ThenInclude(c => c.Subject)
            .Where(r => r.ClassRoomId == request.ClassRoomId
                        && r.Day == request.Day
                        && r.PeriodIndex == request.PeriodIndex)
            .ToListAsync(ct);

        // Already there — nothing to do.
        if (occupants.Any(r => r.CourseId == request.CourseId))
            return await GetClassRoutineAsync(request.ClassRoomId, ct);

        // RULE: a slot holds one course, except that faith-group courses run in parallel — the
        // Muslim and Hindu groups of a class are taught ধর্ম ও নৈতিক শিক্ষা at the same time.
        // Anything else replaces what was there, which is what an admin means by "set this
        // period to maths".
        var runsInParallel = course.Subject.FaithGroup is not null
                             && occupants.Count > 0
                             && occupants.All(r =>
                                 r.Course.Subject.FaithGroup is not null
                                 && r.Course.Subject.FaithGroup != course.Subject.FaithGroup);

        if (occupants.Count > 0 && !runsInParallel)
            db.RoutinePeriods.RemoveRange(occupants);

        db.RoutinePeriods.Add(new RoutinePeriod
        {
            ClassRoomId = request.ClassRoomId,
            CourseId = request.CourseId,
            Day = request.Day,
            PeriodIndex = request.PeriodIndex,
            CreatedAt = timeProvider.GetUtcNow()
        });

        await db.SaveChangesAsync(ct);
        return await GetClassRoutineAsync(request.ClassRoomId, ct);
    }

    public async Task<ClassRoutineDto> ClearRoutinePeriodAsync(
        Guid classRoomId, WeekDay day, int periodIndex, CancellationToken ct = default)
    {
        EnsureAdmin();

        // Clears the whole slot, including both halves of a parallel religion period.
        var existing = await db.RoutinePeriods
            .Where(r => r.ClassRoomId == classRoomId && r.Day == day && r.PeriodIndex == periodIndex)
            .ToListAsync(ct);

        if (existing.Count > 0)
        {
            db.RoutinePeriods.RemoveRange(existing);
            await db.SaveChangesAsync(ct);
        }

        return await GetClassRoutineAsync(classRoomId, ct);
    }

    // ---------------------------------------------------------------- helpers

    private IQueryable<Course> CourseQuery() =>
        db.Courses
            .Include(c => c.ClassRoom)
            .Include(c => c.Subject).ThenInclude(s => s.AllowedAssignmentTypes)
            .Include(c => c.Teacher)
            .AsQueryable();

    /// <summary>
    /// The courses a student actually takes: every active course of a class they are enrolled
    /// in, minus religion courses for a faith that is not theirs.
    /// </summary>
    private IQueryable<Course> StudentCourseQuery(Guid studentId) =>
        CourseQuery().Where(c =>
            c.IsActive
            && c.ClassRoom.Enrollments.Any(e => e.StudentId == studentId)
            && (c.Subject.FaithGroup == null
                || c.ClassRoom.Enrollments.Any(e =>
                    e.StudentId == studentId && e.Student.Faith == c.Subject.FaithGroup)));

    private async Task<CourseDto> ProjectCourseAsync(Course course, CancellationToken ct)
    {
        var studentCount = await db.Enrollments.CountAsync(
            e => e.ClassRoomId == course.ClassRoomId
                 && (course.Subject.FaithGroup == null || e.Student.Faith == course.Subject.FaithGroup),
            ct);

        var assignmentCount = await db.Assignments.CountAsync(a => a.CourseId == course.Id, ct);
        var publishedCount = await db.Assignments.CountAsync(
            a => a.CourseId == course.Id && a.Status == AssignmentStatus.Published, ct);

        return course.ToDto(studentCount, assignmentCount, publishedCount);
    }

    private async Task EnsureCanSeeCourseAsync(Course course, CancellationToken ct)
    {
        switch (currentUser.Role)
        {
            case UserRole.Admin:
                return;

            case UserRole.Teacher when course.IsTaughtBy(currentUser.UserId):
                return;

            case UserRole.Student:
                var takes = await StudentCourseQuery(currentUser.UserId)
                    .AnyAsync(c => c.Id == course.Id, ct);

                if (takes) return;
                break;
        }

        // Reported as missing rather than forbidden, so the response does not confirm that a
        // course the caller has no business seeing exists.
        throw new NotFoundException($"কোর্স {course.Id} পাওয়া যায়নি।");
    }

    private async Task EnsureIsActiveTeacherAsync(Guid teacherId, CancellationToken ct)
    {
        var teacher = await db.Users.FirstOrDefaultAsync(u => u.Id == teacherId, ct)
            ?? throw new NotFoundException($"ব্যবহারকারী {teacherId} পাওয়া যায়নি।");

        if (teacher.Role != UserRole.Teacher)
            throw new ValidationFailedException("কেবল শিক্ষক অ্যাকাউন্টকে কোর্সে নিয়োগ দেওয়া যায়।");

        if (!teacher.IsActive)
            throw new ValidationFailedException("নিষ্ক্রিয় শিক্ষককে কোর্সে নিয়োগ দেওয়া যাবে না।");
    }

    private void EnsureAdmin()
    {
        if (currentUser.Role != UserRole.Admin)
            throw new ForbiddenException("কেবল প্রশাসক এই কাজটি করতে পারেন।");
    }
}
