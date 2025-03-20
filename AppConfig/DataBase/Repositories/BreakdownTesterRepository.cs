using AppConfig.DataBase.Models;

namespace AppConfig.DataBase.Repositories
{
  /// <summary>
  /// Репозиторий для управления пробойными установками.
  /// </summary>
  internal class BreakdownTesterRepository : Repository<BreakdownTesterEntity>
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="BreakdownTesterRepository"/>.
    /// </summary>
    /// <param name="context">Контекст подключения к БД.</param>
    internal BreakdownTesterRepository(AppDbContext context) : base(context) { }

    /// <summary>
    /// Получает список всех устройств, привязанных к определенному шасси.
    /// </summary>
    /// <param name="numberChassis">Номер шасси.</param>
    /// <returns>Список пробойных установок.</returns>
    internal List<BreakdownTesterEntity> GetDevicesByNumberChassis(int numberChassis)
    {
      return _dbSet.Where(device => device.NumberChassis == numberChassis).ToList();
    }
  }
}
