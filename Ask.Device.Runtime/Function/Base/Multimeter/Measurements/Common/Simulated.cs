using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace Ask.Device.Runtime.Function.Base.Multimeter.Measurements.Common
{
  /// <summary>
  /// Предоставляет генерацию имитированных результатов измерений.
  /// </summary>
  internal class Simulated
  {
    /// <summary>
    /// Генератор случайных чисел.
    /// </summary>
    private static readonly Random _rnd = new();

    /// <summary>
    /// Возвращает имитированное значение измерения.
    /// </summary>
    /// <param name="rangeFrom">Нижняя граница допустимого диапазона.</param>
    /// <param name="rangeTo">Верхняя граница допустимого диапазона.</param>
    /// <param name="measurementTypeCommand">Тип выполняемого электрического испытания.</param>
    /// <returns>
    /// Сгенерированное значение измерения либо <c>-1</c>, если режим имитации отключён.
    /// </returns>
    internal static double GetSimulatedValue(double rangeFrom, double rangeTo, ElectricalTestFunction measurementTypeCommand)
    {
      if (rangeTo == -1)
      {
        rangeTo = rangeFrom * 2;
      }

      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        if (IdleMeasurementErrorSimulator.TryGetValue(rangeFrom, rangeTo, out double erroneousValue))
        {
          return erroneousValue;
        }

        switch (measurementTypeCommand)
        {
          case ElectricalTestFunction.None:
            break;
          case ElectricalTestFunction.DielectricWithstandAC:
          case ElectricalTestFunction.DielectricWithstandDC:
          case ElectricalTestFunction.InsulationResistance:
          case ElectricalTestFunction.ACVoltage:
          case ElectricalTestFunction.DCVoltage:
          case ElectricalTestFunction.Resistance:
          case ElectricalTestFunction.Capacitance:
          case ElectricalTestFunction.Continuity:
          case ElectricalTestFunction.Diode:
            return (rangeFrom + rangeTo) / 2;
        }

        double min = rangeFrom / 2;
        double max = rangeTo == double.MaxValue
            ? rangeTo
            : rangeTo * 2;

        if (min > max)
          (min, max) = (max, min);

        if (double.IsInfinity(min) || double.IsInfinity(max))
          return rangeFrom;

        if (Math.Abs(max - min) < double.Epsilon)
          return min;

        return min + (_rnd.NextDouble() * (max - min));
      }

      return -1;
    }
  }
}
