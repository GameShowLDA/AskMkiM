using Agilent.CommandExpert.ScpiNet.Ag3466x_2_08;

namespace Core.KeysightLibrary
{
  /// <summary>
  /// Класс для взаимодействия с прозвонкой.
  /// </summary>
  public class ContinuityMeasurement
  {
    /// <summary>
    /// Проверяет проводимость.
    /// </summary>
    /// <param name="meter">Модель мультиметра.</param>
    /// <returns>Результат проверки проводимости.</returns>
    static public double MeasureContinuity(Ag3466x meter)
    {
      double continuity = 0D;
      meter.SCPI.MEASure.CONTinuity.QueryAsciiReal(out continuity);
      // TODO :  Вычитание 1.4 из-за погрешности системы. Потом удалить.
      return continuity - 1.4;
    }

    /// <summary>
    /// Конфигурирует устройство для проверки проводимости.
    /// </summary>
    public void ConfigureContinuity()
    {
      // Код реализации здесь
    }
  }
}
