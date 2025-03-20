using AppConfig.DataBase.Models;

namespace AppConfig.DataBase.Repositories
{
  /// <summary>
  /// Репозиторий для управления модулями источников питания.
  /// </summary>
  internal class PowerSourceModuleRepository : Repository<PowerSourceModuleEntity>
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="PowerSourceModuleRepository"/>.
    /// </summary>
    /// <param name="context">Контекст подключения к БД.</param>
    internal PowerSourceModuleRepository(AppDbContext context) : base(context) { }

    /// <summary>
    /// Получает список всех устройств, привязанных к определенному шасси.
    /// </summary>
    /// <param name="numberChassis">Номер шасси.</param>
    /// <returns>Список модулей напряжения и тока.</returns>
    internal List<PowerSourceModuleEntity> GetDevicesByNumberChassis(int numberChassis)
    {
      return _dbSet.Where(device => device.NumberChassis == numberChassis).ToList();
    }
  }
}
