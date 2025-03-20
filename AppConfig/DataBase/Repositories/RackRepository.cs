using AppConfig.DataBase.Models;

namespace AppConfig.DataBase.Repositories
{
  /// <summary>
  /// Репозиторий для управления стойками (Rack) в базе данных.
  /// </summary>
  internal class RackRepository : Repository<RackEntity>
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="RackRepository"/>.
    /// </summary>
    /// <param name="context">Контекст подключения к базе данных.</param>
    internal RackRepository(AppDbContext context) : base(context) { }
  }
}
