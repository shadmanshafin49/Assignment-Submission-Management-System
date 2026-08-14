using Ams.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ams.Infrastructure.Persistence.Configurations;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> b)
    {
        b.ToTable("courses");
        b.HasKey(x => x.Id);

        b.Property(x => x.Code).IsRequired().HasMaxLength(20);
        b.Property(x => x.AcademicYear).IsRequired().HasMaxLength(10);
        b.Property(x => x.IsActive).IsRequired();
        b.Property(x => x.CreatedAt).IsRequired();

        b.HasOne(x => x.ClassRoom)
            .WithMany(c => c.Courses)
            .HasForeignKey(x => x.ClassRoomId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Subject)
            .WithMany(s => s.Courses)
            .HasForeignKey(x => x.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Teacher)
            .WithMany(u => u.Courses)
            .HasForeignKey(x => x.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        // A subject is offered to a class exactly once in a year, and the code is its identity.
        b.HasIndex(x => new { x.ClassRoomId, x.SubjectId, x.AcademicYear }).IsUnique();
        b.HasIndex(x => new { x.Code, x.AcademicYear }).IsUnique();
        b.HasIndex(x => x.TeacherId);
    }
}

public class RoutinePeriodConfiguration : IEntityTypeConfiguration<RoutinePeriod>
{
    public void Configure(EntityTypeBuilder<RoutinePeriod> b)
    {
        b.ToTable("routine_periods");
        b.HasKey(x => x.Id);

        b.Property(x => x.Day).HasConversion<string>().IsRequired().HasMaxLength(15);
        b.Property(x => x.PeriodIndex).IsRequired();
        b.Property(x => x.CreatedAt).IsRequired();

        b.HasOne(x => x.ClassRoom)
            .WithMany(c => c.RoutinePeriods)
            .HasForeignKey(x => x.ClassRoomId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Course)
            .WithMany(c => c.RoutinePeriods)
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        // The same course cannot be booked twice into one slot. "One course per slot" is the
        // narrower rule and lives in the service, because the religion period legitimately holds
        // two courses at once — see RoutinePeriod's remarks.
        b.HasIndex(x => new { x.ClassRoomId, x.Day, x.PeriodIndex, x.CourseId }).IsUnique();
        b.HasIndex(x => new { x.ClassRoomId, x.Day, x.PeriodIndex });
        b.HasIndex(x => x.CourseId);
    }
}
