using AppConfig.DataBase.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppConfig.DataBase.Services
{
  /// <summary>
  /// Репозиторий для управления пробойными установками
  /// </summary>
  public class BreakdownTesterRepository : Repository<BreakdownTesterEntity>
  {
    public BreakdownTesterRepository(AppDbContext context) : base(context) { }
  }
}
