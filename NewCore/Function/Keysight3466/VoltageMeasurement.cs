using Agilent.CommandExpert.ScpiNet.Ag3466x_2_08;
using System;

namespace NewCore.Function.KeysightLibrary
{
  /// <summary>
  /// Класс для взаимодействия с напряжением.
  /// </summary>
  public class VoltageMeasurement
  {
    public VoltageMeasurement(Ag3466x gpt79904) => _ag3466x = gpt79904;
    Ag3466x _ag3466x { get; set; }

    /// <summary>
    /// Измеряет переменное напряжение.
    /// </summary>
    /// <returns>Измеренное значение переменного напряжения.</returns>
    internal double MeasureVoltageAC()
    {
      SetVoltageAC();
      try
      {
        double acVoltage = 0D;
        _ag3466x.SCPI.MEASure.VOLTage.AC.QueryAsciiReal(null, null, out acVoltage);
        return acVoltage;
      }
      catch
      {
        return -1;
      }
    }

    /// <summary>
    /// Измеряет постоянное напряжение.
    /// </summary>
    /// <param name="meter">Модель мультиметра..</param>
    /// <returns>Измеренное значение постоянного напряжения.</returns>
    internal double MeasureVoltageDC()
    {
      SetVoltageDC();
      try
      {
        double dcVoltage;
        _ag3466x.SCPI.MEASure.VOLTage.DC.QueryAsciiRealClone(null, null, out dcVoltage);
        return dcVoltage;
      }
      catch
      {
        return -1;
      }
    }

    /// <summary>
    /// Конфигурирует устройство для проверки переменного напряжения.
    /// </summary>
    public void SetVoltageAC()
    {
      _ag3466x.SCPI.CONFigure.VOLTage.AC.Command();
    }

    /// <summary>
    /// Конфигурирует устройство для проверки переменного напряжения.
    /// </summary>
    public void SetVoltageDC()
    {
      _ag3466x.SCPI.CONFigure.VOLTage.DC.Command();
    }
  }
}
