using AppConfig.DataBase.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppConfig.DataBase.Services
{
  /// <summary>
  /// Репозиторий для управления менеджерами шасси
  /// </summary>
  internal class ChassisManagerRepository : Repository<ChassisManagerEntity>
  {
    public ChassisManagerRepository(AppDbContext context) : base(context) { }
  }
}
