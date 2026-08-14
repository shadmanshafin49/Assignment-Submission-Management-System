using Ams.Application.Abstractions;
using Ams.Application.Common;
using Ams.Application.Dtos;
using Ams.Domain.Entities;
using Ams.Domain.Enums;
using Ams.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ams.Application.Services;

public class SubmissionService(
    IAppDbContext db,
    ICurrentUser currentUser,
    ISettingsService settings,
    IFileStorage storage,
    TimeProvider timeProvider,
    ILogger<SubmissionService> logger) : ISubmissionService
{
    // ---------------------------------------------------------- student surface

    public async Task<SubmissionDto> SubmitAsync(
        Guid assignmentId,
        CreateSubmissionRequest request,
        IReadOnlyList<FileUpload> files,
        CancellationToken ct = default)
    {
        if (currentUser.Role != UserRole.Student)
            throw new ForbiddenException("কেবল শিক্ষার্থী কাজ জমা দিতে পারে।");

        var now = timeProvider.GetUtcNow();
        var answer = request.AnswerText.Trim();

        // RULE: an answer must actually contain something — typed text, files, or both.
        if (answer.Length == 0 && files.Count == 0)
            throw new ValidationFailedException("উত্তর লিখুন অথবা অন্তত একটি ফাইল সংযুক্ত করুন।");

        var maxCount = await settings.GetIntAsync(AppSettingKeys.MaxAttachmentsPerSubmission, 3, ct);
        if (files.Count > maxCount)
            throw new BusinessRuleException($"সর্বোচ্চ {maxCount}টি ফাইল সংযুক্ত করা যাবে।");

        foreach (var file in files)
            await ValidateUploadAsync(file, ct);

        var assignment = await db.Assignments
            .Include(a => a.Course).ThenInclude(c => c.Subject)
            .FirstOrDefaultAsync(a => a.Id == assignmentId, ct)
            ?? throw new NotFoundException($"অ্যাসাইনমেন্ট {assignmentId} পাওয়া যায়নি।");

        // RULE: a draft is invisible to students — report it as missing rather than forbidden.
        if (!assignment.IsVisibleToStudents)
            throw new NotFoundException($"অ্যাসাইনমেন্ট {assignmentId} পাওয়া যায়নি।");

        // RULE: students may only submit to work set on a course they actually take.
        await EnsureTakesCourseAsync(assignment.Course, ct);

        // RULE: one submission per student per assignment. Editing goes through UpdateAsync.
        var existing = await db.Submissions.FirstOrDefaultAsync(
            s => s.AssignmentId == assignmentId && s.StudentId == currentUser.UserId, ct);

        if (existing is not null)
            throw new BusinessRuleException(
                "আপনি এই অ্যাসাইনমেন্ট ইতিমধ্যে জমা দিয়েছেন। পরিবর্তে জমাটি সম্পাদনা করুন।");

        // RULE: past the deadline, a submission is only accepted when the assignment allows
        // late work — and it is then flagged Late rather than silently treated as on time.
        if (!assignment.CanAcceptSubmission(now))
            throw new BusinessRuleException(
                "এই অ্যাসাইনমেন্টের সময়সীমা শেষ হয়ে গেছে এবং বিলম্বে জমা গ্রহণ করা হয় না।");

        var isLate = assignment.IsPastDeadline(now);

        var submission = new Submission
        {
            AssignmentId = assignmentId,
            StudentId = currentUser.UserId,
            AnswerText = answer,
            Status = isLate ? SubmissionStatus.Late : SubmissionStatus.Submitted,
            SubmittedAt = now
        };

        // Files are written to storage before the row is saved, so a failed write aborts the
        // whole submission rather than recording an answer whose attachment does not exist.
        foreach (var file in files)
        {
            var storageKey = await storage.SaveAsync("submissions", file, ct);

            submission.Attachments.Add(new SubmissionAttachment
            {
                FileName = SafeDisplayName(file.FileName),
                StorageKey = storageKey,
                ContentType = file.ContentType,
                SizeBytes = file.SizeBytes,
                UploadedById = currentUser.UserId,
                UploadedAt = now
            });
        }

        db.Submissions.Add(submission);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Student {StudentId} submitted assignment {AssignmentId} (late={IsLate})",
            currentUser.UserId, assignmentId, isLate);

        return await GetByIdAsync(submission.Id, ct);
    }

    public async Task<SubmissionDto> UpdateAsync(
        Guid submissionId, UpdateSubmissionRequest request, CancellationToken ct = default)
    {
        if (currentUser.Role != UserRole.Student)
            throw new ForbiddenException("কেবল শিক্ষার্থী নিজের জমা সম্পাদনা করতে পারে।");

        var now = timeProvider.GetUtcNow();

        var submission = await db.Submissions
            .Include(s => s.Assignment)
            .Include(s => s.Attachments)
            .FirstOrDefaultAsync(s => s.Id == submissionId, ct)
            ?? throw new NotFoundException($"জমা {submissionId} পাওয়া যায়নি।");

        // RULE: a student may only ever touch their own work.
        if (submission.StudentId != currentUser.UserId)
            throw new ForbiddenException("আপনি কেবল নিজের জমা সম্পাদনা করতে পারেন।");

        // RULE: editable only while resubmission is permitted, the work is ungraded, and the
        // deadline has not passed — unless the teacher explicitly returned it for revision.
        if (!submission.CanBeEditedByStudent(submission.Assignment, now))
            throw new BusinessRuleException(BuildNotEditableReason(submission, now));

        var answer = request.AnswerText.Trim();

        // RULE: an answer must have text or at least one file. Blanking both would leave the
        // teacher a submission with nothing in it, which reads as "submitted" but is not.
        if (answer.Length == 0 && submission.Attachments.Count == 0)
            throw new ValidationFailedException("উত্তর লিখুন অথবা অন্তত একটি ফাইল সংযুক্ত করুন।");

        submission.AnswerText = answer;
        submission.UpdatedAt = now;

        // Re-submitting after a return puts the work back in the grading queue, preserving
        // whether it was originally late.
        if (submission.Status == SubmissionStatus.ReturnedForRevision)
        {
            submission.Status = submission.Assignment.IsPastDeadline(now)
                ? SubmissionStatus.Late
                : SubmissionStatus.Submitted;
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Student {StudentId} updated submission {SubmissionId}",
            currentUser.UserId, submissionId);

        return await GetByIdAsync(submissionId, ct);
    }

    public async Task<PagedResult<SubmissionDto>> ListMineAsync(
        SubmissionListQuery query, CancellationToken ct = default)
    {
        if (currentUser.Role != UserRole.Student)
            throw new ForbiddenException("এই তালিকা কেবল শিক্ষার্থীদের জন্য।");

        var q = BaseQuery().Where(s => s.StudentId == currentUser.UserId);

        if (query.Status is { } status)
            q = q.Where(s => s.Status == status);

        if (query.AssignmentId is { } assignmentId)
            q = q.Where(s => s.AssignmentId == assignmentId);

        if (query.CourseId is { } courseId)
            q = q.Where(s => s.Assignment.CourseId == courseId);

        return await ProjectAsync(q.OrderByDescending(s => s.SubmittedAt), query, ct);
    }

    // ------------------------------------------------------------ attachments

    public async Task<AttachmentDto> AddAttachmentAsync(
        Guid submissionId, FileUpload file, CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow();

        var submission = await db.Submissions
            .Include(s => s.Assignment)
            .Include(s => s.Attachments)
            .FirstOrDefaultAsync(s => s.Id == submissionId, ct)
            ?? throw new NotFoundException($"জমা {submissionId} পাওয়া যায়নি।");

        if (currentUser.Role != UserRole.Student || submission.StudentId != currentUser.UserId)
            throw new ForbiddenException("আপনি কেবল নিজের জমায় ফাইল যুক্ত করতে পারেন।");

        // Attaching is editing: the same window that governs changing the text governs adding
        // a file, otherwise a graded answer could still be altered.
        if (!submission.CanBeEditedByStudent(submission.Assignment, now))
            throw new BusinessRuleException(BuildNotEditableReason(submission, now));

        var maxCount = await settings.GetIntAsync(AppSettingKeys.MaxAttachmentsPerSubmission, 3, ct);
        if (submission.Attachments.Count >= maxCount)
            throw new BusinessRuleException($"সর্বোচ্চ {maxCount}টি ফাইল সংযুক্ত করা যাবে।");

        await ValidateUploadAsync(file, ct);

        var storageKey = await storage.SaveAsync("submissions", file, ct);

        var attachment = new SubmissionAttachment
        {
            SubmissionId = submissionId,
            FileName = SafeDisplayName(file.FileName),
            StorageKey = storageKey,
            ContentType = file.ContentType,
            SizeBytes = file.SizeBytes,
            UploadedById = currentUser.UserId,
            UploadedAt = now
        };

        db.SubmissionAttachments.Add(attachment);
        submission.UpdatedAt = now;
        await db.SaveChangesAsync(ct);

        return attachment.ToDto();
    }

    public async Task RemoveAttachmentAsync(
        Guid submissionId, Guid attachmentId, CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow();

        var submission = await db.Submissions
            .Include(s => s.Assignment)
            .Include(s => s.Attachments)
            .FirstOrDefaultAsync(s => s.Id == submissionId, ct)
            ?? throw new NotFoundException($"জমা {submissionId} পাওয়া যায়নি।");

        if (currentUser.Role != UserRole.Student || submission.StudentId != currentUser.UserId)
            throw new ForbiddenException("আপনি কেবল নিজের জমার ফাইল সরাতে পারেন।");

        if (!submission.CanBeEditedByStudent(submission.Assignment, now))
            throw new BusinessRuleException(BuildNotEditableReason(submission, now));

        var attachment = submission.Attachments.FirstOrDefault(a => a.Id == attachmentId)
            ?? throw new NotFoundException($"সংযুক্তি {attachmentId} পাওয়া যায়নি।");

        // RULE: removing the last file from an answer with no text would empty the submission.
        if (submission.Attachments.Count == 1 && submission.AnswerText.Length == 0)
            throw new BusinessRuleException(
                "এটিই একমাত্র উত্তর — সরানোর আগে লিখিত উত্তর দিন বা অন্য ফাইল যুক্ত করুন।");

        db.SubmissionAttachments.Remove(attachment);
        submission.UpdatedAt = now;
        await db.SaveChangesAsync(ct);

        await storage.DeleteAsync(attachment.StorageKey, ct);
    }

    public async Task<StoredFile> OpenAttachmentAsync(
        Guid submissionId, Guid attachmentId, CancellationToken ct = default)
    {
        var submission = await BaseQuery().FirstOrDefaultAsync(s => s.Id == submissionId, ct)
            ?? throw new NotFoundException($"জমা {submissionId} পাওয়া যায়নি।");

        // The same rule as reading the submission itself: author, marking teacher, or admin.
        EnsureCanView(submission);

        var attachment = await db.SubmissionAttachments
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.SubmissionId == submissionId, ct)
            ?? throw new NotFoundException($"সংযুক্তি {attachmentId} পাওয়া যায়নি।");

        return await storage.OpenAsync(
                   attachment.StorageKey, attachment.FileName, attachment.ContentType, ct)
               ?? throw new NotFoundException("ফাইলটি সার্ভারে পাওয়া যায়নি।");
    }

    // ------------------------------------------------------ teacher/admin surface

    public async Task<PagedResult<SubmissionDto>> ListForAssignmentAsync(
        Guid assignmentId, SubmissionListQuery query, CancellationToken ct = default)
    {
        var assignment = await db.Assignments
            .Include(a => a.Course)
            .FirstOrDefaultAsync(a => a.Id == assignmentId, ct)
            ?? throw new NotFoundException($"অ্যাসাইনমেন্ট {assignmentId} পাওয়া যায়নি।");

        EnsureCanReviewAssignment(assignment);

        var q = BaseQuery().Where(s => s.AssignmentId == assignmentId);

        if (query.Status is { } status)
            q = q.Where(s => s.Status == status);

        if (query.StudentId is { } studentId)
            q = q.Where(s => s.StudentId == studentId);

        // Marking runs down the roll sheet, so that is the order a teacher expects.
        return await ProjectAsync(q, query, ct, orderByRoll: assignment.Course.ClassRoomId);
    }

    public async Task<PagedResult<SubmissionDto>> ListAllAsync(
        SubmissionListQuery query, CancellationToken ct = default)
    {
        var q = BaseQuery();

        // A teacher sees submissions on their own courses; an admin sees everything.
        if (currentUser.Role == UserRole.Teacher)
            q = q.Where(s => s.Assignment.Course.TeacherId == currentUser.UserId);
        else if (currentUser.Role != UserRole.Admin)
            throw new ForbiddenException("এখানে জমা দেখার অনুমতি কেবল শিক্ষক ও প্রশাসকের।");

        if (query.Status is { } status)
            q = q.Where(s => s.Status == status);

        if (query.AssignmentId is { } assignmentId)
            q = q.Where(s => s.AssignmentId == assignmentId);

        if (query.StudentId is { } studentId)
            q = q.Where(s => s.StudentId == studentId);

        if (query.CourseId is { } courseId)
            q = q.Where(s => s.Assignment.CourseId == courseId);

        if (query.ClassRoomId is { } classRoomId)
            q = q.Where(s => s.Assignment.Course.ClassRoomId == classRoomId);

        return await ProjectAsync(q.OrderByDescending(s => s.SubmittedAt), query, ct);
    }

    public async Task<SubmissionDto> GetByIdAsync(
        Guid submissionId, CancellationToken ct = default)
    {
        var submission = await BaseQuery().FirstOrDefaultAsync(s => s.Id == submissionId, ct)
            ?? throw new NotFoundException($"জমা {submissionId} পাওয়া যায়নি।");

        EnsureCanView(submission);

        var roll = await RollNumberAsync(
            submission.StudentId, submission.Assignment.Course.ClassRoomId, ct);

        return submission.ToDto(submission.Assignment, timeProvider.GetUtcNow(), roll);
    }

    public async Task<SubmissionDto> GradeAsync(
        Guid submissionId, GradeSubmissionRequest request, CancellationToken ct = default)
    {
        var submission = await db.Submissions
            .Include(s => s.Assignment).ThenInclude(a => a.Course)
            .FirstOrDefaultAsync(s => s.Id == submissionId, ct)
            ?? throw new NotFoundException($"জমা {submissionId} পাওয়া যায়নি।");

        // RULE: only the teacher who owns the assignment may grade it. Admins have read-only
        // oversight and deliberately cannot award marks.
        EnsureCanGrade(submission.Assignment);

        // RULE: marks must fall within 0..MaxMarks.
        if (!submission.Assignment.IsValidMark(request.Marks))
            throw new ValidationFailedException(
                $"নম্বর ০ থেকে {submission.Assignment.MaxMarks} এর মধ্যে হতে হবে।");

        var now = timeProvider.GetUtcNow();

        submission.Marks = request.Marks;
        submission.Feedback = string.IsNullOrWhiteSpace(request.Feedback)
            ? null
            : request.Feedback.Trim();
        submission.Status = SubmissionStatus.Graded;
        submission.GradedByTeacherId = currentUser.UserId;
        submission.GradedAt = now;
        submission.UpdatedAt = now;

        await db.SaveChangesAsync(ct);
        logger.LogInformation(
            "Teacher {TeacherId} graded submission {SubmissionId} with {Marks}/{MaxMarks}",
            currentUser.UserId, submissionId, request.Marks, submission.Assignment.MaxMarks);

        return await GetByIdAsync(submissionId, ct);
    }

    public async Task<SubmissionDto> UpdateStatusAsync(
        Guid submissionId, UpdateSubmissionStatusRequest request, CancellationToken ct = default)
    {
        var submission = await db.Submissions
            .Include(s => s.Assignment).ThenInclude(a => a.Course)
            .FirstOrDefaultAsync(s => s.Id == submissionId, ct)
            ?? throw new NotFoundException($"জমা {submissionId} পাওয়া যায়নি।");

        EnsureCanGrade(submission.Assignment);

        var now = timeProvider.GetUtcNow();

        // Moving to Graded requires marks, which only the grade endpoint can supply.
        if (request.Status == SubmissionStatus.Graded)
            throw new BusinessRuleException("মূল্যায়নের জন্য গ্রেড এন্ডপয়েন্ট ব্যবহার করুন।");

        // Returning work for revision clears the previous grade so the student is not shown
        // marks for an answer they are being asked to replace.
        if (request.Status == SubmissionStatus.ReturnedForRevision)
        {
            submission.Marks = null;
            submission.GradedByTeacherId = null;
            submission.GradedAt = null;
        }

        submission.Status = request.Status;
        submission.UpdatedAt = now;

        await db.SaveChangesAsync(ct);
        logger.LogInformation(
            "Teacher {TeacherId} set submission {SubmissionId} to {Status}",
            currentUser.UserId, submissionId, request.Status);

        return await GetByIdAsync(submissionId, ct);
    }

    // ---------------------------------------------------------------- helpers

    private IQueryable<Submission> BaseQuery() =>
        db.Submissions
            .Include(s => s.Assignment).ThenInclude(a => a.Course)
            .Include(s => s.Student)
            .Include(s => s.GradedByTeacher)
            .Include(s => s.Attachments)
            .AsQueryable();

    private async Task<PagedResult<SubmissionDto>> ProjectAsync(
        IQueryable<Submission> query,
        SubmissionListQuery paging,
        CancellationToken ct,
        Guid? orderByRoll = null)
    {
        var now = timeProvider.GetUtcNow();

        // Roll numbers live on the enrollment, so ordering by roll means joining through it.
        var ordered = orderByRoll is { } classRoomId
            ? query.OrderBy(s => db.Enrollments
                .Where(e => e.StudentId == s.StudentId && e.ClassRoomId == classRoomId)
                .Select(e => (int?)e.RollNumber)
                .FirstOrDefault() ?? int.MaxValue)
            : query;

        var paged = await ordered.ToPagedResultAsync(paging.Page, paging.PageSize, ct);

        // One lookup for the whole page rather than one per row.
        var rolls = await RollLookupAsync(paged.Items, ct);

        var items = paged.Items
            .Select(s => s.ToDto(
                s.Assignment,
                now,
                rolls.GetValueOrDefault((s.StudentId, s.Assignment.Course.ClassRoomId))))
            .ToList();

        return PagedResult<SubmissionDto>.Create(items, paged.Page, paged.PageSize, paged.TotalCount);
    }

    private async Task<Dictionary<(Guid StudentId, Guid ClassRoomId), int>> RollLookupAsync(
        IReadOnlyList<Submission> submissions, CancellationToken ct)
    {
        if (submissions.Count == 0)
            return [];

        var studentIds = submissions.Select(s => s.StudentId).Distinct().ToList();
        var classIds = submissions.Select(s => s.Assignment.Course.ClassRoomId).Distinct().ToList();

        var rows = await db.Enrollments
            .Where(e => studentIds.Contains(e.StudentId) && classIds.Contains(e.ClassRoomId))
            .Select(e => new { e.StudentId, e.ClassRoomId, e.RollNumber })
            .ToListAsync(ct);

        return rows.ToDictionary(r => (r.StudentId, r.ClassRoomId), r => r.RollNumber);
    }

    private async Task<int?> RollNumberAsync(Guid studentId, Guid classRoomId, CancellationToken ct) =>
        await db.Enrollments
            .Where(e => e.StudentId == studentId && e.ClassRoomId == classRoomId)
            .Select(e => (int?)e.RollNumber)
            .FirstOrDefaultAsync(ct);

    /// <summary>
    /// A student may submit to a course only if they are on its class roster and — for the
    /// religion courses — the course matches their own faith group.
    /// </summary>
    private async Task EnsureTakesCourseAsync(Course course, CancellationToken ct)
    {
        var enrolled = await db.Enrollments.AnyAsync(
            e => e.StudentId == currentUser.UserId && e.ClassRoomId == course.ClassRoomId, ct);

        if (!enrolled)
            throw new ForbiddenException("এই অ্যাসাইনমেন্ট আপনার শ্রেণির জন্য নয়।");

        var faith = course.Subject?.FaithGroup
                    ?? await db.Subjects
                        .Where(s => s.Id == course.SubjectId)
                        .Select(s => s.FaithGroup)
                        .FirstOrDefaultAsync(ct);

        if (faith is null)
            return;

        var myFaith = await db.Users
            .Where(u => u.Id == currentUser.UserId)
            .Select(u => u.Faith)
            .FirstOrDefaultAsync(ct);

        if (myFaith != faith)
            throw new ForbiddenException("এই ধর্ম শিক্ষা কোর্সটি আপনার জন্য নয়।");
    }

    /// <summary>Students see only their own work; teachers see work on assignments they own.</summary>
    private void EnsureCanView(Submission submission)
    {
        switch (currentUser.Role)
        {
            case UserRole.Admin:
                return;

            case UserRole.Student when submission.StudentId == currentUser.UserId:
                return;

            case UserRole.Teacher when submission.Assignment.CreatedByTeacherId == currentUser.UserId:
                return;

            default:
                throw new ForbiddenException("এই জমা দেখার অনুমতি নেই।");
        }
    }

    private void EnsureCanReviewAssignment(Assignment assignment)
    {
        if (currentUser.Role == UserRole.Admin)
            return;

        if (currentUser.Role == UserRole.Teacher && assignment.CreatedByTeacherId == currentUser.UserId)
            return;

        throw new ForbiddenException("এই অ্যাসাইনমেন্টের জমা দেখার অনুমতি নেই।");
    }

    private void EnsureCanGrade(Assignment assignment)
    {
        if (currentUser.Role != UserRole.Teacher)
            throw new ForbiddenException("কেবল শিক্ষক মূল্যায়ন করতে পারেন।");

        if (assignment.CreatedByTeacherId != currentUser.UserId)
            throw new ForbiddenException("আপনি কেবল নিজের অ্যাসাইনমেন্টের জমা মূল্যায়ন করতে পারেন।");
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

    private static string SafeDisplayName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        return string.IsNullOrWhiteSpace(name) ? "attachment" : name;
    }

    private static string BuildNotEditableReason(Submission submission, DateTimeOffset now)
    {
        if (submission.IsGraded)
            return "এই জমা মূল্যায়ন হয়ে গেছে, তাই আর পরিবর্তন করা যাবে না।";

        if (!submission.Assignment.AllowResubmission)
            return "এই অ্যাসাইনমেন্টে জমা দেওয়ার পর পরিবর্তন করা যায় না।";

        if (submission.Assignment.IsPastDeadline(now))
            return "সময়সীমা শেষ হয়ে গেছে, তাই এই জমা আর পরিবর্তন করা যাবে না।";

        return "এই জমা আর পরিবর্তন করা যাবে না।";
    }
}
