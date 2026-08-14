using System.Globalization;
using Ams.Application.Abstractions;
using Ams.Application.Common;
using Ams.Application.Dtos;
using Ams.Domain.Entities;
using Ams.Domain.Enums;
using Ams.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ams.Application.Services;

public class AssignmentService(
    IAppDbContext db,
    ICurrentUser currentUser,
    ISettingsService settings,
    IFileStorage storage,
    TimeProvider timeProvider,
    ILogger<AssignmentService> logger) : IAssignmentService
{
    // ---------------------------------------------------------------- queries

    public async Task<PagedResult<AssignmentDto>> ListAsync(
        AssignmentListQuery query, CancellationToken ct = default)
    {
        var q = BaseQuery();

        // A teacher only ever sees work on their own courses; an admin sees everything.
        if (currentUser.Role == UserRole.Teacher)
            q = q.Where(a => a.Course.TeacherId == currentUser.UserId);
        else if (currentUser.Role != UserRole.Admin)
            throw new ForbiddenException("এখানে অ্যাসাইনমেন্ট দেখার অনুমতি কেবল শিক্ষক ও প্রশাসকের।");

        if (query.CourseId is { } courseId)
            q = q.Where(a => a.CourseId == courseId);

        if (query.ClassRoomId is { } classId)
            q = q.Where(a => a.Course.ClassRoomId == classId);

        if (query.SubjectId is { } subjectId)
            q = q.Where(a => a.Course.SubjectId == subjectId);

        if (query.Status is { } status)
            q = q.Where(a => a.Status == status);

        if (query.Type is { } type)
            q = q.Where(a => a.Type == type);

        if (query.WeekNumber is { } week)
            q = q.Where(a => a.WeekNumber == week);

        if (query.TeacherId is { } teacherId && currentUser.Role == UserRole.Admin)
            q = q.Where(a => a.CreatedByTeacherId == teacherId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            q = q.Where(a => a.Title.ToLower().Contains(term)
                             || a.Description.ToLower().Contains(term)
                             || a.Course.Code.ToLower().Contains(term));
        }

        var paged = await q
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new
            {
                Assignment = a,
                SubmissionCount = a.Submissions.Count,
                GradedCount = a.Submissions.Count(s => s.Status == SubmissionStatus.Graded),
                // How many students the work was actually set for — the denominator a teacher
                // needs to see "18 of 30 submitted". Religion courses have a smaller cohort.
                Expected = a.Course.ClassRoom.Enrollments.Count(e =>
                    a.Course.Subject.FaithGroup == null
                    || e.Student.Faith == a.Course.Subject.FaithGroup),
                CommentCount = a.Comments.Count(c => c.DeletedAt == null)
            })
            .ToPagedResultAsync(query.Page, query.PageSize, ct);

        var items = paged.Items
            .Select(x => x.Assignment.ToDto(
                x.SubmissionCount, x.GradedCount, x.Expected, x.CommentCount))
            .ToList();

        return PagedResult<AssignmentDto>.Create(items, paged.Page, paged.PageSize, paged.TotalCount);
    }

    public async Task<AssignmentDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var assignment = await BaseQuery()
            .Include(a => a.Submissions)
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new NotFoundException($"অ্যাসাইনমেন্ট {id} পাওয়া যায়নি।");

        EnsureCanManage(assignment);

        var expected = await ExpectedSubmissionCountAsync(assignment, ct);
        var commentCount = await db.AssignmentComments
            .CountAsync(c => c.AssignmentId == id && c.DeletedAt == null, ct);

        return assignment.ToDto(
            assignment.Submissions.Count,
            assignment.Submissions.Count(s => s.Status == SubmissionStatus.Graded),
            expected,
            commentCount);
    }

    // --------------------------------------------------------------- commands

    public async Task<AssignmentDto> CreateAsync(
        CreateAssignmentRequest request, CancellationToken ct = default)
    {
        if (currentUser.Role != UserRole.Teacher)
            throw new ForbiddenException("কেবল শিক্ষক অ্যাসাইনমেন্ট তৈরি করতে পারেন।");

        var now = timeProvider.GetUtcNow();

        var course = await db.Courses
            .Include(c => c.Subject).ThenInclude(s => s.AllowedAssignmentTypes)
            .FirstOrDefaultAsync(c => c.Id == request.CourseId, ct)
            ?? throw new NotFoundException($"কোর্স {request.CourseId} পাওয়া যায়নি।");

        // RULE: a teacher may only set work on a course an admin put them on.
        EnsureTeaches(course);

        if (!course.IsActive)
            throw new BusinessRuleException("নিষ্ক্রিয় কোর্সে অ্যাসাইনমেন্ট দেওয়া যাবে না।");

        // RULE: the work must be a kind this subject actually sets.
        EnsureTypeAllowed(course, request.Type);

        // Teachers set work weekly; the default deadline is the school's standard window.
        var deadline = request.Deadline
            ?? now.AddDays(await settings.GetIntAsync(AppSettingKeys.DefaultDeadlineDays, 7, ct));

        ValidateMaxMarks(request.MaxMarks);

        var assignment = new Assignment
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            CourseId = course.Id,
            Type = request.Type,
            ChapterOrLesson = string.IsNullOrWhiteSpace(request.ChapterOrLesson)
                ? null
                : request.ChapterOrLesson.Trim(),
            CreatedByTeacherId = currentUser.UserId,
            WeekNumber = IsoWeekOf(now),
            AssignedOn = now,
            Deadline = deadline,
            MaxMarks = request.MaxMarks,
            Status = AssignmentStatus.Draft,
            AllowLateSubmission = request.AllowLateSubmission
                ?? await settings.GetBoolAsync(AppSettingKeys.DefaultAllowLateSubmission, false, ct),
            AllowResubmission = request.AllowResubmission
                ?? await settings.GetBoolAsync(AppSettingKeys.DefaultAllowResubmission, true, ct),
            AllowComments = request.AllowComments
                ?? await settings.GetBoolAsync(AppSettingKeys.DefaultAllowComments, true, ct),
            CreatedAt = now
        };

        db.Assignments.Add(assignment);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Teacher {TeacherId} created assignment {AssignmentId} on course {CourseCode}",
            currentUser.UserId, assignment.Id, course.Code);

        return await GetByIdAsync(assignment.Id, ct);
    }

    public async Task<AssignmentDto> UpdateAsync(
        Guid id, UpdateAssignmentRequest request, CancellationToken ct = default)
    {
        var assignment = await db.Assignments
            .Include(a => a.Course).ThenInclude(c => c.Subject).ThenInclude(s => s.AllowedAssignmentTypes)
            .Include(a => a.Submissions)
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new NotFoundException($"অ্যাসাইনমেন্ট {id} পাওয়া যায়নি।");

        EnsureCanManage(assignment);

        var now = timeProvider.GetUtcNow();
        var hasSubmissions = assignment.Submissions.Count > 0;
        var isRetargeting = assignment.CourseId != request.CourseId;

        // RULE: moving work to another course after students have answered would silently
        // orphan their submissions, so it is blocked once any submission exists.
        if (isRetargeting && hasSubmissions)
            throw new BusinessRuleException(
                "শিক্ষার্থীরা কাজ জমা দেওয়ার পর অ্যাসাইনমেন্টের কোর্স পরিবর্তন করা যাবে না।");

        var course = assignment.Course;

        if (isRetargeting)
        {
            course = await db.Courses
                .Include(c => c.Subject).ThenInclude(s => s.AllowedAssignmentTypes)
                .FirstOrDefaultAsync(c => c.Id == request.CourseId, ct)
                ?? throw new NotFoundException($"কোর্স {request.CourseId} পাওয়া যায়নি।");

            EnsureTeaches(course);
        }

        EnsureTypeAllowed(course, request.Type);

        // A published assignment must always point at a future deadline; a draft may be parked
        // with any deadline until it is published.
        if (assignment.IsPublished)
            ValidateDeadline(request.Deadline, now);

        ValidateMaxMarks(request.MaxMarks);

        // RULE: lowering the ceiling below marks already awarded would invalidate them.
        if (hasSubmissions && request.MaxMarks < assignment.MaxMarks)
        {
            var highestAwarded = assignment.Submissions.Max(s => s.Marks ?? 0);
            if (request.MaxMarks < highestAwarded)
                throw new BusinessRuleException(
                    $"পূর্ণ নম্বর {request.MaxMarks} করা যাবে না — একটি জমায় ইতিমধ্যে {highestAwarded} নম্বর দেওয়া হয়েছে।");
        }

        assignment.Title = request.Title.Trim();
        assignment.Description = request.Description.Trim();
        assignment.CourseId = request.CourseId;
        assignment.Type = request.Type;
        assignment.ChapterOrLesson = string.IsNullOrWhiteSpace(request.ChapterOrLesson)
            ? null
            : request.ChapterOrLesson.Trim();
        assignment.Deadline = request.Deadline;
        assignment.MaxMarks = request.MaxMarks;
        assignment.AllowLateSubmission = request.AllowLateSubmission;
        assignment.AllowResubmission = request.AllowResubmission;
        assignment.AllowComments = request.AllowComments;
        assignment.UpdatedAt = now;

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Assignment {AssignmentId} updated by {UserId}", id, currentUser.UserId);

        return await GetByIdAsync(id, ct);
    }

    public async Task<AssignmentDto> PublishAsync(Guid id, CancellationToken ct = default)
    {
        var assignment = await db.Assignments
            .Include(a => a.Course)
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new NotFoundException($"অ্যাসাইনমেন্ট {id} পাওয়া যায়নি।");

        EnsureCanManage(assignment);

        if (assignment.IsPublished)
            throw new BusinessRuleException("এই অ্যাসাইনমেন্ট ইতিমধ্যে প্রকাশিত।");

        var now = timeProvider.GetUtcNow();

        // RULE: publishing work students could never submit to is a configuration error.
        ValidateDeadline(assignment.Deadline, now);
        ValidateMaxMarks(assignment.MaxMarks);

        assignment.Status = AssignmentStatus.Published;
        assignment.PublishedAt = now;
        assignment.UpdatedAt = now;

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Assignment {AssignmentId} published by {UserId}", id, currentUser.UserId);

        return await GetByIdAsync(id, ct);
    }

    public async Task<AssignmentDto> UnpublishAsync(Guid id, CancellationToken ct = default)
    {
        var assignment = await db.Assignments
            .Include(a => a.Course)
            .Include(a => a.Submissions)
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new NotFoundException($"অ্যাসাইনমেন্ট {id} পাওয়া যায়নি।");

        EnsureCanManage(assignment);

        if (!assignment.IsPublished)
            throw new BusinessRuleException("এই অ্যাসাইনমেন্ট প্রকাশিত নয়।");

        // RULE: withdrawing an assignment students have already answered would hide their work.
        if (assignment.Submissions.Count > 0)
            throw new BusinessRuleException(
                "শিক্ষার্থীরা কাজ জমা দিয়েছে, তাই এটি খসড়ায় ফেরানো যাবে না।");

        var now = timeProvider.GetUtcNow();
        assignment.Status = AssignmentStatus.Draft;
        assignment.PublishedAt = null;
        assignment.UpdatedAt = now;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var assignment = await db.Assignments
            .Include(a => a.Course)
            .Include(a => a.Submissions)
            .Include(a => a.Attachments)
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new NotFoundException($"অ্যাসাইনমেন্ট {id} পাওয়া যায়নি।");

        EnsureCanManage(assignment);

        // RULE: student work is never destroyed as a side effect of a teacher's cleanup.
        if (assignment.Submissions.Count > 0)
            throw new BusinessRuleException(
                "এই অ্যাসাইনমেন্টে জমা রয়েছে, তাই মুছে ফেলা যাবে না।");

        // The rows cascade; the bytes have to be removed deliberately or they leak forever.
        var keys = assignment.Attachments.Select(a => a.StorageKey).ToList();

        db.Assignments.Remove(assignment);
        await db.SaveChangesAsync(ct);

        foreach (var key in keys)
            await storage.DeleteAsync(key, ct);

        logger.LogInformation("Assignment {AssignmentId} deleted by {UserId}", id, currentUser.UserId);
    }

    // ------------------------------------------------------------ attachments

    public async Task<AttachmentDto> AddAttachmentAsync(
        Guid id, FileUpload file, CancellationToken ct = default)
    {
        var assignment = await db.Assignments
            .Include(a => a.Course)
            .Include(a => a.Attachments)
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new NotFoundException($"অ্যাসাইনমেন্ট {id} পাওয়া যায়নি।");

        // Only the owning teacher attaches material — an admin has read-only oversight and
        // should not be adding to somebody else's lesson.
        if (currentUser.Role != UserRole.Teacher || assignment.CreatedByTeacherId != currentUser.UserId)
            throw new ForbiddenException("কেবল সংশ্লিষ্ট শিক্ষক ফাইল সংযুক্ত করতে পারেন।");

        var maxCount = await settings.GetIntAsync(AppSettingKeys.MaxAttachmentsPerAssignment, 5, ct);
        if (assignment.Attachments.Count >= maxCount)
            throw new BusinessRuleException($"সর্বোচ্চ {maxCount}টি ফাইল সংযুক্ত করা যাবে।");

        await ValidateUploadAsync(file, ct);

        var storageKey = await storage.SaveAsync("assignments", file, ct);

        var attachment = new AssignmentAttachment
        {
            AssignmentId = id,
            FileName = SafeDisplayName(file.FileName),
            StorageKey = storageKey,
            ContentType = file.ContentType,
            SizeBytes = file.SizeBytes,
            UploadedById = currentUser.UserId,
            UploadedAt = timeProvider.GetUtcNow()
        };

        db.AssignmentAttachments.Add(attachment);
        await db.SaveChangesAsync(ct);

        return attachment.ToDto();
    }

    public async Task RemoveAttachmentAsync(
        Guid id, Guid attachmentId, CancellationToken ct = default)
    {
        var assignment = await db.Assignments
            .Include(a => a.Course)
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new NotFoundException($"অ্যাসাইনমেন্ট {id} পাওয়া যায়নি।");

        if (currentUser.Role != UserRole.Teacher || assignment.CreatedByTeacherId != currentUser.UserId)
            throw new ForbiddenException("কেবল সংশ্লিষ্ট শিক্ষক সংযুক্তি সরাতে পারেন।");

        var attachment = await db.AssignmentAttachments
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.AssignmentId == id, ct)
            ?? throw new NotFoundException($"সংযুক্তি {attachmentId} পাওয়া যায়নি।");

        db.AssignmentAttachments.Remove(attachment);
        await db.SaveChangesAsync(ct);
        await storage.DeleteAsync(attachment.StorageKey, ct);
    }

    public async Task<StoredFile> OpenAttachmentAsync(
        Guid id, Guid attachmentId, CancellationToken ct = default)
    {
        // Download goes through the same visibility check as the assignment itself, so a
        // direct link to a draft's worksheet is as invisible as the draft.
        await EnsureCanReadAssignmentAsync(id, ct);

        var attachment = await db.AssignmentAttachments
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.AssignmentId == id, ct)
            ?? throw new NotFoundException($"সংযুক্তি {attachmentId} পাওয়া যায়নি।");

        return await storage.OpenAsync(
                   attachment.StorageKey, attachment.FileName, attachment.ContentType, ct)
               ?? throw new NotFoundException("ফাইলটি সার্ভারে পাওয়া যায়নি।");
    }

    // --------------------------------------------------------------- comments

    public async Task<IReadOnlyList<AssignmentCommentDto>> ListCommentsAsync(
        Guid id, CancellationToken ct = default)
    {
        var assignment = await EnsureCanReadAssignmentAsync(id, ct);

        var comments = await db.AssignmentComments
            .Include(c => c.Author)
            .Where(c => c.AssignmentId == id)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);

        return comments.Select(c => c.ToDto(CanDeleteComment(c, assignment))).ToList();
    }

    public async Task<AssignmentCommentDto> AddCommentAsync(
        Guid id, CreateCommentRequest request, CancellationToken ct = default)
    {
        var assignment = await EnsureCanReadAssignmentAsync(id, ct);

        // RULE: an admin observes; they do not join the class conversation.
        if (currentUser.Role == UserRole.Admin)
            throw new ForbiddenException("প্রশাসক শ্রেণি আলোচনায় মন্তব্য করতে পারেন না।");

        if (!assignment.AllowComments)
            throw new BusinessRuleException("এই অ্যাসাইনমেন্টে মন্তব্য বন্ধ রয়েছে।");

        var body = request.Body.Trim();
        if (body.Length == 0)
            throw new ValidationFailedException("মন্তব্য খালি রাখা যাবে না।");

        var comment = new AssignmentComment
        {
            AssignmentId = id,
            AuthorId = currentUser.UserId,
            Body = body,
            CreatedAt = timeProvider.GetUtcNow()
        };

        db.AssignmentComments.Add(comment);
        await db.SaveChangesAsync(ct);

        var created = await db.AssignmentComments
            .Include(c => c.Author)
            .FirstAsync(c => c.Id == comment.Id, ct);

        return created.ToDto(canDelete: true);
    }

    public async Task DeleteCommentAsync(Guid id, Guid commentId, CancellationToken ct = default)
    {
        var assignment = await EnsureCanReadAssignmentAsync(id, ct);

        var comment = await db.AssignmentComments
            .FirstOrDefaultAsync(c => c.Id == commentId && c.AssignmentId == id, ct)
            ?? throw new NotFoundException($"মন্তব্য {commentId} পাওয়া যায়নি।");

        if (!CanDeleteComment(comment, assignment))
            throw new ForbiddenException("এই মন্তব্য মোছার অনুমতি নেই।");

        if (comment.IsDeleted)
            return;

        comment.DeletedAt = timeProvider.GetUtcNow();
        comment.DeletedById = currentUser.UserId;
        comment.Body = string.Empty;

        await db.SaveChangesAsync(ct);
    }

    // ---------------------------------------------------------- student surface

    public async Task<PagedResult<StudentAssignmentDto>> ListForStudentAsync(
        StudentAssignmentListQuery query, CancellationToken ct = default)
    {
        if (currentUser.Role != UserRole.Student)
            throw new ForbiddenException("এই তালিকা কেবল শিক্ষার্থীদের জন্য।");

        var now = timeProvider.GetUtcNow();
        var courseIds = await StudentCourseIdsAsync(ct);

        // RULE: students only ever see PUBLISHED work on courses they actually take — which
        // already excludes another faith's religion course.
        var q = BaseQuery()
            .Where(a => a.Status == AssignmentStatus.Published && courseIds.Contains(a.CourseId));

        if (query.CourseId is { } courseId)
            q = q.Where(a => a.CourseId == courseId);

        if (query.SubjectId is { } subjectId)
            q = q.Where(a => a.Course.SubjectId == subjectId);

        if (query.DueOnly == true)
            q = q.Where(a => a.Deadline >= now);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            q = q.Where(a => a.Title.ToLower().Contains(term));
        }

        if (query.PendingOnly == true)
            q = q.Where(a => !a.Submissions.Any(s => s.StudentId == currentUser.UserId));

        var paged = await q
            .OrderBy(a => a.Deadline)
            .Select(a => new
            {
                Assignment = a,
                // Only the caller's own submission is ever projected.
                MySubmission = a.Submissions.FirstOrDefault(s => s.StudentId == currentUser.UserId),
                CommentCount = a.Comments.Count(c => c.DeletedAt == null)
            })
            .ToPagedResultAsync(query.Page, query.PageSize, ct);

        var items = paged.Items
            .Select(x => x.Assignment.ToStudentDto(x.MySubmission, now, x.CommentCount))
            .ToList();

        return PagedResult<StudentAssignmentDto>.Create(
            items, paged.Page, paged.PageSize, paged.TotalCount);
    }

    public async Task<StudentAssignmentDto> GetForStudentAsync(
        Guid id, CancellationToken ct = default)
    {
        if (currentUser.Role != UserRole.Student)
            throw new ForbiddenException("এই তালিকা কেবল শিক্ষার্থীদের জন্য।");

        var now = timeProvider.GetUtcNow();
        var courseIds = await StudentCourseIdsAsync(ct);

        var result = await BaseQuery()
            .Where(a => a.Id == id
                        && a.Status == AssignmentStatus.Published
                        && courseIds.Contains(a.CourseId))
            .Select(a => new
            {
                Assignment = a,
                MySubmission = a.Submissions.FirstOrDefault(s => s.StudentId == currentUser.UserId),
                CommentCount = a.Comments.Count(c => c.DeletedAt == null)
            })
            .FirstOrDefaultAsync(ct)
            // A draft, or another course's work, is reported as missing rather than forbidden so
            // the endpoint does not confirm that the assignment exists.
            ?? throw new NotFoundException($"অ্যাসাইনমেন্ট {id} পাওয়া যায়নি।");

        return result.Assignment.ToStudentDto(result.MySubmission, now, result.CommentCount);
    }

    // ---------------------------------------------------------------- helpers

    private IQueryable<Assignment> BaseQuery() =>
        db.Assignments
            .Include(a => a.Course).ThenInclude(c => c.ClassRoom)
            .Include(a => a.Course).ThenInclude(c => c.Subject)
            .Include(a => a.CreatedByTeacher)
            .Include(a => a.Attachments)
            .AsQueryable();

    /// <summary>
    /// Course ids the calling student takes. Mirrors the rule in <see cref="AcademicService"/>:
    /// their class's active courses, minus religion courses for another faith.
    /// </summary>
    private async Task<List<Guid>> StudentCourseIdsAsync(CancellationToken ct)
    {
        var me = await db.Users
            .Where(u => u.Id == currentUser.UserId)
            .Select(u => new { u.Faith })
            .FirstOrDefaultAsync(ct);

        var classIds = await db.Enrollments
            .Where(e => e.StudentId == currentUser.UserId)
            .Select(e => e.ClassRoomId)
            .ToListAsync(ct);

        return await db.Courses
            .Where(c => c.IsActive
                        && classIds.Contains(c.ClassRoomId)
                        && (c.Subject.FaithGroup == null || c.Subject.FaithGroup == me!.Faith))
            .Select(c => c.Id)
            .ToListAsync(ct);
    }

    /// <summary>How many students the work was set for — the denominator on the teacher's view.</summary>
    private async Task<int> ExpectedSubmissionCountAsync(Assignment assignment, CancellationToken ct)
    {
        var faith = assignment.Course.Subject?.FaithGroup
                    ?? await db.Subjects
                        .Where(s => s.Id == assignment.Course.SubjectId)
                        .Select(s => s.FaithGroup)
                        .FirstOrDefaultAsync(ct);

        return await db.Enrollments.CountAsync(
            e => e.ClassRoomId == assignment.Course.ClassRoomId
                 && (faith == null || e.Student.Faith == faith), ct);
    }

    /// <summary>
    /// Loads an assignment the caller is allowed to read — used by the comment thread and the
    /// attachment download, which both have to work for teachers, admins and enrolled students.
    /// </summary>
    private async Task<Assignment> EnsureCanReadAssignmentAsync(Guid id, CancellationToken ct)
    {
        var assignment = await db.Assignments
            .Include(a => a.Course)
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new NotFoundException($"অ্যাসাইনমেন্ট {id} পাওয়া যায়নি।");

        switch (currentUser.Role)
        {
            case UserRole.Admin:
                return assignment;

            case UserRole.Teacher when assignment.Course.IsTaughtBy(currentUser.UserId):
                return assignment;

            case UserRole.Student when assignment.IsPublished:
                var courseIds = await StudentCourseIdsAsync(ct);
                if (courseIds.Contains(assignment.CourseId))
                    return assignment;
                break;
        }

        throw new NotFoundException($"অ্যাসাইনমেন্ট {id} পাওয়া যায়নি।");
    }

    /// <summary>Authors delete their own; the course's teacher moderates the whole thread.</summary>
    private bool CanDeleteComment(AssignmentComment comment, Assignment assignment)
    {
        if (comment.AuthorId == currentUser.UserId)
            return true;

        return currentUser.Role == UserRole.Teacher
               && assignment.Course.IsTaughtBy(currentUser.UserId);
    }

    /// <summary>Owner teacher may manage; admin has oversight; everyone else is refused.</summary>
    private void EnsureCanManage(Assignment assignment)
    {
        if (currentUser.Role == UserRole.Admin)
            return;

        if (currentUser.Role == UserRole.Teacher && assignment.CreatedByTeacherId == currentUser.UserId)
            return;

        throw new ForbiddenException("এই অ্যাসাইনমেন্ট পরিচালনার অনুমতি নেই।");
    }

    private void EnsureTeaches(Course course)
    {
        if (!course.IsTaughtBy(currentUser.UserId))
            throw new ForbiddenException(
                "আপনি এই কোর্সের শিক্ষক নন। প্রশাসকের সাথে যোগাযোগ করুন।");
    }

    private static void EnsureTypeAllowed(Course course, AssignmentType type)
    {
        var allowed = course.Subject?.AllowedAssignmentTypes;

        // An empty list means the subject has not been configured; refuse rather than allow
        // everything, so a misconfiguration fails loudly instead of silently widening the rule.
        if (allowed is null || allowed.All(t => t.Type != type))
            throw new ValidationFailedException(
                $"'{BanglaNames.AssignmentType(type)}' ধরনের কাজ "
                + $"'{course.Subject?.Name}' বিষয়ে দেওয়া যায় না।");
    }

    private async Task ValidateUploadAsync(FileUpload file, CancellationToken ct)
    {
        var maxMb = await settings.GetIntAsync(AppSettingKeys.MaxUploadSizeMb, 10, ct);
        if (file.SizeBytes > (long)maxMb * 1024 * 1024)
            throw new ValidationFailedException($"ফাইলের আকার সর্বোচ্চ {maxMb} মেগাবাইট হতে পারে।");

        if (file.SizeBytes <= 0)
            throw new ValidationFailedException("খালি ফাইল আপলোড করা যাবে না।");

        var allowedRaw = await settings.GetStringAsync(
            AppSettingKeys.AllowedUploadExtensions, "pdf,doc,docx,txt,jpg,jpeg,png", ct);

        var allowed = allowedRaw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.ToLowerInvariant())
            .ToHashSet();

        var extension = Path.GetExtension(file.FileName).TrimStart('.').ToLowerInvariant();

        if (extension.Length == 0 || !allowed.Contains(extension))
            throw new ValidationFailedException(
                $"এই ধরনের ফাইল অনুমোদিত নয়। অনুমোদিত: {string.Join(", ", allowed)}");
    }

    /// <summary>
    /// Strips any path from the uploader's filename before it is stored for display. The bytes
    /// are already written under a generated key, so this only protects whatever renders the
    /// name later.
    /// </summary>
    private static string SafeDisplayName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        return string.IsNullOrWhiteSpace(name) ? "attachment" : name;
    }

    private static int IsoWeekOf(DateTimeOffset instant) =>
        ISOWeek.GetWeekOfYear(instant.UtcDateTime);

    private static void ValidateDeadline(DateTimeOffset deadline, DateTimeOffset now)
    {
        if (deadline <= now)
            throw new ValidationFailedException("সময়সীমা ভবিষ্যতে হতে হবে।");
    }

    private static void ValidateMaxMarks(int maxMarks)
    {
        if (maxMarks <= 0)
            throw new ValidationFailedException("পূর্ণ নম্বর শূন্যের বেশি হতে হবে।");
    }
}
