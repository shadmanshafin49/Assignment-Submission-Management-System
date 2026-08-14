using Ams.Application.Dtos;
using Ams.Domain.Entities;
using Ams.Domain.Enums;
using Ams.Domain.Exceptions;
using Ams.Infrastructure.Security;
using Ams.UnitTests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using AuthService = Ams.Application.Services.AuthService;

namespace Ams.UnitTests.Auth;

public class AuthenticationTests : IDisposable
{
    private const string Password = "Password@123";

    private readonly TestContext _ctx = new();
    private readonly PasswordHasher _hasher = new();
    private readonly TokenService _tokens;
    private readonly User _student;

    public AuthenticationTests()
    {
        _tokens = new TokenService(
            Options.Create(new JwtOptions
            {
                // Test-only signing key; the real one comes from configuration.
                Key = "test-signing-key-that-is-long-enough-for-hs256",
                Issuer = "AmsApi",
                Audience = "AmsFrontend",
                AccessTokenMinutes = 60
            }),
            _ctx.Clock);

        _student = new User
        {
            FullName = "Alice Student",
            Email = "alice@school.test",
            PasswordHash = _hasher.Hash(Password),
            Role = UserRole.Student,
            IsActive = true,
            CreatedAt = TestContext.Now
        };

        _ctx.Db.Users.Add(_student);
        _ctx.Db.SaveChanges();
        _ctx.ClearTracking();
    }

    public void Dispose() => _ctx.Dispose();

    private AuthService Service(Guid? callerId = null) =>
        new(_ctx.Context,
            _hasher,
            _tokens,
            TestContext.As(callerId ?? _student.Id, UserRole.Student, _student.Email),
            _ctx.Clock,
            NullLogger<AuthService>.Instance);

    [Fact]
    public async Task Correct_credentials_return_a_token_and_the_users_profile()
    {
        var result = await Service().LoginAsync(new LoginRequest("alice@school.test", Password));

        result.AccessToken.ShouldNotBeNullOrWhiteSpace();
        result.RefreshToken.ShouldNotBeNullOrWhiteSpace();
        result.User.Email.ShouldBe("alice@school.test");
        result.User.Role.ShouldBe(UserRole.Student);
    }

    [Fact]
    public async Task Login_is_case_insensitive_for_the_email()
    {
        var result = await Service().LoginAsync(new LoginRequest("ALICE@School.test", Password));

        result.User.Email.ShouldBe("alice@school.test");
    }

    [Fact]
    public async Task A_wrong_password_is_rejected()
    {
        await Should.ThrowAsync<ForbiddenException>(
            () => Service().LoginAsync(new LoginRequest("alice@school.test", "WrongPassword1")));
    }

    [Fact]
    public async Task An_unknown_email_and_a_wrong_password_fail_identically()
    {
        // A different message for each would let an attacker enumerate registered accounts.
        var unknown = await Should.ThrowAsync<ForbiddenException>(
            () => Service().LoginAsync(new LoginRequest("nobody@school.test", Password)));

        var wrongPassword = await Should.ThrowAsync<ForbiddenException>(
            () => Service().LoginAsync(new LoginRequest("alice@school.test", "WrongPassword1")));

        unknown.Message.ShouldBe(wrongPassword.Message);
    }

    [Fact]
    public async Task A_deactivated_account_cannot_log_in()
    {
        _student.IsActive = false;
        _ctx.Db.Users.Update(_student);
        await _ctx.Db.SaveChangesAsync();
        _ctx.ClearTracking();

        var ex = await Should.ThrowAsync<ForbiddenException>(
            () => Service().LoginAsync(new LoginRequest("alice@school.test", Password)));

        ex.Message.ShouldContain("deactivated");
    }

    [Fact]
    public async Task A_refresh_token_can_be_exchanged_for_a_new_access_token()
    {
        var login = await Service().LoginAsync(new LoginRequest("alice@school.test", Password));
        _ctx.ClearTracking();

        var refreshed = await Service().RefreshAsync(new RefreshTokenRequest(login.RefreshToken));

        refreshed.AccessToken.ShouldNotBeNullOrWhiteSpace();
        refreshed.RefreshToken.ShouldNotBe(login.RefreshToken);
    }

    [Fact]
    public async Task A_refresh_token_cannot_be_used_twice()
    {
        // Rotation: presenting a token burns it, so a stolen copy is useless after first use.
        var login = await Service().LoginAsync(new LoginRequest("alice@school.test", Password));
        _ctx.ClearTracking();

        await Service().RefreshAsync(new RefreshTokenRequest(login.RefreshToken));
        _ctx.ClearTracking();

        await Should.ThrowAsync<ForbiddenException>(
            () => Service().RefreshAsync(new RefreshTokenRequest(login.RefreshToken)));
    }

    [Fact]
    public async Task An_expired_refresh_token_is_rejected()
    {
        var login = await Service().LoginAsync(new LoginRequest("alice@school.test", Password));
        _ctx.ClearTracking();

        // Refresh tokens live 7 days.
        _ctx.Clock.Advance(TimeSpan.FromDays(8));

        var ex = await Should.ThrowAsync<ForbiddenException>(
            () => Service().RefreshAsync(new RefreshTokenRequest(login.RefreshToken)));

        ex.Message.ShouldContain("expired");
    }

    [Fact]
    public async Task Logging_out_revokes_the_refresh_token()
    {
        var login = await Service().LoginAsync(new LoginRequest("alice@school.test", Password));
        _ctx.ClearTracking();

        await Service().LogoutAsync(login.RefreshToken);
        _ctx.ClearTracking();

        await Should.ThrowAsync<ForbiddenException>(
            () => Service().RefreshAsync(new RefreshTokenRequest(login.RefreshToken)));
    }

    [Fact]
    public async Task Changing_a_password_revokes_every_outstanding_session()
    {
        var login = await Service().LoginAsync(new LoginRequest("alice@school.test", Password));
        _ctx.ClearTracking();

        await Service().ChangePasswordAsync(new ChangePasswordRequest(Password, "NewPassword@456"));
        _ctx.ClearTracking();

        await Should.ThrowAsync<ForbiddenException>(
            () => Service().RefreshAsync(new RefreshTokenRequest(login.RefreshToken)));
    }

    [Fact]
    public async Task Changing_a_password_requires_the_current_one()
    {
        await Should.ThrowAsync<ForbiddenException>(
            () => Service().ChangePasswordAsync(
                new ChangePasswordRequest("NotMyPassword1", "NewPassword@456")));
    }

    [Fact]
    public async Task The_new_password_works_on_the_next_login()
    {
        await Service().ChangePasswordAsync(new ChangePasswordRequest(Password, "NewPassword@456"));
        _ctx.ClearTracking();

        var result = await Service().LoginAsync(
            new LoginRequest("alice@school.test", "NewPassword@456"));

        result.User.Email.ShouldBe("alice@school.test");
    }
}
