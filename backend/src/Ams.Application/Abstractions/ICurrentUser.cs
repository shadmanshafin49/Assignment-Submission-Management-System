using Ams.Domain.Enums;

namespace Ams.Application.Abstractions;

/// <summary>
/// The authenticated caller, resolved from the JWT by the API layer. Services take
/// identity from here rather than trusting ids supplied in request bodies — a student
/// cannot submit "as" someone else by editing the payload.
/// </summary>
public interface ICurrentUser
{
    Guid UserId { get; }
    string Email { get; }
    UserRole Role { get; }
    bool IsAuthenticated { get; }
}
