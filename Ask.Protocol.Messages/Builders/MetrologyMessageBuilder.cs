using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Static.Messages;

namespace Ask.Protocol.Messages.Builders;

/// <summary>
/// Формирует сообщения о выполнении метрологических режимов и их сводных результатах.
/// </summary>
internal static class MetrologyMessageBuilder
{
  /// <summary>
  /// Формирует сообщение об ошибке расчёта допустимого диапазона измерения.
  /// </summary>
  /// <param name="details">Описание ошибки расчёта.</param>
  /// <returns>Сообщение об ошибке расчёта метрологического допуска.</returns>
  internal static ShowMessageModel BuildToleranceCalculationError(string details)
  {
    return new ShowMessageModel(
      "Ошибка расчёта метрологического допуска",
      message: details,
      type: ShowMessageModel.MessageType.Error);
  }

  /// <summary>
  /// Формирует заголовок сводных результатов метрологического режима.
  /// </summary>
  /// <param name="command">Метрологический режим.</param>
  /// <returns>Заголовок сводных результатов режима.</returns>
  internal static ShowMessageModel BuildResultHeader(MeasurementTypeCommand command)
  {
    string displayName = command.GetCommandDisplayInfo().DisplayName;
    return new ShowMessageModel($"Результаты режима {displayName}");
  }

  /// <summary>
  /// Формирует сообщение о предельной погрешности метрологического режима.
  /// </summary>
  /// <param name="command">Метрологический режим.</param>
  /// <param name="value">Значение предельной погрешности.</param>
  /// <param name="isPositive">Признак положительной погрешности.</param>
  /// <returns>Сообщение о предельной погрешности.</returns>
  internal static ShowMessageModel BuildExtremeError(
    MeasurementTypeCommand command,
    double value,
    bool isPositive)
  {
    string unit = command.GetCommandDisplayInfo().Unit;
    string direction = isPositive ? "положительная" : "отрицательная";

    return new ShowMessageModel(
      $"Максимальная {direction} погрешность",
      message: MeasurementValueFormatter.FormatWithUnit(value, unit),
      type: ShowMessageModel.MessageType.Info)
    {
      IndentLevel = 1,
    };
  }
}
