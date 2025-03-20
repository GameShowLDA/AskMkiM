using AppConfig.DataBase.Models;

namespace AppConfig.DataBase.Repositories
{
  /// <summary>
  /// Репозиторий для управления менеджерами шасси.
  /// </summary>
  public class ChassisManagerRepository : Repository<ChassisManagerEntity>
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ChassisManagerRepository"/>.
    /// </summary>
    /// <param name="context">Контекст подключения к БД.</param>
    public ChassisManagerRepository() : base(AppConfig.Config.SystemStateManager.Context) { }

    /// <summary>
    /// Получает список всех устройств, привязанных к определенному шасси.
    /// </summary>
    /// <param name="numberChassis">Номер шасси.</param>
    /// <returns>Список быстрых измерителей.</returns>
    public ChassisManagerEntity GetByNumber(int numberChassis)
    {
      return _dbSet.Find(numberChassis);
    }
  }
}
