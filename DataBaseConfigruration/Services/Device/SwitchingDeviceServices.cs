using Ask.Core.Services.Errors.DataBase;
using Ask.Core.Shared.Entity.Devices;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;

namespace DataBaseConfiguration.Services.Device
{
  /// <summary>
  /// Сервис для работы с моделями устройства коммутации шин из базы данных.
  /// Предоставляет методы для работы с устройствами, полученными из БД,
  /// преобразуя их из моделей данных в объекты.
  /// </summary>
  public class SwitchingDeviceServices : Service<ISwitchingDevice>
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="SwitchingDeviceServices"/>.
    /// </summary>
    public SwitchingDeviceServices() : base(DataBaseConfig.Context)
    { }

    public override void Create(ISwitchingDevice entity)
    {
      bool exists = _context.Set<SwitchingDeviceEntity>().Any(e => e.NumberChassis == entity.NumberChassis && e.Number == entity.Number);
      if (exists)
      {
        throw new DuplicateEntityException($"Устройство коммутации шин с шасси {entity.NumberChassis} и адресом {entity.Number} уже существует.");
      }

      base.Create(entity);
    }

    /// <summary>
    /// Получает список всех устройств, привязанных к определенному шасси.
    /// </summary>
    /// <param name="numberChassis">Номер шасси.</param>
    /// <returns>Список устройств коммутации шин.</returns>
    public List<ISwitchingDevice> GetDevicesByNumberChassis(int numberChassis)
    {
      var data = _context.Set<SwitchingDeviceEntity>()
                         .Where(device => device.NumberChassis == numberChassis)
                         .ToList();

      var result = data
          .OfType<ISwitchingDevice>()
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
    public List<SwitchingDeviceEntity> GetEntitiesByNumberChassis(int numberChassis)
    {
      return _context.Set<SwitchingDeviceEntity>()
                     .Where(device => device.NumberChassis == numberChassis)
                     .ToList();
    }

    public List<SwitchingDeviceEntity> GetAllEntities()
    {
      return GetAllData().OfType<SwitchingDeviceEntity>().ToList();
    }

  }
}
