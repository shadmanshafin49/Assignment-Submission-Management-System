using Ams.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ams.Infrastructure.Persistence.Configurations;

public class SubmissionConfiguration : IEntityTypeConfiguration<Submission>
{
    public void Configure(EntityTypeBuilder<Submission> b)
    {
        b.ToTable("submissions");
        b.HasKey(x => x.Id);

        b.Property(x => x.AnswerText).IsRequired().HasMaxLength(20000);
        b.Property(x => x.Status).HasConversion<string>().IsRequired().HasMaxLength(30);
        b.Property(x => x.SubmittedAt).IsRequired();
        b.Property(x => x.Feedback).HasMaxLength(5000);

        b.HasOne(x => x.Assignment)
            .WithMany(a => a.Submissions)
            .HasForeignKey(x => x.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Student)
            .WithMany(u => u.Submissions)
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.GradedByTeacher)
            .WithMany()
            .HasForeignKey(x => x.GradedByTeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        // One submission per student per assignment — enforced at the database, not just in code.
        b.HasIndex(x => new { x.AssignmentId, x.StudentId }).IsUnique();
        b.HasIndex(x => x.StudentId);
    }
}
