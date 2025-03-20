using AppConfig.DataBase.Models;

namespace AppConfig.DataBase.Repositories
{
  /// <summary>
  /// Репозиторий для управления пробойными установками.
  /// </summary>
  public class BreakdownTesterRepository : Repository<BreakdownTesterEntity>
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="BreakdownTesterRepository"/>.
    /// </summary>
    /// <param name="context">Контекст подключения к БД.</param>
    public BreakdownTesterRepository() : base(AppConfig.Config.SystemStateManager.Context) { }

    /// <summary>
    /// Получает список всех устройств, привязанных к определенному шасси.
    /// </summary>
    /// <param name="numberChassis">Номер шасси.</param>
    /// <returns>Список пробойных установок.</returns>
    public List<BreakdownTesterEntity> GetDevicesByNumberChassis(int numberChassis)
    {
      return _dbSet.Where(device => device.NumberChassis == numberChassis).ToList();
    }
  }
}
