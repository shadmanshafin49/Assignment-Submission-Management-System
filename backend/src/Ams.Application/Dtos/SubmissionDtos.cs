using Ams.Application.Common;
using Ams.Domain.Enums;

namespace Ams.Application.Dtos;

public record CreateSubmissionRequest(string AnswerText);

public record UpdateSubmissionRequest(string AnswerText);

public record GradeSubmissionRequest(int Marks, string? Feedback);

public record UpdateSubmissionStatusRequest(SubmissionStatus Status);

public record SubmissionDto(
    Guid Id,
    Guid AssignmentId,
    string AssignmentTitle,
    Guid StudentId,
    string StudentName,
    string StudentEmail,
    int? RollNumber,
    string AnswerText,
    SubmissionStatus Status,
    DateTimeOffset SubmittedAt,
    DateTimeOffset? UpdatedAt,
    int? Marks,
    int MaxMarks,
    string? Feedback,
    string? GradedByTeacherName,
    DateTimeOffset? GradedAt,
    bool CanEdit,
    IReadOnlyList<AttachmentDto> Attachments);

public class SubmissionListQuery : PagedQuery
{
    public SubmissionStatus? Status { get; set; }
    public Guid? AssignmentId { get; set; }
    public Guid? StudentId { get; set; }
    public Guid? CourseId { get; set; }
    public Guid? ClassRoomId { get; set; }
}
