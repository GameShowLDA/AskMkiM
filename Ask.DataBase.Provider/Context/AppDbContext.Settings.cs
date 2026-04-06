using Ask.Core.Shared.DTO.Settings;
using Microsoft.EntityFrameworkCore;

namespace Ask.DataBase.Provider.Context
{
  public partial class AppDbContext
  {
    /// <summary>
    /// Таблица настроек протокола.
    /// </summary>
    public DbSet<SettingsProtocolDto> SettingsProtocol { get; set; }

    /// <summary>
    /// Таблица настроек выполнения.
    /// </summary>
    public DbSet<SettingsExecutionDto> Execution { get; set; }

    /// <summary>
    /// Таблица горячих клавиш файлов.
    /// </summary>
    public DbSet<FileHotkeyDto> FileHotKeys { get; set; }

    /// <summary>
    /// Таблица настроек интерфейса программы
    /// </summary>
    public DbSet<UserInterfaceDto> UserInterface { get; set; }

    /// <summary>
    /// Таблица настроек отображения информации об устройствах.
    /// </summary>
    public DbSet<DeviceDisplaySettingsDto> DeviceDisplaySettings { get; set; }
  }
}
