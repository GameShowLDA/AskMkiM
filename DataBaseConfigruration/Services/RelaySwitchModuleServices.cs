using DataBaseConfiguration.Models;
using NewCore.Base.Interface.Main;

namespace DataBaseConfiguration.Services
{
  /// <summary>
  /// Сервис для работы с моделями модуля коммутации реле из базы данных.
  /// Предоставляет методы для работы с устройствами, полученными из БД,
  /// преобразуя их из моделей данных в объекты.
  /// </summary>
  public class RelaySwitchModuleServices : Service<IRelaySwitchModule>
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="RelaySwitchModuleServices"/>.
    /// </summary>
    public RelaySwitchModuleServices() : base(Configurations.DataBaseConfig.Context)
    { }

    /// <summary>
    /// Получает список всех устройств, привязанных к определенному шасси.
    /// </summary>
    /// <param name="numberChassis">Номер шасси.</param>
    /// <returns>Список модулей коммутации реле.</returns>
    public List<IRelaySwitchModule> GetDevicesByNumberChassis(int numberChassis)
    {
      var data = _context.Set<RelaySwitchModuleEntity>()
                         .Where(device => device.NumberChassis == numberChassis)
                         .ToList();

      var result = data
          .OfType<IRelaySwitchModule>()
          .Select(GetDeviceInstance)
          .Where(instance => instance != null)
          .ToList();

      return result;
    }

    /// <summary>
    /// Получает список сущностей пробойных установок, привязанных к определённому шасси.
    /// </summary>
    /// <param name="numberChassis">Номер шасси.</param>
    /// <returns>Список <see cref="BreakdownTesterEntity"/>.</returns>
    public List<RelaySwitchModuleEntity> GetEntitiesByNumberChassis(int numberChassis)
    {
      return _context.Set<RelaySwitchModuleEntity>()
                     .Where(device => device.NumberChassis == numberChassis)
                     .ToList();
    }
  }
}
