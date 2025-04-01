using DataBaseConfiguration.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataBaseConfiguration.Configurations
{
  public class RelaySwitchModuleConfiguration : IEntityTypeConfiguration<RelaySwitchModuleEntity>
  {
    /// <summary>
    /// Настройки таблицы настроек МКРа в базе данных.
    /// </summary>
    /// <param name="builder">Объект для настройки сущности настроек МКРа в базе данных.</param>
    public void Configure(EntityTypeBuilder<RelaySwitchModuleEntity> builder)
    {
      builder.HasKey(x => x.Id);
    }
  }
}
