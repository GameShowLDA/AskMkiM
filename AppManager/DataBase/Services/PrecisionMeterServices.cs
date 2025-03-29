using AppManager.DataBase.Models;
using AppManager.DataBase.Repositories;
using NewCore.Base.Device;
using NewCore.Base.Interface.Main;

namespace AppManager.DataBase.Services
{
  /// <summary>
  /// Сервис для работы с моделями точных измерителей из базы данных.
  /// Предоставляет методы для работы с устройствами, полученными из БД,
  /// преобразуя их из моделей данных в объекты.
  /// </summary>
  public class PrecisionMeterServices : Service<IPrecisionMeter>
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="PrecisionMeterServices"/>.
    /// </summary>
    public PrecisionMeterServices() : base(AppManager.Config.SystemStateManager.Context)
    { }

    /// <summary>
    /// Получает список всех устройств, привязанных к определенному шасси.
    /// </summary>
    /// <param name="numberChassis">Номер шасси.</param>
    /// <returns>Список точных измерителей.</returns>
    public List<IPrecisionMeter> GetDevicesByNumberChassis(int numberChassis)
    {
      var data = _context.Set<PrecisionMeterEntity>()
                         .Where(device => device.NumberChassis == numberChassis)
                         .ToList();

      var result = data
          .OfType<IPrecisionMeter>()
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
    public List<PrecisionMeterEntity> GetEntitiesByNumberChassis(int numberChassis)
    {
      return _context.Set<PrecisionMeterEntity>()
                     .Where(device => device.NumberChassis == numberChassis)
                     .ToList();
    }
  }
}
