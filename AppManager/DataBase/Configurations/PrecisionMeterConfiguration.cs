using AppManager.DataBase.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppManager.DataBase.Configurations
{
  public class PrecisionMeterConfiguration : IEntityTypeConfiguration<PrecisionMeterEntity>
  {
    /// <summary>
    /// Настройки таблицы настроек точного измерителя в базе данных.
    /// </summary>
    /// <param name="builder">Объект для настройки сущности настроек точного измерителя в базе данных.</param>
    public void Configure(EntityTypeBuilder<PrecisionMeterEntity> builder)
    {
      builder.HasKey(x => x.Id);
    }
  }
}
