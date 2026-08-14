using Ams.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ams.Infrastructure.Persistence.Configurations;

public class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
{
    public void Configure(EntityTypeBuilder<AppSetting> b)
    {
        b.ToTable("app_settings");
        b.HasKey(x => x.Key);

        b.Property(x => x.Key).HasMaxLength(100);
        b.Property(x => x.Value).IsRequired().HasMaxLength(500);
        b.Property(x => x.Description).IsRequired().HasMaxLength(500);
        b.Property(x => x.ValueType).IsRequired().HasMaxLength(20);
        b.Property(x => x.Category).IsRequired().HasMaxLength(60);
        b.Property(x => x.IsEditable).IsRequired();
        b.Property(x => x.DisplayOrder).IsRequired();
    }
}
