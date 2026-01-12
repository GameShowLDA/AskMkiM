using Ask.Core.Services.Errors.DataBase;
using Ask.Core.Shared.Entity.Devices;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;

namespace DataBaseConfiguration.Services.Device
{
  /// <summary>
  /// Сервис для работы с моделями ППУ из базы данных.
  /// Предоставляет методы для работы с устройствами, полученными из БД,
  /// преобразуя их из моделей данных в объекты.
  /// </summary>
  public class BreakdownTesterServices : Service<IBreakdownTester>
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="BreakdownTesterServices"/>.
    /// </summary>
    public BreakdownTesterServices() : base(DataBaseConfig.Context)
    { }

    public override void Create(IBreakdownTester entity)
    {

      bool exists = _context.Set<BreakdownTesterEntity>().Any(e => e.NumberChassis == entity.NumberChassis && e.Number == entity.Number);
      if (exists)
      {
        throw new DuplicateEntityException($"Модуль с шасси {entity.NumberChassis} и адресом {entity.Number} уже существует.");
      }

      base.Create(entity);
    }

    /// <summary>
    /// Получает список всех устройств, привязанных к определенному шасси.
    /// </summary>
    /// <param name="numberChassis">Номер шасси.</param>
    /// <returns>Список пробойных установок.</returns>
    public List<IBreakdownTester> GetDevicesByNumberChassis(int numberChassis)
    {
      var data = _context.Set<BreakdownTesterEntity>()
                         .Where(device => device.NumberChassis == numberChassis)
                         .ToList();

      var result = data
          .OfType<IBreakdownTester>()
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
    public List<BreakdownTesterEntity> GetEntitiesByNumberChassis(int numberChassis)
    {
      return _context.Set<BreakdownTesterEntity>()
                     .Where(device => device.NumberChassis == numberChassis)
                     .ToList();
    }

    public List<BreakdownTesterEntity> GetAllEntities()
    {
      return GetAllData().OfType<BreakdownTesterEntity>().ToList();
    }
  }
}
