using AppManager.DataBase.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppManager.DataBase.Configurations
{
  public class BreakdownTesterConfiguration : IEntityTypeConfiguration<BreakdownTesterEntity>
  {
    /// <summary>
    /// Настройки таблицы настроек пробойной установки в базе данных.
    /// </summary>
    /// <param name="builder">Объект для настройки сущности настроек пробойной установки в базе данных.</param>
    public void Configure(EntityTypeBuilder<BreakdownTesterEntity> builder)
    {
      builder.HasKey(x => x.Id);
    }
  }
}
