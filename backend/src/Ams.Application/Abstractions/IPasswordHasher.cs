namespace Ams.Application.Abstractions;

public interface IPasswordHasher
{
    string Hash(string password);

    /// <summary>Constant-time verification of a plaintext password against a stored hash.</summary>
    bool Verify(string password, string passwordHash);
}
