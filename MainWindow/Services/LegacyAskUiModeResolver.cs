using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis;
using Ask.DataBase.Engine.Static.Devices;
using Ask.DataBase.Provider.Context;

namespace MainWindowProgram.Services;

/// <summary>
/// Определяет, должна ли оболочка открывать legacy-разделы тестера АСК вместо разделов тестера АСКМ.
/// </summary>
public static class LegacyAskUiModeResolver
{
  /// <summary>
  /// Проверяет наличие в конфигурации стойки старого тестера АСК.
  /// </summary>
  /// <returns>Признак наличия стойки АСК.</returns>
  public static bool ShouldUseLegacyAsk()
  {
    try
    {
      if (ChassisManagers.GetAllAsync().GetAwaiter().GetResult().Any(IsLegacyAskChassis))
      {
        return true;
      }

      using var context = new AppDbContext();
      return context.ChassisManagers.Any(chassis =>
        chassis.Name == "Тестер АСК"
        || (chassis.DeviceClass != null && chassis.DeviceClass.EndsWith(".ManagerASKMKI")));
    }
    catch
    {
      return false;
    }
  }

  /// <summary>
  /// Определяет, является ли стойка старым тестером АСК.
  /// </summary>
  /// <param name="chassis">Стойка из конфигурации оборудования.</param>
  /// <returns>Признак стойки АСК.</returns>
  public static bool IsLegacyAskChassis(IChassisManager chassis)
  {
    return string.Equals(chassis.Name, "Тестер АСК", StringComparison.OrdinalIgnoreCase)
      || (chassis.DeviceClass?.EndsWith(".ManagerASKMKI", StringComparison.Ordinal) ?? false);
  }
}
