using System.Security.Claims;
using Ams.Application.Abstractions;
using Ams.Domain.Enums;

namespace Ams.Api.Security;

/// <summary>
/// Resolves the caller from the validated JWT. Every service takes identity from here, so a
/// request body can never impersonate another user by supplying a different id.
/// </summary>
public class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid UserId
    {
        get
        {
            var raw = Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
        }
    }

    public string Email => Principal?.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

    public UserRole Role
    {
        get
        {
            var raw = Principal?.FindFirstValue(ClaimTypes.Role);
            return Enum.TryParse<UserRole>(raw, out var role) ? role : default;
        }
    }
}
