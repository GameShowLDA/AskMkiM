using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataBaseConfiguration.Models.MeasurementError;
using DataBaseConfiguration.Repositories;

namespace DataBaseConfiguration.Services
{
  public class MeasurementErrorServices : Repository<MeasurementErrorEntity>
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="MeasurementErrorRepository"/>.
    /// </summary>
    /// <param name="context">Контекст базы данных.</param>
    public MeasurementErrorServices() : base(DataBaseConfig.Context)
    {
    }
  }
}
