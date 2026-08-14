using Ams.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ams.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(x => x.Id);

        b.Property(x => x.FullName).IsRequired().HasMaxLength(150);
        b.Property(x => x.FullNameEn).IsRequired().HasMaxLength(150);
        b.Property(x => x.Email).IsRequired().HasMaxLength(256);
        b.Property(x => x.PasswordHash).IsRequired().HasMaxLength(512);
        b.Property(x => x.Designation).HasMaxLength(120);

        // Stored as text so the database is readable without decoding integers.
        b.Property(x => x.Role).HasConversion<string>().IsRequired().HasMaxLength(20);
        b.Property(x => x.Faith).HasConversion<string>().HasMaxLength(20);

        b.Property(x => x.IsActive).IsRequired();
        b.Property(x => x.CreatedAt).IsRequired();

        // Emails are normalised to lower-case before save, so a plain unique index suffices.
        b.HasIndex(x => x.Email).IsUnique();
        b.HasIndex(x => x.Role);
    }
}
