using Ams.Infrastructure.Security;
using Shouldly;

namespace Ams.UnitTests.Auth;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void A_hashed_password_verifies_against_itself()
    {
        var hash = _hasher.Hash("Password@123");

        _hasher.Verify("Password@123", hash).ShouldBeTrue();
    }

    [Fact]
    public void A_different_password_does_not_verify()
    {
        var hash = _hasher.Hash("Password@123");

        _hasher.Verify("Password@124", hash).ShouldBeFalse();
    }

    [Fact]
    public void Verification_is_case_sensitive()
    {
        var hash = _hasher.Hash("Password@123");

        _hasher.Verify("password@123", hash).ShouldBeFalse();
    }

    [Fact]
    public void The_same_password_hashes_differently_each_time()
    {
        // Each hash gets a fresh random salt, so identical passwords are not detectable
        // by comparing stored hashes.
        var first = _hasher.Hash("Password@123");
        var second = _hasher.Hash("Password@123");

        first.ShouldNotBe(second);
        _hasher.Verify("Password@123", first).ShouldBeTrue();
        _hasher.Verify("Password@123", second).ShouldBeTrue();
    }

    [Fact]
    public void The_hash_never_contains_the_plaintext()
    {
        var hash = _hasher.Hash("Password@123");

        hash.ShouldNotContain("Password@123");
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-valid-hash")]
    [InlineData("1.2")]
    [InlineData("abc.def.ghi")]
    public void A_malformed_stored_hash_fails_closed_rather_than_throwing(string storedHash)
    {
        // A corrupt row must deny access, never crash the login endpoint.
        _hasher.Verify("Password@123", storedHash).ShouldBeFalse();
    }
}
