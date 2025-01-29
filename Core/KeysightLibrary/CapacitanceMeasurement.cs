using Agilent.CommandExpert.ScpiNet.Ag3466x_2_08;

namespace Core.KeysightLibrary
{
  /// <summary>
  /// Класс для взаимодействия с ёмкостью.
  /// </summary>
  static public class CapacitanceMeasurement
  {
    /// <summary>
    /// Измеряет ёмкость в микрофарадах.
    /// </summary>
    /// <param name="model">Модель мультиметра..</param>
    /// <returns>Измеренное значение ёмкости в микрофарадах.</returns>
    static public double MeasureCapacitance(Ag3466x model)
    {
      SetCapacitanceMode(model);
      double capacitanceFarads = 0D;
      model.SCPI.MEASure.CAPacitance.QueryAsciiReal(null, null, out capacitanceFarads);
      double capacitanceMicrofarads = capacitanceFarads * 1e6;
      return capacitanceMicrofarads;
    }

    /// <summary>
    /// Устанавливает режим измерения ёмкости.
    /// </summary>
    static public void SetCapacitanceMode(Ag3466x meter)
    {
      meter.SCPI.CONFigure.CAPacitance.Command(null, null);
    }

  }
}
