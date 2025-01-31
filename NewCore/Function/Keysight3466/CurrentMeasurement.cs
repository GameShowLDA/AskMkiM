using Agilent.CommandExpert.ScpiNet.Ag3466x_2_08;

namespace NewCore.Function.KeysightLibrary
{
  /// <summary>
  /// Класс для взаимодействия с током.
  /// </summary>
  public class CurrentMeasurement
  {
    public CurrentMeasurement(Ag3466x gpt79904) => _ag3466x = gpt79904;
    Ag3466x _ag3466x { get; set; }

    /// <summary>
    /// Устанавливает режим для измерения переменного тока.
    /// </summary>
    /// <param name="range">Диапазон измерения.</param>
    public void SetCurrentAC()
    {
      _ag3466x.SCPI.CONFigure.CURRent.AC.Command(null, null);
    }

    /// <summary>
    /// Устанавливает режим для постоянного переменного тока.
    /// </summary>
    /// <param name="range">Диапазон измерения.</param>
    public void SetCurrentDC()
    {
      _ag3466x.SCPI.CONFigure.CURRent.DC.Command(null, null);
    }

    /// <summary>
    /// Измеряет постоянный ток.
    /// </summary>
    /// <returns>Измеренное значение постоянного тока.</returns>
    public double MeasureCurrentDC()
    {
      SetCurrentDC();
      double dcCurrent = 0D;
      _ag3466x.SCPI.MEASure.CURRent.DC.QueryAsciiReal(null, null, out dcCurrent);
      return dcCurrent;
    }


    /// <summary>
    /// Измеряет переменный ток.
    /// </summary>
    /// <returns>Измеренное значение переменного тока.</returns>
    public double MeasureCurrentAC()
    {
      SetCurrentAC();
      double acCurrent = 0D;
      _ag3466x.SCPI.MEASure.CURRent.AC.QueryAsciiReal(null, null, out acCurrent);
      return acCurrent;
    }
  }
}
