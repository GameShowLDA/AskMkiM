using AppConfig.DataBase.Models;

namespace AppConfig.DataBase.Repositories
{
  /// <summary>
  /// Репозиторий для управления стойками (Rack) в базе данных.
  /// </summary>
  public class RackRepository : Repository<RackEntity>
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="RackRepository"/>.
    /// </summary>
    /// <param name="context">Контекст подключения к базе данных.</param>
    public RackRepository() : base(AppConfig.Config.SystemStateManager.Context) { }
  }
}
