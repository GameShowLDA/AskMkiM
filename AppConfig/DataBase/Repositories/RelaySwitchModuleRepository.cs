using AppConfig.DataBase.Models;

namespace AppConfig.DataBase.Repositories
{
  /// <summary>
  /// Репозиторий для управления модулями коммутации реле.
  /// </summary>
  public class RelaySwitchModuleRepository : Repository<RelaySwitchModuleEntity>
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="RelaySwitchModuleRepository"/>.
    /// </summary>
    /// <param name="context">Описание.</param>
    public RelaySwitchModuleRepository() : base(AppConfig.Config.SystemStateManager.Context) { }

    /// <summary>
    /// Получает список всех устройств, привязанных к определенному шасси.
    /// </summary>
    /// <param name="numberChassis">Номер шасси.</param>
    /// <returns>Список модулей коммутации реле.</returns>
    public List<RelaySwitchModuleEntity> GetDevicesByNumberChassis(int numberChassis)
    {
      return _dbSet.Where(device => device.NumberChassis == numberChassis).ToList();
    }
  }
}
