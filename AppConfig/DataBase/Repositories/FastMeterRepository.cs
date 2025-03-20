using AppConfig.DataBase.Models;

namespace AppConfig.DataBase.Repositories
{
  /// <summary>
  /// Репозиторий для управления быстрыми измерителями.
  /// </summary>
  internal class FastMeterRepository : Repository<FastMeterEntity>
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="FastMeterRepository"/>.
    /// </summary>
    /// <param name="context">Контекст подключения к БД.</param>
    internal FastMeterRepository(AppDbContext context) : base(context) { }

    /// <summary>
    /// Получает список всех устройств, привязанных к определенному шасси.
    /// </summary>
    /// <param name="numberChassis">Номер шасси.</param>
    /// <returns>Список быстрых измерителей.</returns>
    internal List<FastMeterEntity> GetDevicesByNumberChassis(int numberChassis)
    {
      return _dbSet.Where(device => device.NumberChassis == numberChassis).ToList();
    }
  }
}
