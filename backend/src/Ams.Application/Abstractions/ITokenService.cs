using Ams.Domain.Entities;

namespace Ams.Application.Abstractions;

public interface ITokenService
{
    /// <summary>Issues a signed JWT carrying the user's id, email and role.</summary>
    (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(User user);

    /// <summary>Generates a cryptographically random refresh token and its storable hash.</summary>
    (string Token, string TokenHash) CreateRefreshToken();

    /// <summary>Hashes a refresh token for lookup against the stored value.</summary>
    string HashRefreshToken(string token);
}
