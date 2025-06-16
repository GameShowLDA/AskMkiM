using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppConfiguration.Enums;
using AppConfiguration.MeasurementError;
using DataBaseConfiguration.Models.MeasurementError;
using DataBaseConfiguration.Repositories;

namespace DataBaseConfiguration.Services.MeasurementError
{
  public class MeasurementErrorServices : Repository<MeasurementErrorEntity>, IMeasurementErrorProvider
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="MeasurementErrorRepository"/>.
    /// </summary>
    /// <param name="context">Контекст базы данных.</param>
    public MeasurementErrorServices() : base(DataBaseConfig.Context)
    {
    }

    /// <summary>
    /// Возвращает числовую погрешность по заданному типу команды.
    /// </summary>
    /// <param name="type">Тип команды.</param>
    /// <returns>Числовая погрешность.</returns>
    public double GetNumericError(TypeCommand type)
    {
      var entity = _dbSet.FirstOrDefault(e => e.Type == type);
      if (entity == null)
        throw new InvalidOperationException($"Погрешность для типа команды {type} не найдена в базе данных.");

      return entity.NumericError;
    }

    /// <summary>
    /// Возвращает процентную погрешность по заданному типу команды.
    /// </summary>
    /// <param name="type">Тип команды.</param>
    /// <returns>Процентная погрешность.</returns>
    public double GetPercentageError(TypeCommand type)
    {
      var entity = _dbSet.FirstOrDefault(e => e.Type == type);
      if (entity == null)
        throw new InvalidOperationException($"Погрешность для типа команды {type} не найдена в базе данных.");

      return entity.PercentageError;
    }

    /// <summary>
    /// Возвращает числовую и процентную погрешности по заданному типу команды.
    /// </summary>
    /// <param name="type">Тип команды.</param>
    /// <returns>Кортеж: числовая и процентная погрешности.</returns>
    public (double Numeric, double Percent) GetErrorParameters(TypeCommand type)
    {
      var entity = _dbSet.FirstOrDefault(e => e.Type == type);
      if (entity == null)
        throw new InvalidOperationException($"Погрешность для типа команды {type} не найдена в базе данных.");

      return (entity.NumericError, entity.PercentageError);
    }


    public (double Min, double Max) GetRange(TypeCommand typeCommand, double expectedValue)
    {
      var (numeric, percent) = GetErrorParameters(typeCommand);
      double min = expectedValue - numeric - expectedValue * percent / 100.0;
      double max = expectedValue + numeric + expectedValue * percent / 100.0;
      return (min, max);
    }
  }
}
