using Ask.Core.Services.Errors.DataBase;
using Ask.Core.Shared.Entity.Devices;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;

namespace DataBaseConfiguration.Services.Device
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
    public RelaySwitchModuleServices() : base(DataBaseConfig.Context)
    { }

    public override void Create(IRelaySwitchModule entity)
    {
      bool exist = _context.Set<RelaySwitchModuleEntity>().Any(e => e.NumberChassis == entity.NumberChassis && e.Number == entity.Number);
      if (exist)
      {
        throw new DuplicateEntityException($"Модуль коммутации реле с шасси {entity.NumberChassis} и адресом {entity.Number} уже существует.");
      }
      base.Create(entity);
    }

    /// <summary>
    /// Получает список всех устройств.
    /// </summary>
    /// <returns>Список модулей коммутации реле.</returns>
    public List<IRelaySwitchModule> GetAllDevices()
    {
      var data = _context.Set<RelaySwitchModuleEntity>()
                         .ToList();

      var result = data
          .OfType<IRelaySwitchModule>()
          .Select(GetDeviceInstance)
          .Where(instance => instance != null)
          .ToList();

      return result;
    }

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
    /// Получает список всех устройств, привязанных к определенному шасси.
    /// </summary>
    /// <param name="numberChassis">Номер шасси.</param>
    /// <returns>Список модулей коммутации реле.</returns>
    public IRelaySwitchModule GetDeviceByNumberChassis(int numberChassis, int number)
    {
      var data = _context.Set<RelaySwitchModuleEntity>()
                         .Where(device => device.NumberChassis == numberChassis && device.Number == number)
                         .ToList();

      var result = data
          .OfType<IRelaySwitchModule>()
          .Select(GetDeviceInstance)
          .Where(instance => instance != null)
          .ToList().FirstOrDefault();

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

    public List<RelaySwitchModuleEntity> GetAllEntities()
    {
      return GetAllData().OfType<RelaySwitchModuleEntity>().ToList();
    }

    public void UpdateResistance(int chassis, int module, double value)
    {
      var entity = _context.Set<RelaySwitchModuleEntity>()
        .FirstOrDefault(e =>
          e.NumberChassis == chassis &&
          e.Number == module);

      if (entity == null)
      {
        throw new Exception(
          $"Модуль коммутации реле с шасси {chassis} и номером {module} не найден.");
      }

      entity.SwitchResistance = value;

      _context.SaveChanges();
    }
  }
}
