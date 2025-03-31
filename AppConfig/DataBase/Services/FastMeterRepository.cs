using AppConfig.DataBase.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppConfig.DataBase.Services
{
  /// <summary>
  /// Репозиторий для управления быстрыми измерителями
  /// </summary>
  internal class FastMeterRepository : Repository<FastMeterEntity>
  {
    public FastMeterRepository(AppDbContext context) : base(context) { }
  }
}
