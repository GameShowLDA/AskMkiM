using Agilent.CommandExpert.ScpiNet.Ag3466x_2_08;

namespace NewCore.Function.KeysightLibrary
{
  /// <summary>
  /// Класс для взаимодействия с сопротивлением.
  /// </summary>
  public class ResistanceMeasurement
  {
    public ResistanceMeasurement(Ag3466x gpt79904) => _ag3466x = gpt79904;
    Ag3466x _ag3466x { get; set; }

    /// <summary>
    /// Устанавливает режим сопротивление.
    /// </summary>
    public void SetResistanceMode()
    {
      _ag3466x.SCPI.CONFigure.RESistance.Command(null, null);
    }

    /// <summary>
    /// Устанавливает режим сопротивление.
    /// </summary>
    public void SetFResistanceMode()
    {
      _ag3466x.SCPI.CONFigure.FRESistance.Command(null, null);
    }

    /// <summary>
    /// Измеряет сопротивление.
    /// </summary>
    /// <param name="meter">Модель мультиметра..</param>
    /// <returns>Измеренное значение сопротивления.</returns>
    public double MeasureResistance()
    {
      _ag3466x.SCPI.MEASure.RESistance.QueryAsciiReal(null, null, out double resistance);
      return resistance;
    }

    /// <summary>
    /// Измеряет четырехпроводное сопротивление.
    /// </summary>
    /// <returns>Измеренное значение четырехпроводного сопротивления.</returns>
    public double MeasureFourWireResistance()
    {
      SetFResistanceMode();
      double fresistance = 0D;
      _ag3466x.SCPI.MEASure.FRESistance.QueryAsciiReal(null, null, out fresistance);
      return fresistance;
    }
  }
}
