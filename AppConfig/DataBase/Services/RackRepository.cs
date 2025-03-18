using AppConfig.DataBase.Models;

namespace AppConfig.DataBase.Services
{
  public class RackRepository : Repository<RackEntity>
  {
    public RackRepository(AppDbContext context) : base(context) { }
  }
}
