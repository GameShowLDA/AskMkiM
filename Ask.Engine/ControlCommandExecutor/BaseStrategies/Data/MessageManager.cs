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
      if (await ExecutionConfig.GetIsIdleModeEnabled() && await ExecutionConfig.GetIsErrorSimulationEnabled())
      {
        if (upperLimit != -1)
        {
          value = new Random().Next(0, (int)upperLimit * 2);
        }
        else
        {
          value = new Random().Next();
        }
      }

      bool result = upperLimit != -1 ? value >= lowerLimit && value <= upperLimit : value >= lowerLimit;

      if (!result || await DeviceDisplayConfig.GetMeasurementResultsVisibilityAsync())
      {
        bool overload = false;
        if (measurementTypeCommand == MeasurementTypeCommand.KC || measurementTypeCommand == MeasurementTypeCommand.PR || measurementTypeCommand == MeasurementTypeCommand.EHT)
        {
          overload = value.ToString() != "9,9E+37" ? false : true;
        }

        var message = ExecutorMessageBuilder.BuildMeasurementResultMessage(measurementTypeCommand, lowerLimit, upperLimit, value, chains: chains, overload: overload);
        message.Status = result ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error;
        message.IndentLevel = 2;

        await messageService.ShowMessageAsync(message, skipPause: true);
      }

      return (result, value);
    }
  }
}
