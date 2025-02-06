using AppConfig.DataBase.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppConfig.DataBase.Configurations
{
  public class SwitchingDeviceConfiguration : IEntityTypeConfiguration<SwitchingDeviceEntity>
  {
    /// <summary>
    /// Настройки таблицы настроек УКШ в базе данных.
    /// </summary>
    /// <param name="builder">Объект для настройки сущности настроек УКШ в базе данных.</param>
    public void Configure(EntityTypeBuilder<SwitchingDeviceEntity> builder)
    {
      builder.HasKey(x => x.Id);
    }
  }
}
