using Agilent.CommandExpert.ScpiNet.Ag3466x_2_08;

namespace NewCore.Function.KeysightLibrary
{
  /// <summary>
  /// Класс для взаимодействия с прозвонкой.
  /// </summary>
  public class ContinuityMeasurement
  {
    public ContinuityMeasurement(Ag3466x gpt79904) => _ag3466x = gpt79904;
    Ag3466x _ag3466x { get; set; }

    /// <summary>
    /// Проверяет проводимость.
    /// </summary>
    /// <param name="meter">Модель мультиметра.</param>
    /// <returns>Результат проверки проводимости.</returns>
    public double MeasureContinuity()
    {
      SetConfigureContinuity();
      try
      {
        double continuity = 0D;
        _ag3466x.SCPI.MEASure.CONTinuity.QueryAsciiReal(out continuity);
        // TODO :  Вычитание 1.4 из-за погрешности системы. Потом удалить.
        return continuity - 1.4;
      }
      catch
      {
        return -1;
      }
    }

    /// <summary>
    /// Конфигурирует устройство для проверки проводимости.
    /// </summary>
    public void SetConfigureContinuity()
    {
      _ag3466x.SCPI.CONFigure.CONTinuity.Command();
    }
  }
}
