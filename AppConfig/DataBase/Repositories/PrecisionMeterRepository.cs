using AppConfig.DataBase.Models;

namespace AppConfig.DataBase.Repositories
{
  /// <summary>
  /// Репозиторий для управления точными измерителями.
  /// </summary>
  public class PrecisionMeterRepository : Repository<PrecisionMeterEntity>
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="PrecisionMeterRepository"/>.
    /// </summary>
    /// <param name="context">Контекст подключения к БД.</param>
    public PrecisionMeterRepository() : base(AppConfig.Config.SystemStateManager.Context) { }

    /// <summary>
    /// Получает список всех устройств, привязанных к определенному шасси.
    /// </summary>
    /// <param name="numberChassis">Номер шасси.</param>
    /// <returns>Список точных измерителей.</returns>
    public List<PrecisionMeterEntity> GetDevicesByNumberChassis(int numberChassis)
    {
      return _dbSet.Where(device => device.NumberChassis == numberChassis).ToList();
    }
  }
}
