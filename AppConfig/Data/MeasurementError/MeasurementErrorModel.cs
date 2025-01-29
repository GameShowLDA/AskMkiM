namespace AppConfig.Data.MeasurementError
{
  public class MeasurementErrorModel
  {
    /// <summary>
    /// Перечисление, представляющее различные типы команд в системе.
    /// </summary>
    public enum TypeCommand
    {
      /// <summary>
      /// Тип команды KC.
      /// </summary>
      KC,

      /// <summary>
      /// Тип команды PR.
      /// </summary>
      PR,

      /// <summary>
      /// Тип команды CI.
      /// </summary>
      CI,

      /// <summary>
      /// Тип команды IE.
      /// </summary>
      IE,
    }


    /// <summary>
    /// Тип режима метрологии.
    /// </summary>
    public TypeCommand Type { get; set; }

    /// <summary>
    /// Погрешность в процентах.
    /// </summary>
    public double PercentageError { get; set; }

    /// <summary>
    /// Погрешность в числовом значении.
    /// </summary>
    public double NumericError { get; set; }

    /// <summary>
    /// Конструктор для инициализации модели с заданными значениями.
    /// </summary>
    /// <param name="percentageError">Погрешность в процентах.</param>
    /// <param name="numericError">Погрешность в числовом значении.</param>
    /// <param name="electricalParameter">Электрический параметр.</param>
    public MeasurementErrorModel(TypeCommand typeCommand, double percentageError, double numericError)
    {
      Type = typeCommand;
      PercentageError = percentageError;
      NumericError = numericError;
    }

    /// <summary>
    /// Конструктор для инициализации модели с заданными значениями.
    /// </summary>
    /// <param name="percentageError">Погрешность в процентах.</param>
    /// <param name="numericError">Погрешность в числовом значении.</param>
    /// <param name="electricalParameter">Электрический параметр.</param>
    public MeasurementErrorModel() { }
  }
}
