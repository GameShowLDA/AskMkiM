using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using DataBaseConfiguration.Models.Hotkey;

namespace DataBaseConfiguration.Configurations.Hotkey
{
  internal class FileHotkeyEntityConfiguration : IEntityTypeConfiguration<FileHotkeyEntity>
  {
    /// <summary>
    /// Настройки таблицы настроек пробойной установки в базе данных.
    /// </summary>
    /// <param name="builder">Объект для настройки сущности настроек пробойной установки в базе данных.</param>
    public void Configure(EntityTypeBuilder<FileHotkeyEntity> builder)
    {
      builder.HasKey(x => x.Id);
    }
  }
}
