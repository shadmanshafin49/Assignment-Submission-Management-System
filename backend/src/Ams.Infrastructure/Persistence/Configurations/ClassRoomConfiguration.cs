using Ams.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ams.Infrastructure.Persistence.Configurations;

public class ClassRoomConfiguration : IEntityTypeConfiguration<ClassRoom>
{
    public void Configure(EntityTypeBuilder<ClassRoom> b)
    {
        b.ToTable("class_rooms");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).IsRequired().HasMaxLength(120);
        b.Property(x => x.NameEn).IsRequired().HasMaxLength(120);
        b.Property(x => x.Code).IsRequired().HasMaxLength(30);
        b.Property(x => x.Level).IsRequired();
        b.Property(x => x.Section).HasMaxLength(20);
        b.Property(x => x.AcademicYear).IsRequired().HasMaxLength(20);
        b.Property(x => x.IsActive).IsRequired();
        b.Property(x => x.CreatedAt).IsRequired();

        b.HasIndex(x => x.Code).IsUnique();
        b.HasIndex(x => x.Level);
    }
}
