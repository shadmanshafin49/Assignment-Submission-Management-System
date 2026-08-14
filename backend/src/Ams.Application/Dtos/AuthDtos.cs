using Ams.Domain.Enums;

namespace Ams.Application.Dtos;

public record LoginRequest(string Email, string Password);

public record RefreshTokenRequest(string RefreshToken);

public record AuthResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    UserDto User);

// UserDto lives in AdminDtos.cs — the same shape is returned by /api/auth/me and by the
// admin user list, so there is exactly one definition of "what a user looks like over the wire".

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
