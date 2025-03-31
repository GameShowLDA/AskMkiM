using AppConfig.DataBase.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppConfig.DataBase.Services
{
  /// <summary>
  /// Репозиторий для управления модулями источников питания
  /// </summary>
  internal class PowerSourceModuleRepository : Repository<PowerSourceModuleEntity>
  {
    public PowerSourceModuleRepository(AppDbContext context) : base(context) { }
  }

}
