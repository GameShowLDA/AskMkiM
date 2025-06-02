using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppConfiguration.Enums;

namespace DataBaseConfiguration.Models.MeasurementError
{
  public class MeasurementErrorEntity
  {
    public int Id { get; set; }

    /// <summary>
    /// Тип команды, определяющий режим метрологии.
    /// </summary>
    public TypeCommand Type { get; set; }

    /// <summary>
    /// Погрешность измерения в процентах.
    /// </summary>
    public double PercentageError { get; set; }

    /// <summary>
    /// Погрешность измерения в числовом значении.
    /// </summary>
    public double NumericError { get; set; }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="MeasurementErrorEntity"/> с заданными значениями.
    /// </summary>
    /// <param name="typeCommand">Тип команды (режим метрологии).</param>
    /// <param name="percentageError">Погрешность измерения в процентах.</param>
    /// <param name="numericError">Погрешность измерения в числовом значении.</param>
    public MeasurementErrorEntity(TypeCommand typeCommand, double percentageError, double numericError)
    {
      Type = typeCommand;
      PercentageError = percentageError;
      NumericError = numericError;
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="MeasurementErrorEntity"/>.
    /// </summary>
    public MeasurementErrorEntity() { }
  }
}
