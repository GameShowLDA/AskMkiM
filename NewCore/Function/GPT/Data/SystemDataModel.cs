namespace NewCore.Function.GPT.Data
{
  /// <summary>
  /// Класс для хранения данных конфигурации устройства.
  /// </summary>
  public class SystemDataModel
  {
    /// <summary>
    /// Контраст дисплея (от 1 до 8).
    /// </summary>
    public int LcdContrast { get; set; }

    /// <summary>
    /// Яркость дисплея (1 - темный, 2 - яркий).
    /// </summary>
    public int LcdBrightness { get; set; }

    /// <summary>
    /// Состояние звука успешного теста (true - включен, false - выключен).
    /// </summary>
    public bool BuzzerPrimarySound { get; set; }

    /// <summary>
    /// Состояние звука ошибочного теста (true - включен, false - выключен).
    /// </summary>
    public bool BuzzerFeedbackSound { get; set; }

    /// <summary>
    /// Продолжительность звука успешного теста (0.2 - 999.9 секунд).
    /// </summary>
    public double BuzzerPrimaryTime { get; set; }

    /// <summary>
    /// Продолжительность звука ошибочного теста (0.2 - 999.9 секунд).
    /// </summary>
    public double BuzzerFeedbackTime { get; set; }
  }
}
