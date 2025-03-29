using DataBaseConfiguration.Services;
using NewCore.Base.Interface.Main;

namespace Mode.TestSuite.Metrology
{
  /// <summary>
  /// Вспомогательный класс для работы с модулями МКР.
  /// </summary>
  public static class RelayModuleHelper
  {
    /// <summary>
    /// Получает модули МКР по номеру шасси и диапазону модулей.
    /// </summary>
    /// <param name="chassisNumber">Номер шасси.</param>
    /// <param name="startModule">Номер начального модуля.</param>
    /// <param name="endModule">Номер конечного модуля.</param>
    /// <returns>Список модулей, соответствующих диапазону.</returns>
    public static List<IRelaySwitchModule> GetModulesByRange(int chassisNumber, int startModule, int endModule)
    {
      var relayRepo = new RelaySwitchModuleServices();
      var allModules = relayRepo.GetDevicesByNumberChassis(chassisNumber);
      var filteredModules = allModules
          .Where(m => m.Number >= startModule && m.Number <= endModule)
          .ToList();

      return filteredModules;
    }
  }
}
