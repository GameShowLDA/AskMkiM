using AppConfig.DataBase.Models;

namespace AppConfig.DataBase.Repositories
{
  /// <summary>
  /// Репозиторий для управления точными измерителями.
  /// </summary>
  internal class PrecisionMeterRepository : Repository<PrecisionMeterEntity>
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="PrecisionMeterRepository"/>.
    /// </summary>
    /// <param name="context">Контекст подключения к БД.</param>
    internal PrecisionMeterRepository(AppDbContext context) : base(context) { }

    /// <summary>
    /// Получает список всех устройств, привязанных к определенному шасси.
    /// </summary>
    /// <param name="numberChassis">Номер шасси.</param>
    /// <returns>Список точных измерителей.</returns>
    internal List<PrecisionMeterEntity> GetDevicesByNumberChassis(int numberChassis)
    {
      return _dbSet.Where(device => device.NumberChassis == numberChassis).ToList();
    }
  }
}
