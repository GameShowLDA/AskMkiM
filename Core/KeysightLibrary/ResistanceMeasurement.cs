using Agilent.CommandExpert.ScpiNet.Ag3466x_2_08;

namespace Core.KeysightLibrary
{
  /// <summary>
  /// Класс для взаимодействия с сопротивлением.
  /// </summary>
  public class ResistanceMeasurement
  {
    /// <summary>
    /// Устанавливает режим сопротивление.
    /// </summary>
    static public void SetResistanceMode(Ag3466x meter)
    {
      meter.SCPI.CONFigure.RESistance.Command(null, null);
    }

    /// <summary>
    /// Измеряет сопротивление.
    /// </summary>
    /// <param name="meter">Модель мультиметра..</param>
    /// <returns>Измеренное значение сопротивления.</returns>
    static public double MeasureResistance(Ag3466x meter)
    {
      meter.SCPI.MEASure.RESistance.QueryAsciiReal(null, null, out double resistance);
      return resistance;
    }

    /// <summary>
    /// Устанавливает вторичный параметр для измерения сопротивления.
    /// </summary>
    /// <param name="secondary">Вторичный параметр.</param>
    public void SetResistanceSecondary(double secondary)
    {
      // Код реализации здесь
    }

    /// <summary>
    /// Устанавливает разрешение для измерения сопротивления.
    /// </summary>
    /// <param name="resolution">Значение разрешения.</param>
    public void SetResistanceResolution(double resolution)
    {
      // Код реализации здесь
    }

    /// <summary>
    /// Устанавливает разрешение для измерения четырехпроводного сопротивления.
    /// </summary>
    /// <param name="resolution">Значение разрешения.</param>
    public void SetFourWireResistanceResolution(double resolution)
    {
      // Код реализации здесь
    }

    /// <summary>
    /// Устанавливает вторичный параметр для измерения четырехпроводного сопротивления.
    /// </summary>
    /// <param name="secondary">Вторичный параметр.</param>
    public void SetFourWireResistanceSecondary(double secondary)
    {
      // Код реализации здесь
    }

    /// <summary>
    /// Устанавливает диапазон для измерения четырехпроводного сопротивления.
    /// </summary>
    /// <param name="range">Диапазон измерения.</param>
    public void SetFourWireResistanceRange(double range)
    {
      // Код реализации здесь
    }

    /// <summary>
    /// Устанавливает диапазон для измерения сопротивления.
    /// </summary>
    /// <param name="meter">Модель мультиметра..</param>
    /// <param name="range">Максимальное значение.</param>
    public void SetResistanceRange(Ag3466x meter, double range)
    {
      SetResistanceMode(meter);
      meter.SCPI.SENSe.RESistance.RANGe.Command(range);
    }

    /// <summary>
    /// Измеряет четырехпроводное сопротивление.
    /// </summary>
    /// <returns>Измеренное значение четырехпроводного сопротивления.</returns>
    public double MeasureFourWireResistance()
    {
      // Код реализации здесь
      return 0.0;
    }

    /// <summary>
    /// Включает автоматическое определение диапазона для измерения четырехпроводного сопротивления.
    /// </summary>
    public void EnableFourWireResistanceAutoRange()
    {
      // Код реализации здесь
    }
  }
}
