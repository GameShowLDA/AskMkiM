using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;

namespace Ask.Device.Application.Function.Helpers
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
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        if (!ExecutionConfig.GetIsErrorSimulationEnabled().Result)
        {
          switch (measurementTypeCommand)
          {
            case ElectricalTestFunction.None:
              break;
            case ElectricalTestFunction.DielectricWithstandAC:
              return 30;
            case ElectricalTestFunction.DielectricWithstandDC:
              return 1;
            case ElectricalTestFunction.InsulationResistance:
              return GenerateInsulationResistance(rangeFrom, rangeTo);

            case ElectricalTestFunction.ACVoltage:
            case ElectricalTestFunction.DCVoltage:
            case ElectricalTestFunction.Resistance:
            case ElectricalTestFunction.Capacitance:
            case ElectricalTestFunction.Continuity:
            case ElectricalTestFunction.Diode:
              return (rangeFrom + rangeTo) / 2;
          }
        }
        else
        {
          switch (measurementTypeCommand)
          {
            case ElectricalTestFunction.DielectricWithstandAC:
              return new Random().Next(0, 80);
            case ElectricalTestFunction.DielectricWithstandDC:
              return new Random().Next(0, 5);
            case ElectricalTestFunction.InsulationResistance:
              return new Random().Next(0, 60000);
          }
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

    /// <summary>
    /// Генерирует случайное значение сопротивления изоляции с учётом заданного диапазона.
    /// </summary>
    /// <param name="rangeFrom">
    /// Нижняя граница диапазона. Если значение равно <c>-1</c>,
    /// используется минимальное допустимое значение (<c>0</c>).
    /// </param>
    /// <param name="rangeTo">
    /// Верхняя граница диапазона. Если значение равно <c>600000</c>,
    /// используется максимальное допустимое значение (<c>600000</c>).
    /// </param>
    /// <returns>
    /// Случайное значение сопротивления изоляции в указанном диапазоне.
    /// Если диапазон не задан (нижняя граница равна <c>-1</c>, а верхняя — <c>600000</c>),
    /// возвращается значение <c>60000</c>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Выбрасывается, если нижняя граница диапазона больше верхней.
    /// </exception>
    private static int GenerateInsulationResistance(double rangeFrom, double rangeTo)
    {
      const int MaxValue = 600000;
      const int DefaultValue = 60000;
      const int UndefinedMin = -1;

      if (rangeFrom == UndefinedMin && rangeTo == MaxValue)
      {
        return DefaultValue;
      }

      int min = rangeFrom != UndefinedMin ? (int)rangeFrom : 0;
      int max = rangeTo != MaxValue ? (int)rangeTo : MaxValue;

      if (min > max)
      {
        throw new ArgumentOutOfRangeException(nameof(rangeFrom), "Нижняя граница диапазона не может быть больше верхней.");
      }

      return Random.Shared.Next(min, max + 1);
    }
  }
}
