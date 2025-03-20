using AppConfig.DataBase.Models;

namespace AppConfig.DataBase.Repositories
{
  /// <summary>
  /// Репозиторий для управления модулями коммутации реле.
  /// </summary>
  internal class RelaySwitchModuleRepository : Repository<RelaySwitchModuleEntity>
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="RelaySwitchModuleRepository"/>.
    /// </summary>
    /// <param name="context">Описание.</param>
    internal RelaySwitchModuleRepository(AppDbContext context) : base(context) { }

    /// <summary>
    /// Получает список всех устройств, привязанных к определенному шасси.
    /// </summary>
    /// <param name="numberChassis">Номер шасси.</param>
    /// <returns>Список модулей коммутации реле.</returns>
    internal List<RelaySwitchModuleEntity> GetDevicesByNumberChassis(int numberChassis)
    {
      return _dbSet.Where(device => device.NumberChassis == numberChassis).ToList();
    }
  }
}
