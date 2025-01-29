using Agilent.CommandExpert.ScpiNet.Ag3466x_2_08;

namespace Core.KeysightLibrary
{
  /// <summary>
  /// Класс для взаимодействия с напряжением.
  /// </summary>
  static internal class VoltageMeasurement
  {
    /// <summary>
    /// Измеряет переменное напряжение.
    /// </summary>
    /// <returns>Измеренное значение переменного напряжения.</returns>
    static internal double MeasureVoltageAC()
    {
      // Код реализации здесь
      return 0.0;
    }

    /// <summary>
    /// Устанавливает вторичный параметр для измерения переменного напряжения.
    /// </summary>
    /// <param name="secondary">Вторичный параметр.</param>
    static internal void SetVoltageACSecondary(double secondary)
    {
      // Код реализации здесь
    }

    /// <summary>
    /// Устанавливает диапазон для измерения переменного напряжения.
    /// </summary>
    /// <param name="range">Диапазон измерения.</param>
    static internal void SetVoltageACRange(double range)
    {
      // Код реализации здесь
    }

    /// <summary>
    /// Измеряет постоянное напряжение.
    /// </summary>
    /// <param name="meter">Модель мультиметра..</param>
    /// <returns>Измеренное значение постоянного напряжения.</returns>
    static internal double MeasureVoltageDC(Ag3466x meter)
    {
      try
      {
        double dcVoltage;
        meter.SCPI.MEASure.VOLTage.DC.QueryAsciiRealClone(null, null, out dcVoltage);
        return dcVoltage;
      }
      catch
      {
        return -1;
      }
    }

    /// <summary>
    /// Устанавливает диапазон для измерения постоянного напряжения.
    /// </summary>
    /// <param name="range">Диапазон измерения.</param>
    static internal void SetVoltageDCRange(double range)
    {
      // Код реализации здесь
    }

    /// <summary>
    /// Устанавливает вторичный параметр для измерения постоянного напряжения.
    /// </summary>
    /// <param name="secondary">Вторичный параметр.</param>
    static internal void SetVoltageDCSecondary(double secondary)
    {
      // Код реализации здесь
    }

    /// <summary>
    /// Устанавливает вторичный коэффициент для измерения постоянного напряжения.
    /// </summary>
    /// <param name="ratioSecondary">Вторичный коэффициент.</param>
    static internal void SetVoltageDCRatioSecondary(double ratioSecondary)
    {
      // Код реализации здесь
    }

    /// <summary>
    /// Устанавливает разрешение для измерения постоянного напряжения.
    /// </summary>
    /// <param name="resolution">Значение разрешения.</param>
    static internal void SetVoltageDCResolution(double resolution)
    {
      // Код реализации здесь
    }

    /// <summary>
    /// Включает автоматическое определение диапазона для измерения постоянного напряжения.
    /// </summary>
    static internal void EnableVoltageDCAutoRange()
    {
      // Код реализации здесь
    }

    /// <summary>
    /// Измеряет отношение постоянного напряжения.
    /// </summary>
    /// <returns>Измеренное значение отношения постоянного напряжения.</returns>
    static internal double MeasureVoltageDCRatio()
    {
      // Код реализации здесь
      return 0.0;
    }
  }
}
