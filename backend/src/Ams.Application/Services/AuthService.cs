using Ams.Application.Abstractions;
using Ams.Application.Common;
using Ams.Application.Dtos;
using Ams.Domain.Entities;
using Ams.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ams.Application.Services;

public class AuthService(
    IAppDbContext db,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    ILogger<AuthService> logger) : IAuthService
{
    private const int RefreshTokenLifetimeDays = 7;

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        // Deliberately identical failure for "no such user" and "wrong password" so the
        // endpoint cannot be used to enumerate registered email addresses.
        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            logger.LogWarning("Failed login attempt for {Email}", email);
            throw new ForbiddenException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            logger.LogWarning("Login attempt on deactivated account {UserId}", user.Id);
            throw new ForbiddenException("This account has been deactivated.");
        }

        logger.LogInformation("User {UserId} ({Role}) logged in", user.Id, user.Role);
        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow();
        var hash = tokenService.HashRefreshToken(request.RefreshToken);

        var stored = await db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct)
            ?? throw new ForbiddenException("Invalid refresh token.");

        if (!stored.IsActive(now))
            throw new ForbiddenException("Refresh token has expired or been revoked.");

        if (!stored.User.IsActive)
            throw new ForbiddenException("This account has been deactivated.");

        // Rotate: the presented token is burned before a new one is issued.
        stored.RevokedAt = now;

        return await IssueTokensAsync(stored.User, ct);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        var hash = tokenService.HashRefreshToken(refreshToken);
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (stored is not null && stored.RevokedAt is null)
        {
            stored.RevokedAt = timeProvider.GetUtcNow();
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<UserDto> GetCurrentUserAsync(CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == currentUser.UserId, ct)
            ?? throw new NotFoundException("User not found.");

        return user.ToDto();
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == currentUser.UserId, ct)
            ?? throw new NotFoundException("User not found.");

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new ForbiddenException("Current password is incorrect.");

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        user.UpdatedAt = timeProvider.GetUtcNow();

        // Changing a password invalidates every outstanding session.
        var tokens = await db.RefreshTokens
            .Where(t => t.UserId == user.Id && t.RevokedAt == null)
            .ToListAsync(ct);
        foreach (var t in tokens)
            t.RevokedAt = timeProvider.GetUtcNow();

        await db.SaveChangesAsync(ct);
        logger.LogInformation("User {UserId} changed their password", user.Id);
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow();
        var (accessToken, expiresAt) = tokenService.CreateAccessToken(user);
        var (refreshToken, refreshHash) = tokenService.CreateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshHash,
            CreatedAt = now,
            ExpiresAt = now.AddDays(RefreshTokenLifetimeDays)
        });

        await db.SaveChangesAsync(ct);

        return new AuthResponse(accessToken, expiresAt, refreshToken, user.ToDto());
    }
}
