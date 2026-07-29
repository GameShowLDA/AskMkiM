using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Static.Messages;

namespace Ask.Engine.ControlCommandExecutor.BaseStrategies.Data
{
  /// <summary>
  /// Предоставляет методы для отображения результатов измерений.
  /// </summary>
  internal class MessageManager
  {
    /// <summary>
    /// Отображает результат измерения и определяет, соответствует ли он заданным пределам.
    /// </summary>
    /// <param name="messageService">Сервис отображения сообщений.</param>
    /// <param name="measurementTypeCommand">Тип выполняемого измерения.</param>
    /// <param name="lowerLimit">Нижняя граница допустимого значения.</param>
    /// <param name="upperLimit">Верхняя граница допустимого значения.</param>
    /// <param name="value">Измеренное значение.</param>
    /// <param name="chains">Обозначение измеряемых цепей.</param>
    /// <param name="isOverloadExpected">
    /// <see langword="true"/>, если ожидается проверка значения на перегрузку прибора;
    /// в противном случае — <see langword="false"/>.
    /// </param>
    /// <returns>
    /// Кортеж, содержащий результат проверки измерения и итоговое измеренное значение.
    /// </returns>
    public static async Task<(bool, double)> ShowMeasurementResultAsync(
      IUserInteractionService messageService,
      MeasurementTypeCommand measurementTypeCommand,
      MeasurementRange measurementRange,
      string? chains = null,
      bool isOverloadExpected = false)
    {
      var random = new Random();
      double value = measurementRange.TargetValue;

      if (ExecutionConfig.GetIsIdleModeEnabled() && ExecutionConfig.GetIsErrorSimulationEnabled())
      {
        if (measurementRange.UpperBound != -1)
        {
          value = random.NextDouble() * ((measurementRange.UpperBound + 1) * 2);
        }
        else
        {
          value = random.NextDouble();
        }
      }

      bool result = isOverloadExpected
        ? IsOverloadValue(value)
        : measurementRange.UpperBound != -1 ? value >= measurementRange.LowerBound && value <= measurementRange.UpperBound : value >= measurementRange.LowerBound;

      if (messageService != null && (!result || DeviceDisplayConfig.GetMeasurementResultsVisibility()))
      {
        var message = ExecutorMessageBuilder.BuildMeasurementResultMessage(measurementTypeCommand, measurementRange.LowerBound, measurementRange.UpperBound, value, chains: chains);
        message.Status = result ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error;
        message.IndentLevel = 2;

        await messageService.ShowMessageAsync(message, skipPause: true);
      }

      return (result, value);
    }

    /// <summary>
    /// Определяет, соответствует ли измеренное значение перегрузке прибора.
    /// </summary>
    /// <param name="value">Измеренное значение.</param>
    /// <returns>
    /// <see langword="true"/>, если значение соответствует перегрузке прибора.
    /// В противном случае — <see langword="false"/>.
    /// </returns>
    private static bool IsOverloadValue(double value) => MeasurementValueFormatter.IsOverloadValue(value);
  }
}
