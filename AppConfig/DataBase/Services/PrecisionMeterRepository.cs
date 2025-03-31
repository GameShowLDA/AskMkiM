using AppConfig.DataBase.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppConfig.DataBase.Services
{
  /// <summary>
  /// Репозиторий для управления точными измерителями
  /// </summary>
  internal class PrecisionMeterRepository : Repository<PrecisionMeterEntity>
  {
    public PrecisionMeterRepository(AppDbContext context) : base(context) { }
  }
}
