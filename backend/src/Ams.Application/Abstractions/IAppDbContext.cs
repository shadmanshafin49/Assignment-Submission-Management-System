using Ams.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ams.Application.Abstractions;

/// <summary>
/// Persistence surface the application services depend on. Declaring it here (rather than
/// referencing <c>AppDbContext</c> directly) keeps the business rules independent of the
/// Infrastructure project and lets tests swap in a SQLite-backed context.
/// </summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<ClassRoom> ClassRooms { get; }
    DbSet<Subject> Subjects { get; }
    DbSet<SubjectAssignmentType> SubjectAssignmentTypes { get; }
    DbSet<Course> Courses { get; }
    DbSet<Enrollment> Enrollments { get; }
    DbSet<RoutinePeriod> RoutinePeriods { get; }
    DbSet<Assignment> Assignments { get; }
    DbSet<AssignmentAttachment> AssignmentAttachments { get; }
    DbSet<AssignmentComment> AssignmentComments { get; }
    DbSet<Submission> Submissions { get; }
    DbSet<SubmissionAttachment> SubmissionAttachments { get; }
    DbSet<AppSetting> AppSettings { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
