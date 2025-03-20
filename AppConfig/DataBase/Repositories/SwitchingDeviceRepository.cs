using AppConfig.DataBase.Models;

namespace AppConfig.DataBase.Repositories
{
  /// <summary>
  /// Репозиторий для управления устройствами коммутации шин в базе данных.
  /// </summary>
  internal class SwitchingDeviceRepository : Repository<SwitchingDeviceEntity>
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="SwitchingDeviceRepository"/>.
    /// </summary>
    /// <param name="context">Контекст подключения к БД.</param>
    internal SwitchingDeviceRepository(AppDbContext context) : base(context) { }

    /// <summary>
    /// Получает список всех устройств, привязанных к определенному шасси.
    /// </summary>
    /// <param name="numberChassis">Номер шасси.</param>
    /// <returns>Список устройств коммутации шин.</returns>
    internal List<SwitchingDeviceEntity> GetDevicesByNumberChassis(int numberChassis)
    {
      return _dbSet.Where(device => device.NumberChassis == numberChassis).ToList();
    }
  }
}
