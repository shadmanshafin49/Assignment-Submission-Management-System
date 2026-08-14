using Ams.Domain.Enums;

namespace Ams.Domain.Entities;

public class Submission
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AssignmentId { get; set; }
    public Assignment Assignment { get; set; } = null!;

    public Guid StudentId { get; set; }
    public User Student { get; set; } = null!;

    /// <summary>
    /// The typed answer. May be empty when the student answered entirely with attached files —
    /// a photographed page of handwriting is the normal case for a maths or drawing assignment.
    /// The service requires text or at least one file, not necessarily both.
    /// </summary>
    public string AnswerText { get; set; } = string.Empty;

    public SubmissionStatus Status { get; set; } = SubmissionStatus.Submitted;

    public DateTimeOffset SubmittedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    // ---- Grading ----
    public int? Marks { get; set; }
    public string? Feedback { get; set; }
    public Guid? GradedByTeacherId { get; set; }
    public User? GradedByTeacher { get; set; }
    public DateTimeOffset? GradedAt { get; set; }

    // Navigation
    public ICollection<SubmissionAttachment> Attachments { get; set; } = [];

    // ---- Domain rules -------------------------------------------------------

    public bool IsGraded => Status == SubmissionStatus.Graded;

    /// <summary>
    /// A student may edit their own submission when all of the following hold:
    /// the assignment permits resubmission, the work has not already been graded, and the
    /// deadline has not passed. A submission explicitly returned for revision bypasses the
    /// deadline check, since the teacher deliberately re-opened it.
    /// </summary>
    public bool CanBeEditedByStudent(Assignment assignment, DateTimeOffset now)
    {
        if (Status == SubmissionStatus.ReturnedForRevision)
            return true;

        if (!assignment.AllowResubmission)
            return false;

        if (IsGraded)
            return false;

        return !assignment.IsPastDeadline(now);
    }
}
