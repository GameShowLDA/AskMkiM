using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppConfig.DataBase.Models;

namespace AppConfig.DataBase.Services
{
  public class RackRepository : Repository<RackEntity>
  {
    public RackRepository(AppDbContext context) : base(context) { }
  }
}
