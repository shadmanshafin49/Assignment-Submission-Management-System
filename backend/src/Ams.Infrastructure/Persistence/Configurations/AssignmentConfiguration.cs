using Ams.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ams.Infrastructure.Persistence.Configurations;

public class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> b)
    {
        b.ToTable("assignments");
        b.HasKey(x => x.Id);

        b.Property(x => x.Title).IsRequired().HasMaxLength(200);
        b.Property(x => x.Description).IsRequired().HasMaxLength(5000);
        b.Property(x => x.ChapterOrLesson).HasMaxLength(200);
        b.Property(x => x.Type).HasConversion<string>().IsRequired().HasMaxLength(40);
        b.Property(x => x.WeekNumber).IsRequired();
        b.Property(x => x.AssignedOn).IsRequired();
        b.Property(x => x.Deadline).IsRequired();
        b.Property(x => x.MaxMarks).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().IsRequired().HasMaxLength(20);
        b.Property(x => x.AllowLateSubmission).IsRequired();
        b.Property(x => x.AllowResubmission).IsRequired();
        b.Property(x => x.AllowComments).IsRequired();
        b.Property(x => x.CreatedAt).IsRequired();

        b.HasOne(x => x.Course)
            .WithMany(c => c.Assignments)
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.CreatedByTeacher)
            .WithMany(u => u.CreatedAssignments)
            .HasForeignKey(x => x.CreatedByTeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        // Drives the student feed: published work on a course, and the teacher's own list.
        b.HasIndex(x => new { x.CourseId, x.Status });
        b.HasIndex(x => x.CreatedByTeacherId);
        b.HasIndex(x => x.Deadline);
        b.HasIndex(x => x.WeekNumber);
    }
}

public class AssignmentAttachmentConfiguration : IEntityTypeConfiguration<AssignmentAttachment>
{
    public void Configure(EntityTypeBuilder<AssignmentAttachment> b)
    {
        b.ToTable("assignment_attachments");
        b.HasKey(x => x.Id);

        b.Property(x => x.FileName).IsRequired().HasMaxLength(260);
        b.Property(x => x.StorageKey).IsRequired().HasMaxLength(260);
        b.Property(x => x.ContentType).IsRequired().HasMaxLength(150);
        b.Property(x => x.SizeBytes).IsRequired();
        b.Property(x => x.UploadedAt).IsRequired();

        // Deleting an assignment (only ever possible while it has no submissions) takes its
        // attachment rows with it; the files are removed by the storage service in the same call.
        b.HasOne(x => x.Assignment)
            .WithMany(a => a.Attachments)
            .HasForeignKey(x => x.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.AssignmentId);
        b.HasIndex(x => x.StorageKey).IsUnique();
    }
}

public class SubmissionAttachmentConfiguration : IEntityTypeConfiguration<SubmissionAttachment>
{
    public void Configure(EntityTypeBuilder<SubmissionAttachment> b)
    {
        b.ToTable("submission_attachments");
        b.HasKey(x => x.Id);

        b.Property(x => x.FileName).IsRequired().HasMaxLength(260);
        b.Property(x => x.StorageKey).IsRequired().HasMaxLength(260);
        b.Property(x => x.ContentType).IsRequired().HasMaxLength(150);
        b.Property(x => x.SizeBytes).IsRequired();
        b.Property(x => x.UploadedAt).IsRequired();

        b.HasOne(x => x.Submission)
            .WithMany(s => s.Attachments)
            .HasForeignKey(x => x.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.SubmissionId);
        b.HasIndex(x => x.StorageKey).IsUnique();
    }
}

public class AssignmentCommentConfiguration : IEntityTypeConfiguration<AssignmentComment>
{
    public void Configure(EntityTypeBuilder<AssignmentComment> b)
    {
        b.ToTable("assignment_comments");
        b.HasKey(x => x.Id);

        b.Property(x => x.Body).IsRequired().HasMaxLength(2000);
        b.Property(x => x.CreatedAt).IsRequired();

        b.HasOne(x => x.Assignment)
            .WithMany(a => a.Comments)
            .HasForeignKey(x => x.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Author)
            .WithMany(u => u.Comments)
            .HasForeignKey(x => x.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        // The thread is always read in posting order for one assignment.
        b.HasIndex(x => new { x.AssignmentId, x.CreatedAt });
    }
}
