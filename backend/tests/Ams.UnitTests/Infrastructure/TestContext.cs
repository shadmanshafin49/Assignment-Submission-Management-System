using Ams.Application.Abstractions;
using Ams.Domain.Enums;
using Ams.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace Ams.UnitTests.Infrastructure;

/// <summary>
/// Per-test database and clock.
/// <para>
/// Uses SQLite in-memory rather than the EF Core InMemory provider because the rules under test
/// depend on real relational behaviour — the unique index on (AssignmentId, StudentId) that
/// enforces "one submission per student" is silently ignored by the InMemory provider, which
/// would make those tests pass for the wrong reason.
/// </para>
/// <para>
/// The clock is a <see cref="FakeTimeProvider"/> so every deadline rule is deterministic
/// instead of depending on when the suite happens to run.
/// </para>
/// </summary>
public sealed class TestContext : IDisposable
{
    /// <summary>Fixed "now" for all tests. Arbitrary but stable.</summary>
    public static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly SqliteConnection _connection;

    public AppDbContext Db { get; }
    public FakeTimeProvider Clock { get; }

    public TestContext()
    {
        // The database lives as long as the connection does, so it is held open for the test.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        Db = new AppDbContext(options);
        Db.Database.EnsureCreated();

        Clock = new FakeTimeProvider(Now);
    }

    public IAppDbContext Context => Db;

    /// <summary>Builds a caller identity for the given user, as the JWT layer would supply it.</summary>
    public static FakeCurrentUser As(Guid userId, UserRole role, string email = "user@school.test")
        => new(userId, role, email);

    /// <summary>Detaches all tracked entities so a later query re-reads from the database.</summary>
    public void ClearTracking() => Db.ChangeTracker.Clear();

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }
}

/// <summary>Stand-in for the JWT-derived caller identity.</summary>
public sealed class FakeCurrentUser(Guid userId, UserRole role, string email) : ICurrentUser
{
    public Guid UserId { get; } = userId;
    public string Email { get; } = email;
    public UserRole Role { get; } = role;
    public bool IsAuthenticated => true;
}
