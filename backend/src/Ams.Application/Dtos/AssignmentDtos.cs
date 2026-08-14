using Ams.Application.Common;
using Ams.Domain.Enums;

namespace Ams.Application.Dtos;

/// <summary>
/// A null <c>Deadline</c> lets the server apply the school's default window — seven days,
/// from <c>assignments.default_deadline_days</c>.
/// </summary>
public record CreateAssignmentRequest(
    string Title,
    string Description,
    Guid CourseId,
    AssignmentType Type,
    string? ChapterOrLesson,
    DateTimeOffset? Deadline,
    int MaxMarks,
    bool? AllowLateSubmission,
    bool? AllowResubmission,
    bool? AllowComments);

public record UpdateAssignmentRequest(
    string Title,
    string Description,
    Guid CourseId,
    AssignmentType Type,
    string? ChapterOrLesson,
    DateTimeOffset Deadline,
    int MaxMarks,
    bool AllowLateSubmission,
    bool AllowResubmission,
    bool AllowComments);

public record AttachmentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTimeOffset UploadedAt);

public record AssignmentCommentDto(
    Guid Id,
    Guid AssignmentId,
    Guid AuthorId,
    string AuthorName,
    UserRole AuthorRole,
    string Body,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    bool IsDeleted,
    bool CanDelete);

public record CreateCommentRequest(string Body);

/// <summary>Teacher/admin view of an assignment, including draft state and submission counts.</summary>
public record AssignmentDto(
    Guid Id,
    string Title,
    string Description,
    Guid CourseId,
    string CourseCode,
    Guid ClassRoomId,
    string ClassRoomName,
    Guid SubjectId,
    string SubjectName,
    AssignmentType Type,
    string? ChapterOrLesson,
    Guid CreatedByTeacherId,
    string CreatedByTeacherName,
    int WeekNumber,
    DateTimeOffset AssignedOn,
    DateTimeOffset Deadline,
    int MaxMarks,
    AssignmentStatus Status,
    bool AllowLateSubmission,
    bool AllowResubmission,
    bool AllowComments,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CreatedAt,
    int SubmissionCount,
    int GradedCount,
    int ExpectedSubmissionCount,
    IReadOnlyList<AttachmentDto> Attachments,
    int CommentCount);

/// <summary>
/// Student view. Deliberately omits draft state and other students' data, and folds in the
/// caller's own submission so the UI can render status without a second round-trip.
/// </summary>
public record StudentAssignmentDto(
    Guid Id,
    string Title,
    string Description,
    Guid CourseId,
    string CourseCode,
    string ClassRoomName,
    string SubjectName,
    AssignmentType Type,
    string? ChapterOrLesson,
    string TeacherName,
    int WeekNumber,
    DateTimeOffset AssignedOn,
    DateTimeOffset Deadline,
    int MaxMarks,
    bool AllowLateSubmission,
    bool AllowResubmission,
    bool AllowComments,
    bool IsPastDeadline,
    bool CanSubmit,
    IReadOnlyList<AttachmentDto> Attachments,
    int CommentCount,
    SubmissionDto? MySubmission);

public class AssignmentListQuery : PagedQuery
{
    public Guid? CourseId { get; set; }
    public Guid? ClassRoomId { get; set; }
    public Guid? SubjectId { get; set; }
    public AssignmentStatus? Status { get; set; }
    public AssignmentType? Type { get; set; }
    public Guid? TeacherId { get; set; }
    public int? WeekNumber { get; set; }
    public string? Search { get; set; }
}

public class StudentAssignmentListQuery : PagedQuery
{
    public Guid? CourseId { get; set; }
    public Guid? SubjectId { get; set; }
    public string? Search { get; set; }

    /// <summary>When true, only assignments the student has not submitted yet.</summary>
    public bool? PendingOnly { get; set; }

    /// <summary>When true, only work whose deadline has not passed.</summary>
    public bool? DueOnly { get; set; }
}
