using AppConfig.DataBase.Models;
using AppConfig.DataBase.Repositories;
using NewCore.Base.Device;
using NewCore.Base.Interface.Main;

namespace AppConfig.DataBase.Services
{
  /// <summary>
  /// Сервис для работы с моделями быстрых ихмерителей из базы данных.
  /// Предоставляет методы для работы с устройствами, полученными из БД,
  /// преобразуя их из моделей данных в объекты.
  /// </summary>
  public class FastMeterServices : Service<IFastMeter>
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="FastMeterServices"/>.
    /// </summary>
    public FastMeterServices() : base(AppConfig.Config.SystemStateManager.Context)
    { }

    /// <summary>
    /// Получает список всех устройств, привязанных к определенному шасси.
    /// </summary>
    /// <param name="numberChassis">Номер шасси.</param>
    /// <returns>Список быстрых измерителей.</returns>
    public List<IFastMeter> GetDevicesByNumberChassis(int numberChassis)
    {
      var data = _context.Set<FastMeterEntity>()
                         .Where(device => device.NumberChassis == numberChassis)
                         .ToList();

      var result = data
          .OfType<IFastMeter>()
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
    public List<FastMeterEntity> GetEntitiesByNumberChassis(int numberChassis)
    {
      return _context.Set<FastMeterEntity>()
                     .Where(device => device.NumberChassis == numberChassis)
                     .ToList();
    }
  }
}
