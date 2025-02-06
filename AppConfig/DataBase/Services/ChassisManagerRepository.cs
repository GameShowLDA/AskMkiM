using AppConfig.DataBase.Models;

namespace AppConfig.DataBase.Services
{
  /// <summary>
  /// Репозиторий для управления менеджерами шасси
  /// </summary>
  public class ChassisManagerRepository : Repository<ChassisManagerEntity>
  {
    public ChassisManagerRepository(AppDbContext context) : base(context) { }
  }
}
