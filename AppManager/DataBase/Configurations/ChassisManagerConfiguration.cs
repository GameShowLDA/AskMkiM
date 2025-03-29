using AppManager.DataBase.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppManager.DataBase.Configurations
{
  public class ChassisManagerConfiguration : IEntityTypeConfiguration<ChassisManagerEntity>
  {
    /// <summary>
    /// Настройки таблицы настроек менеджера шасси в базе данных.
    /// </summary>
    /// <param name="builder">Объект для настройки сущности настроек менеджера шасси в базе данных.</param>
    public void Configure(EntityTypeBuilder<ChassisManagerEntity> builder)
    {
      builder.HasKey(x => x.Id);
    }
  }
}
