using DataBaseConfiguration.Models;
using NewCore.Base.Interface.Main;

namespace DataBaseConfiguration.Services
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
    public ChassisManagerServices() : base(Configurations.DataBaseConfig.Context)
    { }

    /// <summary>
    /// Получает менеджер шасси по номеру шасси.
    /// </summary>
    /// <param name="numberChassis">Номер шасси.</param>
    /// <returns>Объект менеджера шасси, реализующий <see cref="IChassisManager"/>.</returns>
    public IChassisManager GetByNumber(int numberChassis)
    {
      var entity = _context.Set<ChassisManagerEntity>()
                           .FirstOrDefault(device => device.Number == numberChassis);

      return GetDeviceInstance(entity);
    }

    public List<ChassisManagerEntity> GetAllEntities()
    {
      return GetAllData().OfType<ChassisManagerEntity>().ToList();
    }
  }
}
