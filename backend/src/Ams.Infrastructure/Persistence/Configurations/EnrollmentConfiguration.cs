using Ams.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ams.Infrastructure.Persistence.Configurations;

public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> b)
    {
        b.ToTable("enrollments");
        b.HasKey(x => x.Id);

        b.Property(x => x.RollNumber).IsRequired();
        b.Property(x => x.CreatedAt).IsRequired();

        b.HasOne(x => x.Student)
            .WithMany(u => u.Enrollments)
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.ClassRoom)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(x => x.ClassRoomId)
            .OnDelete(DeleteBehavior.Cascade);

        // A student cannot be enrolled in the same class twice.
        b.HasIndex(x => new { x.StudentId, x.ClassRoomId }).IsUnique();

        // Two students cannot share a roll number in the same class.
        b.HasIndex(x => new { x.ClassRoomId, x.RollNumber }).IsUnique();
    }
}
