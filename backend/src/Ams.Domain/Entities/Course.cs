namespace Ams.Domain.Entities;

/// <summary>
/// One subject taught to one class in one academic year — the thing that actually appears on a
/// routine, has a teacher, and carries assignments. <c>C06-101</c> is বাংলা ১ম পত্র for class 6.
/// <para>
/// This replaces the older "teacher is granted (class, subject)" join. Making the offering itself
/// an entity is what the school's own vocabulary describes: a course has a code, exactly one
/// teacher, a place in the weekly routine, and a stream of assignments. It also removes a whole
/// class of bug — an assignment can no longer point at a (class, subject) pair that nobody teaches.
/// </para>
/// </summary>
public class Course
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Course code in the school's own format: <c>C</c> + two-digit class + <c>-</c> + board
    /// subject code. <c>C07-108</c> is class 7 ইংরেজি ২য় পত্র. Unique per academic year.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    public Guid ClassRoomId { get; set; }
    public ClassRoom ClassRoom { get; set; } = null!;

    public Guid SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    /// <summary>
    /// The one teacher who takes this course. Nullable because an admin can create the offering
    /// before staffing it — an unstaffed course simply has nobody who can set work for it.
    /// </summary>
    public Guid? TeacherId { get; set; }
    public User? Teacher { get; set; }

    public string AcademicYear { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation
    public ICollection<Assignment> Assignments { get; set; } = [];
    public ICollection<RoutinePeriod> RoutinePeriods { get; set; } = [];

    /// <summary>Only the assigned teacher may set and mark work on this course.</summary>
    public bool IsTaughtBy(Guid userId) => TeacherId is { } t && t == userId;

    /// <summary>Builds the canonical course code for a class level and board subject code.</summary>
    public static string BuildCode(int classLevel, string subjectCode)
        => $"C{classLevel:D2}-{subjectCode}";
}
