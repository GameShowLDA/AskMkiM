using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Metadata.Static.Messages;

namespace Ask.Protocol.Messages.Builders;

/// <summary>
/// Формирует сообщения о допустимых диапазонах значений.
/// </summary>
internal static class RangeMessageBuilder
{
  /// <summary>
  /// Формирует сообщение о допустимом диапазоне значений.
  /// </summary>
  /// <param name="measurementUnit">Единица измерения.</param>
  /// <param name="measurementRange">Границы допустимого диапазона.</param>
  /// <param name="header">Заголовок сообщения.</param>
  /// <returns>Сообщение о допустимом диапазоне.</returns>
  /// <exception cref="ArgumentNullException">
  /// Выбрасывается, если <paramref name="measurementUnit"/> или
  /// <paramref name="measurementRange"/> равен <see langword="null"/>.
  /// </exception>
  /// <exception cref="ArgumentException">
  /// Выбрасывается, если <paramref name="header"/> не содержит значимых символов.
  /// </exception>
  internal static ShowMessageModel BuildAllowedRange(
    Enum measurementUnit,
    MeasurementRange measurementRange,
    string header)
  {
    ArgumentNullException.ThrowIfNull(measurementUnit);
    ArgumentNullException.ThrowIfNull(measurementRange);
    ArgumentException.ThrowIfNullOrWhiteSpace(header);

    string unit = measurementUnit.GetUnit();
    string lowerBound = MeasurementValueFormatter.Format(measurementRange.LowerBound);
    string upperBound = MeasurementValueFormatter.Format(measurementRange.UpperBound);

    return new ShowMessageModel(
      header,
      message: $"от {lowerBound} до {upperBound} {unit}");
  }
}
