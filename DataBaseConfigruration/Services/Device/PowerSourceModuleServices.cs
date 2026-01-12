using Ask.Core.Services.Errors.DataBase;
using Ask.Core.Shared.Entity.Devices;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule;

namespace DataBaseConfiguration.Services.Device
{
  /// <summary>
  /// Сервис для работы с моделями модуля источника напряжения и тока из базы данных.
  /// Предоставляет методы для работы с устройствами, полученными из БД,
  /// преобразуя их из моделей данных в объекты.
  /// </summary>
  public class PowerSourceModuleServices : Service<IPowerSourceModule>
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="PowerSourceModuleServices"/>.
    /// </summary>
    public PowerSourceModuleServices() : base(DataBaseConfig.Context)
    { }

    public override void Create(IPowerSourceModule entity)
    {
      bool exist = _context.Set<PowerSourceModuleEntity>().Any(e => e.NumberChassis == entity.NumberChassis && e.Number == entity.Number);
      if (exist)
      {
        throw new DuplicateEntityException($"Модуль источника питания с шасси {entity.NumberChassis} и адресом {entity.Number} уже существует.");
      }

      base.Create(entity);
    }

    /// <summary>
    /// Получает список всех устройств, привязанных к определенному шасси.
    /// </summary>
    /// <param name="numberChassis">Номер шасси.</param>
    /// <returns>Список модулей источников питания.</returns>
    public List<IPowerSourceModule> GetDevicesByNumberChassis(int numberChassis)
    {
      var data = _context.Set<PowerSourceModuleEntity>()
                         .Where(device => device.NumberChassis == numberChassis)
                         .ToList();

      var result = data
          .OfType<IPowerSourceModule>()
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
    public List<PowerSourceModuleEntity> GetEntitiesByNumberChassis(int numberChassis)
    {
      return _context.Set<PowerSourceModuleEntity>()
                     .Where(device => device.NumberChassis == numberChassis)
                     .ToList();
    }

    public List<PowerSourceModuleEntity> GetAllEntities()
    {
      return GetAllData().OfType<PowerSourceModuleEntity>().ToList();
    }
  }
}
