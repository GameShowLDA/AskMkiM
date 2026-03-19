using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Static.Messages;

namespace Ask.Engine.ControlCommandExecutor.BaseStrategies.Data
{
  internal class MessageManager
  {
    public static async Task<(bool, double)> ShowMeasurementResultAsync(IUserInteractionService messageService, MeasurementTypeCommand measurementTypeCommand, double lowerLimit, double upperLimit, double value, string? chains = null)
    {
      var random = new Random();

      if (ExecutionConfig.GetIsIdleModeEnabled() && await ExecutionConfig.GetIsErrorSimulationEnabled())
      {
        if (upperLimit != -1)
        {
          value = random.NextDouble() * ((upperLimit + 1) * 2);
        }
        else
        {
          value = random.NextDouble();
        }
      }

      bool result = upperLimit != -1 ? value >= lowerLimit && value <= upperLimit : value >= lowerLimit;

      if (messageService != null && (!result || DeviceDisplayConfig.GetMeasurementResultsVisibility()))
      {
        var message = ExecutorMessageBuilder.BuildMeasurementResultMessage(measurementTypeCommand, lowerLimit, upperLimit, value, chains: chains);
        message.Status = result ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error;
        message.IndentLevel = 2;

        await messageService.ShowMessageAsync(message, skipPause: true);
      }

      return (result, value);
    }
  }
}
