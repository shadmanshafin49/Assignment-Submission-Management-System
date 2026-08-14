namespace Ams.Domain.Enums;

/// <summary>
/// Lifecycle of a student submission.
/// </summary>
public enum SubmissionStatus
{
    /// <summary>Submitted on or before the deadline, awaiting grading.</summary>
    Submitted = 1,

    /// <summary>Submitted after the deadline (only possible when the assignment allows late work).</summary>
    Late = 2,

    /// <summary>Graded by the owning teacher; marks and feedback are final unless returned.</summary>
    Graded = 3,

    /// <summary>
    /// Teacher sent the work back for revision. This re-opens editing for the student even
    /// after the deadline has passed, and is the only escape hatch from a locked submission.
    /// </summary>
    ReturnedForRevision = 4
}
