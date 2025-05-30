using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataBaseConfiguration.Models.MeasurementError
{
  public class MeasurementErrorEntity
  {
    /// <summary>
    /// Перечисление, представляющее различные типы команд в системе.
    /// </summary>
    public enum TypeCommand
    {
      [CommandInfo("КС", "Ом")]
      /// <summary>
      /// Тип команды KC.
      /// </summary>
      KC,

      [CommandInfo("ПР", "Ом")]
      /// <summary>
      /// Тип команды PR.
      /// </summary>
      PR,

      [CommandInfo("СИ", "МОм")]
      /// <summary>
      /// Тип команды CI.
      /// </summary>
      CI,

      [CommandInfo("ИЕ", "нФ")]
      /// <summary>
      /// Тип команды IE.
      /// </summary>
      IE,
    }

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
