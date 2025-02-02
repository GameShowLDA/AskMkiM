using AppConfig.DataBase.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppConfig.DataBase.Services
{
  /// <summary>
  /// Репозиторий для управления модулями коммутации реле
  /// </summary>
  internal class RelaySwitchModuleRepository : Repository<RelaySwitchModuleEntity>
  {
    public RelaySwitchModuleRepository(AppDbContext context) : base(context) { }
  }
}
