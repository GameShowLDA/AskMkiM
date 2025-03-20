using AppConfig.DataBase.Models;

namespace AppConfig.DataBase.Repositories
{
  /// <summary>
  /// Репозиторий для управления быстрыми измерителями.
  /// </summary>
  public class FastMeterRepository : Repository<FastMeterEntity>
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="FastMeterRepository"/>.
    /// </summary>
    /// <param name="context">Контекст подключения к БД.</param>
    public FastMeterRepository() : base(AppConfig.Config.SystemStateManager.Context) { }

    /// <summary>
    /// Получает список всех устройств, привязанных к определенному шасси.
    /// </summary>
    /// <param name="numberChassis">Номер шасси.</param>
    /// <returns>Список быстрых измерителей.</returns>
    public List<FastMeterEntity> GetDevicesByNumberChassis(int numberChassis)
    {
      return _dbSet.Where(device => device.NumberChassis == numberChassis).ToList();
    }
  }
}
