using AppConfig.DataBase.Models;

namespace AppConfig.DataBase.Repositories
{
  /// <summary>
  /// Репозиторий для управления менеджерами шасси.
  /// </summary>
  internal class ChassisManagerRepository : Repository<ChassisManagerEntity>
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ChassisManagerRepository"/>.
    /// </summary>
    /// <param name="context">Контекст подключения к БД.</param>
    internal ChassisManagerRepository(AppDbContext context) : base(context) { }
  }
}
