using DataBaseConfiguration.Configurations.Device;
using DataBaseConfiguration.Models.Device;
using NewCore.Base.Interface.Main;

namespace DataBaseConfiguration.Services
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

    public List<RackEntity> GetAllEntities()
    {
      return GetAllData().OfType<RackEntity>().ToList();
    }
  }
}
