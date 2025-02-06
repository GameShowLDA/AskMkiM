using AppConfig.DataBase.Models;

namespace AppConfig.DataBase.Services
{
  /// <summary>
  /// Репозиторий для управления модулями источников питания
  /// </summary>
  public class PowerSourceModuleRepository : Repository<PowerSourceModuleEntity>
  {
    public PowerSourceModuleRepository(AppDbContext context) : base(context) { }

    /// <summary>
    /// Получает список всех устройств, привязанных к определенному шасси.
    /// </summary>
    /// <param name="numberChassis">Номер шасси</param>
    /// <returns>Список модулей напряжения и тока.</returns>
    public List<PowerSourceModuleEntity> GetDevicesByNumberChassis(int numberChassis)
    {
      return _dbSet.Where(device => device.NumberChassis == numberChassis).ToList();
    }
  }

}
