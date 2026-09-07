using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;

namespace Ask.Engine.ControlCommandExecutor.Execution;

/// <summary>
/// Формирует и публикует результат измерения по правилам конкретной команды.
/// </summary>
internal interface IMeasurementResultMessageExecutor
{
  /// <summary>
  /// Проверяет результат измерения, публикует его статус и возвращает признак успешной проверки.
  /// </summary>
  /// <param name="context">Данные измерения и параметры его публикации.</param>
  /// <returns>
  /// <see langword="true"/>, если результат измерения соответствует требованиям команды.
  /// В противном случае — <see langword="false"/>.
  /// </returns>
  Task<bool> PublishMeasurementResultAsync(MeasurementResultMessageContext context);
}

/// <summary>
/// Режим проверки результата измерения.
/// </summary>
internal enum MeasurementComparisonMode
{
  /// <summary>
  /// Проверка попадания значения в допустимый диапазон.
  /// </summary>
  Range,

  /// <summary>
  /// Проверка подтверждения разрыва цепи относительно нижней границы.
  /// </summary>
  Disconnection,
}

/// <summary>
/// Содержит данные для проверки и публикации результата измерения.
/// </summary>
internal sealed class MeasurementResultMessageContext
{
  internal MeasurementResultMessageContext(
    MeasurementTypeCommand measurementType,
    MeasurementRange range,
    IUserInteractionService messageService,
    string? measurementTarget = null)
  {
    MeasurementType = measurementType;
    Range = range ?? throw new ArgumentNullException(nameof(range));
    MessageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
    MeasurementTarget = measurementTarget;
    PublishedValue = range.TargetValue;
  }

  internal MeasurementTypeCommand MeasurementType { get; }
  internal MeasurementRange Range { get; }
  internal IUserInteractionService MessageService { get; }
  internal string? MeasurementTarget { get; }
  internal string? MeasurementPoints { get; init; }
  internal CheckType CheckType { get; init; } = CheckType.ControlProgram;
  internal MeasurementComparisonMode ComparisonMode { get; init; } = MeasurementComparisonMode.Range;
  internal bool IsOverloadExpected { get; init; }
  internal bool? SuccessOverride { get; init; }
  internal bool IsIntermediate { get; init; }
  internal double PublishedValue { get; set; }
}
