using Ask.Core.Services.Errors.DataBase;
using Ask.Core.Shared.Entity.Devices;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Rack;

namespace DataBaseConfiguration.Services.Device
{
  /// <summary>
  /// Сервис для работы с моделями стоек СКМ из базы данных.
  /// Предоставляет методы для работы с устройствами, полученными из БД,
  /// преобразуя их из моделей данных в объекты.
  /// </summary>
  public class RackServices : Service<IRack>
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="RackServices"/>.
    /// </summary>
    public RackServices() : base(DataBaseConfig.Context)
    { }

    public override void Create(IRack entity)
    {
      bool exists = _context.Set<RackEntity>().Any(e => e.Number == entity.Number);
      if (exists)
      {
        throw new DuplicateEntityException($"Стойка с номером {entity.Number} уже существует.");
      }
      base.Create(entity);
    }

    public List<RackEntity> GetAllEntities()
    {
      return GetAllData().OfType<RackEntity>().ToList();
    }
  }
}
