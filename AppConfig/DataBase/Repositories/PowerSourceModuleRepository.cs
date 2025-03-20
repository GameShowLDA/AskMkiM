using AppConfig.DataBase.Models;

namespace AppConfig.DataBase.Repositories
{
  /// <summary>
  /// Репозиторий для управления модулями источников питания.
  /// </summary>
  public class PowerSourceModuleRepository : Repository<PowerSourceModuleEntity>
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="PowerSourceModuleRepository"/>.
    /// </summary>
    /// <param name="context">Контекст подключения к БД.</param>
    public PowerSourceModuleRepository() : base(AppConfig.Config.SystemStateManager.Context) { }

    /// <summary>
    /// Получает список всех устройств, привязанных к определенному шасси.
    /// </summary>
    /// <param name="numberChassis">Номер шасси.</param>
    /// <returns>Список модулей напряжения и тока.</returns>
    public List<PowerSourceModuleEntity> GetDevicesByNumberChassis(int numberChassis)
    {
      return _dbSet.Where(device => device.NumberChassis == numberChassis).ToList();
    }
  }
}
