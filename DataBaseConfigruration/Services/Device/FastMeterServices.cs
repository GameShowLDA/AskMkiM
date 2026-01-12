using Ask.Core.Services.Errors.DataBase;
using Ask.Core.Shared.Entity.Devices;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;

namespace DataBaseConfiguration.Services.Device
{
  /// <summary>
  /// Сервис для работы с моделями быстрых измерителей из базы данных.
  /// Предоставляет методы для работы с устройствами, полученными из БД,
  /// преобразуя их из моделей данных в объекты.
  /// </summary>
  public class FastMeterServices : Service<IFastMeter>
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="FastMeterServices"/>.
    /// </summary>
    public FastMeterServices() : base(DataBaseConfig.Context)
    { }

    public override void Create(IFastMeter entity)
    {

      bool exists = _context.Set<FastMeterEntity>().Any(e => e.NumberChassis == entity.NumberChassis && e.Number == entity.Number);
      if (exists)
      {
        throw new DuplicateEntityException($"Мультиметр с шасси {entity.NumberChassis} и адресом {entity.Number} уже существует.");
      }

      base.Create(entity);
    }

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

    public List<FastMeterEntity> GetAllEntities()
    {
      return GetAllData().OfType<FastMeterEntity>().ToList();
    }
  }
}
