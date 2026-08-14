namespace Ams.Domain.Enums;

/// <summary>
/// Lifecycle of an assignment. Students may only ever observe <see cref="Published"/> assignments.
/// </summary>
public enum AssignmentStatus
{
    Draft = 1,
    Published = 2
}
