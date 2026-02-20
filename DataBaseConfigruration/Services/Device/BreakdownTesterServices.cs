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
      return GetAll()
        .Where(device => device.NumberChassis == numberChassis)
        .ToList();
    }

    /// <summary>
    /// Получает список сущностей пробойных установок, привязанных к определённому шасси.
    /// </summary>
    /// <param name="numberChassis">Номер шасси.</param>
    /// <returns>Список <see cref="BreakdownTesterEntity"/>.</returns>
    public List<BreakdownTesterEntity> GetEntitiesByNumberChassis(int numberChassis)
    {
      return GetAllData()
        .OfType<BreakdownTesterEntity>()
        .Where(device => device.NumberChassis == numberChassis)
        .ToList();
    }

    public List<BreakdownTesterEntity> GetAllEntities()
    {
      return GetAllData().OfType<BreakdownTesterEntity>().ToList();
    }

    /// <summary>
    /// Применяет конфигурацию ППУ без рефлексии.
    /// Сохраняет текущий runtime-объект (singleton) и обновляет только конфигурационные поля.
    /// </summary>
    protected override void ApplyConfiguration(IBreakdownTester source, IBreakdownTester target)
    {
      target.Id = source.Id;
      target.NumberChassis = source.NumberChassis;
      target.Name = source.Name;
      target.Description = source.Description;
      target.Number = source.Number;
      target.ConnectionDetails = source.ConnectionDetails;
      target.DeviceClass = source.DeviceClass;

      target.PiMaxVoltage = source.PiMaxVoltage;
      target.SiMaxVoltage = source.SiMaxVoltage;
      target.IRMinVoltage = source.IRMinVoltage;
    }
  }
}
