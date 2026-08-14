namespace Ams.Domain.Entities;

/// <summary>
/// A class the school teaches, e.g. "ষষ্ঠ শ্রেণি" (class 6).
/// Named <c>ClassRoom</c> because <c>class</c> is a reserved word in C#.
/// <para>
/// A class is a roster of students, not a timetable slot: what actually gets taught is a
/// <see cref="Course"/>, which pairs this class with one <see cref="Subject"/> and one teacher.
/// </para>
/// </summary>
public class ClassRoom
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Bangla display name, e.g. "ষষ্ঠ শ্রেণি".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>English name, e.g. "Class 6". Used where a Latin-script label reads better.</summary>
    public string NameEn { get; set; } = string.Empty;

    /// <summary>Short unique code, e.g. "C06". Course codes are built from this.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Numeric level: 6, 7 or 8. Drives ordering and the course-code digits.</summary>
    public int Level { get; set; }

    /// <summary>Section, e.g. "ক". Null when the class is not split into sections.</summary>
    public string? Section { get; set; }

    public string AcademicYear { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation
    public ICollection<Enrollment> Enrollments { get; set; } = [];
    public ICollection<Course> Courses { get; set; } = [];
    public ICollection<RoutinePeriod> RoutinePeriods { get; set; } = [];
}
