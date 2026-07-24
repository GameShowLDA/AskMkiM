using Ask.Core.Services.Extensions;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Engine.Tests.Protocol;

namespace Ask.Engine.Tests.MethodExecutor
{
  /// <summary>
  /// Формирует результаты измерений для протоколов группового метода.
  /// </summary>
  internal static class GroupMethodProtocolBuilder
  {
    /// <summary>
    /// Форматирует измеренное значение с единицей измерения.
    /// </summary>
    /// <param name="value">Измеренное значение.</param>
    /// <param name="unit">Единица измерения.</param>
    /// <returns>Строковое представление измеренного значения.</returns>
    internal static string FormatValue(double value, Enum unit)
      => MeasurementValueFormatter.FormatWithUnit(value, unit.GetUnit());

    /// <summary>
    /// Формирует описание брака для разряда.
    /// </summary>
    /// <param name="dischargeIndex">Индекс проверяемого разряда.</param>
    /// <param name="bitString">Двоичная маска проверяемого разряда.</param>
    /// <param name="limit">Допустимый предел измеряемой величины.</param>
    /// <param name="result">Измеренное значение.</param>
    /// <param name="unit">Единица измерения.</param>
    /// <param name="limitKind">Расположение допустимого предела относительно измеряемой величины.</param>
    /// <returns>Описание результата проверки для итогового заключения.</returns>
    internal static string BuildFailure(
      int dischargeIndex,
      string bitString,
      double limit,
      double result,
      Enum unit,
      MeasurementLimitKind limitKind)
    {
      var formattedLimit = MeasurementValueFormatter.Format(limit);
      var formattedResult = FormatValue(result, unit);
      var unitName = unit.GetUnit();
      var quantitySymbol = unit.GetQuantitySymbol();
      var condition = limitKind switch
      {
        MeasurementLimitKind.Minimum => $"{formattedLimit}<{quantitySymbol} {unitName}",
        MeasurementLimitKind.Maximum => $"{quantitySymbol}<{formattedLimit} {unitName}",
        _ => throw new ArgumentOutOfRangeException(nameof(limitKind), limitKind, null),
      };

      return $"Разряд-{dischargeIndex}[{bitString}] ({condition}). " +
        $"{quantitySymbol}изм = {formattedResult}";
    }
  }
}
