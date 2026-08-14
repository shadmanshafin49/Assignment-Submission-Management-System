using Ams.Domain.Enums;

namespace Ams.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Bangla display name, e.g. "মোঃ রেজাউল করিম".</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>English name, for the roll sheet and anywhere Latin script reads better.</summary>
    public string FullNameEn { get; set; } = string.Empty;

    /// <summary>Login identifier. Stored lower-cased and uniquely indexed.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>PBKDF2 hash in the format <c>iterations.salt.subkey</c> (all base64).</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    /// <summary>
    /// Teacher's designation, e.g. "সহকারী শিক্ষক (গণিত)". Null for students and admins.
    /// </summary>
    public string? Designation { get; set; }

    /// <summary>
    /// A student's religion stream, which decides which ধর্ম ও নৈতিক শিক্ষা course they take.
    /// Null for teachers and admins — they are not enrolled in anything.
    /// </summary>
    public FaithGroup? Faith { get; set; }

    /// <summary>Soft-disable switch. Inactive users are rejected at login.</summary>
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation
    public ICollection<Enrollment> Enrollments { get; set; } = [];
    public ICollection<Course> Courses { get; set; } = [];
    public ICollection<Assignment> CreatedAssignments { get; set; } = [];
    public ICollection<Submission> Submissions { get; set; } = [];
    public ICollection<AssignmentComment> Comments { get; set; } = [];
}
