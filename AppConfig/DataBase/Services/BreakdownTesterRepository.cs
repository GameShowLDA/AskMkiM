using AppConfig.DataBase.Models;

namespace AppConfig.DataBase.Services
{
  /// <summary>
  /// Репозиторий для управления пробойными установками
  /// </summary>
  public class BreakdownTesterRepository : Repository<BreakdownTesterEntity>
  {
    public BreakdownTesterRepository(AppDbContext context) : base(context) { }

    /// <summary>
    /// Получает список всех устройств, привязанных к определенному шасси.
    /// </summary>
    /// <param name="numberChassis">Номер шасси</param>
    /// <returns>Список пробойных установок</returns>
    public List<BreakdownTesterEntity> GetDevicesByNumberChassis(int numberChassis)
    {
      return _dbSet.Where(device => device.NumberChassis == numberChassis).ToList();
    }
  }
}
