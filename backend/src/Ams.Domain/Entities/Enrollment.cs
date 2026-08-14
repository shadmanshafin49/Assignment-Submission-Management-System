namespace Ams.Domain.Entities;

/// <summary>
/// Places a student on a class roster, with their roll number.
/// <para>
/// A student's course list is derived rather than stored: it is every active course of the class
/// they are enrolled in, minus the religion courses that do not match their
/// <see cref="User.Faith"/>. Storing one row per student per course would mean 30 × 13 rows per
/// class kept in sync by hand, and every one of them would be derivable from these two facts.
/// </para>
/// </summary>
public class Enrollment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid StudentId { get; set; }
    public User Student { get; set; } = null!;

    public Guid ClassRoomId { get; set; }
    public ClassRoom ClassRoom { get; set; } = null!;

    /// <summary>রোল নম্বর — unique within the class, and how a teacher actually refers to a student.</summary>
    public int RollNumber { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
