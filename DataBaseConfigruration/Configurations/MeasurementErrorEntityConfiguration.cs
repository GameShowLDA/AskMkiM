using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataBaseConfiguration.Models;
using DataBaseConfiguration.Models.Hotkey;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataBaseConfiguration.Configurations
{
  internal class MeasurementErrorEntityConfiguration : IEntityTypeConfiguration<MeasurementErrorEntity>
  {
    /// <summary>
    /// Настройки таблицы погрешности в базе данных.
    /// </summary>
    /// <param name="builder">Объект для настройки сущности настроек в базе данных.</param>
    public void Configure(EntityTypeBuilder<MeasurementErrorEntity> builder)
    {
      // Первичный ключ
      builder.HasKey(x => x.Id);

      // Уникальный индекс по Type
      builder.HasIndex(x => x.Type)
             .IsUnique();
    }
  }
}
