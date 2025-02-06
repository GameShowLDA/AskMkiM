using Agilent.CommandExpert.ScpiNet.Ag3466x_2_08;

namespace NewCore.Function.KeysightLibrary
{
  /// <summary>
  /// Класс для взаимодействия с ёмкостью.
  /// </summary>
  public class CapacitanceMeasurement
  {
    public CapacitanceMeasurement(Ag3466x gpt79904) => _ag3466x = gpt79904;
    Ag3466x _ag3466x { get; set; }

    /// <summary>
    /// Измеряет ёмкость в микрофарадах.
    /// </summary>
    /// <param name="model">Модель мультиметра..</param>
    /// <returns>Измеренное значение ёмкости в микрофарадах.</returns>
    public double MeasureCapacitance()
    {
      SetCapacitanceMode();
      double capacitanceFarads = 0D;
      _ag3466x.SCPI.MEASure.CAPacitance.QueryAsciiReal(null, null, out capacitanceFarads);
      double capacitanceMicrofarads = capacitanceFarads * 1e6;
      return capacitanceMicrofarads;
    }

    /// <summary>
    /// Устанавливает режим измерения ёмкости.
    /// </summary>
    public void SetCapacitanceMode()
    {
      _ag3466x.SCPI.CONFigure.CAPacitance.Command(null, null);
    }

  }
}
