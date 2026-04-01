using Ask.Core.Shared.Entity.Settings;
using Ask.Core.Shared.Entity.UI;
using Microsoft.EntityFrameworkCore;

namespace DataBaseConfiguration.Context
{
  public partial class AppDbContext
  {
    /// <summary>
    /// Таблица настроек протокола.
    /// </summary>
    public DbSet<SettingsProtocolModel> SettingsProtocol { get; set; }

    /// <summary>
    /// Таблица настроек выполнения.
    /// </summary>
    public DbSet<SettingsExecutionModel> Execution { get; set; }

    /// <summary>
    /// Таблица горячих клавиш файлов.
    /// </summary>
    public DbSet<FileHotkeyEntity> FileHotKeys { get; set; }

    /// <summary>
    /// Таблица настроек интерфейса программы
    /// </summary>
    public DbSet<UserInterfaceModel> UserInterface { get; set; }

    /// <summary>
    /// Таблица настроек отображения информации об устройствах.
    /// </summary>
    public DbSet<DeviceDisplaySettingsModel> DeviceDisplaySettings { get; set; }

    /// <summary>
    /// Таблица паролей системных ролей.
    /// </summary>
    public DbSet<RolePasswordModel> RolePasswords { get; set; }
  }
}
