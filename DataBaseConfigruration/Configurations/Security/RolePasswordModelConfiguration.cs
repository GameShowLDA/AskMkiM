using Ask.Core.Shared.Entity.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataBaseConfiguration.Configurations.Security
{
  internal sealed class RolePasswordModelConfiguration : IEntityTypeConfiguration<RolePasswordModel>
  {
    public void Configure(EntityTypeBuilder<RolePasswordModel> builder)
    {
      builder.HasIndex(x => x.Role).IsUnique();

      builder.Property(x => x.Password)
        .IsRequired()
        .HasMaxLength(256);
    }
  }
}
