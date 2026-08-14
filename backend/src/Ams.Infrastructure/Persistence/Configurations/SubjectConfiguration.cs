using Ams.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ams.Infrastructure.Persistence.Configurations;

public class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> b)
    {
        b.ToTable("subjects");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).IsRequired().HasMaxLength(120);
        b.Property(x => x.NameEn).IsRequired().HasMaxLength(120);
        b.Property(x => x.Code).IsRequired().HasMaxLength(10);
        b.Property(x => x.TextbookName).HasMaxLength(120);
        b.Property(x => x.FullMarks).IsRequired();
        b.Property(x => x.WeeklyPeriods).IsRequired();
        b.Property(x => x.FaithGroup).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.IsOptionalGroup).IsRequired();
        b.Property(x => x.DisplayOrder).IsRequired();
        b.Property(x => x.IsActive).IsRequired();
        b.Property(x => x.CreatedAt).IsRequired();

        // The board subject code is the subject's real-world identity.
        b.HasIndex(x => x.Code).IsUnique();
    }
}

public class SubjectAssignmentTypeConfiguration : IEntityTypeConfiguration<SubjectAssignmentType>
{
    public void Configure(EntityTypeBuilder<SubjectAssignmentType> b)
    {
        b.ToTable("subject_assignment_types");
        b.HasKey(x => x.Id);

        b.Property(x => x.Type).HasConversion<string>().IsRequired().HasMaxLength(40);

        b.HasOne(x => x.Subject)
            .WithMany(s => s.AllowedAssignmentTypes)
            .HasForeignKey(x => x.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.SubjectId, x.Type }).IsUnique();
    }
}
