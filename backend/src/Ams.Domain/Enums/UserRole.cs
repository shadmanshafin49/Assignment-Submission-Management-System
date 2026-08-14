namespace Ams.Domain.Enums;

/// <summary>Application roles. Persisted as text so the database stays readable.</summary>
public enum UserRole
{
    Admin = 1,
    Teacher = 2,
    Student = 3
}
