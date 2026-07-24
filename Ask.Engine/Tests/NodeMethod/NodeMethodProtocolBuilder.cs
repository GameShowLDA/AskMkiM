using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Engine.Tests.Protocol;

namespace Ask.Engine.Tests.NodeMethod
{
  /// <summary>
  /// Формирует результаты измерений для протоколов узлового метода.
  /// </summary>
  internal static class NodeMethodProtocolBuilder
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
    /// Формирует описание брака для точки.
    /// </summary>
    /// <param name="point">Проверяемая точка.</param>
    /// <param name="limit">Допустимый предел измеряемой величины.</param>
    /// <param name="result">Измеренное значение.</param>
    /// <param name="unit">Единица измерения.</param>
    /// <param name="limitKind">Расположение допустимого предела относительно измеряемой величины.</param>
    /// <returns>Описание результата проверки для итогового заключения.</returns>
    internal static string BuildFailure(
      PointModel point,
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

      return $"Точка[{point}]({condition}). {quantitySymbol}изм = {formattedResult}";
    }

    /// <summary>
    /// Формирует описание брака для точки с допустимым диапазоном.
    /// </summary>
    /// <param name="point">Проверяемая точка.</param>
    /// <param name="lowerLimit">Нижняя граница допустимого диапазона.</param>
    /// <param name="upperLimit">Верхняя граница допустимого диапазона.</param>
    /// <param name="result">Измеренное значение.</param>
    /// <param name="unit">Единица измерения.</param>
    /// <returns>Описание результата проверки для итогового заключения.</returns>
    internal static string BuildRangeFailure(
      PointModel point,
      double lowerLimit,
      double upperLimit,
      double result,
      Enum unit)
    {
      var formattedLowerLimit = MeasurementValueFormatter.Format(lowerLimit);
      var formattedUpperLimit = MeasurementValueFormatter.Format(upperLimit);
      var formattedResult = FormatValue(result, unit);
      var unitName = unit.GetUnit();
      var quantitySymbol = unit.GetQuantitySymbol();

      return $"Точка[{point}]({formattedLowerLimit}<{quantitySymbol}<{formattedUpperLimit} {unitName}). " +
        $"{quantitySymbol}изм = {formattedResult}";
    }
  }
}
