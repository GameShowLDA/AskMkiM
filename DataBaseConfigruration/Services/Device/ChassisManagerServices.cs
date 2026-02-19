using Ask.Core.Services.Errors.DataBase;
using Ask.Core.Shared.Entity.Devices;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis;

namespace DataBaseConfiguration.Services.Device
{
  /// <summary>
  /// Сервис для работы с моделями мененджера шасси из базы данных.
  /// Предоставляет методы для работы с устройствами, полученными из БД,
  /// преобразуя их из моделей данных в объекты.
  /// </summary>
  public class ChassisManagerServices : Service<IChassisManager>
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ChassisManagerServices"/>.
    /// </summary>
    public ChassisManagerServices() : base(DataBaseConfig.Context)
    { }

    public override void Create(IChassisManager entity)
    {
      bool exists = _context.Set<ChassisManagerEntity>().Any(e => e.Number == entity.Number);

      if (exists)
      {
        throw new DuplicateEntityException($"Шасси с номером {entity.Number} уже существует.");
      }

      base.Create(entity);
    }

    /// <summary>
    /// Получает менеджер шасси по номеру шасси.
    /// </summary>
    /// <param name="numberChassis">Номер шасси.</param>
    /// <returns>Объект менеджера шасси, реализующий <see cref="IChassisManager"/>.</returns>
    public IChassisManager GetByNumber(int numberChassis)
    {
      return GetAll().FirstOrDefault(device => device.Number == numberChassis);
    }

    public List<ChassisManagerEntity> GetAllEntities()
    {
      return GetAllData().OfType<ChassisManagerEntity>().ToList();
    }
  }
}
