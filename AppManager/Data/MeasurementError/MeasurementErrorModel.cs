namespace AppManager.Data.MeasurementError
{
  /// <summary>
  /// Модель погрешности измерения, содержащая информацию о типе команды, процентной и числовой погрешностях.
  /// </summary>
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
    /// Инициализирует новый экземпляр класса <see cref="MeasurementErrorModel"/> с заданными значениями.
    /// </summary>
    /// <param name="typeCommand">Тип команды (режим метрологии).</param>
    /// <param name="percentageError">Погрешность измерения в процентах.</param>
    /// <param name="numericError">Погрешность измерения в числовом значении.</param>
    public MeasurementErrorModel(TypeCommand typeCommand, double percentageError, double numericError)
    {
      Type = typeCommand;
      PercentageError = percentageError;
      NumericError = numericError;
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="MeasurementErrorModel"/>.
    /// </summary>
    public MeasurementErrorModel() { }
  }
}
