using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandExecutor.BaseStrategies;
using Ask.Engine.ControlCommandExecutor.BaseStrategies.Data;
using Ask.Engine.ControlCommandExecutor.Execution;

namespace Ask.Engine.ControlCommandExecutor.Executors
{
  internal class EhtCommandExecutor : CommandExecutorBase, ICommandExecutor
  {
    public string Mnemonic => "ЭТ";
    private double firstValue = 0;
    private double secondValue = 1000;
    public async Task ExecuteAsync(CommandExecutionContext context, ProtocolModel protocolModel)
    {
      var command = GetRequiredCommand<EhtCommandModel>(context);
      var nameCommand = $"{command.CommandNumber} {command.Mnemonic}";
      var message = BuildSourceLinesMessage(command);
      List<ShowMessageModel> errorMessage = new();
      List<ShowMessageModel> infoMessage = new();

      SetActiveLine(context, command);

      await context.Console.ShowMessageAsync(ExecutorMessageBuilder.BuildCommandExecutionMessage(nameCommand, message), IsBlockStart: true);
      await DeviceManager.ShowDevicesPreparationMessageIfNeededAsync(context);

      var points = DeviceManager.RelayModule.PointManager.CollectPoints(command);
      await EquipmentService.ValidatePointsExistInAnalyzedPointsAsync(points, context.Console);

      var relayModules = DeviceManager.RelayModule.PrepareRelayModules(points, context);
      await DeviceManager.RelayModule.BusManager.ConnectAllBusLinesAsync(relayModules, context.Console);

      var dbc = EquipmentService.GetSwitchingDevice();
      await DeviceManager.SwitchModuleManager.DeviceConnectionManager.ConnectMultimeter(dbc, context.Console);

      var meter = EquipmentService.GetFastMeterOrThrow(context.Console);
      await SettingFastMeter(meter, context.Console);

      if (command.LowerLimitResistance.HasValue)
      {
        firstValue = command.LowerLimitResistance.Value;
      }

      if (command.HigherLimitResistance.HasValue)
      {
        secondValue = command.HigherLimitResistance.Value;
      }

      var cabelResistance = command.CabelResistance != null ? command.CabelResistance.Value : 0;

      PairwiseFirstPointAltContext pairwiseFirstPointCheckerAlt = new PairwiseFirstPointAltContext(
        context,
        command,
        command,
        (firstValue + secondValue) / 2,
        firstValue,
        secondValue);
      pairwiseFirstPointCheckerAlt.CabelResistance = cabelResistance;

      if (command.AlgorithmKey.Contains("Д"))
      {
        pairwiseFirstPointCheckerAlt.IsProtocolAttribute = true;
      }

      var messageResult = await PairwiseFirstPointCheckerAlt.CheckSequenceAsync(pairwiseFirstPointCheckerAlt);
      errorMessage.AddRange(messageResult.errorMessage);
      infoMessage.AddRange(messageResult.infoMessage);

      if (errorMessage.Count > 0)
      {
        protocolModel.AddErrors(nameCommand, errorMessage);
      }
      if (infoMessage.Count > 0)
      {
        protocolModel.AddInfo(nameCommand, infoMessage);
      }
    }

    private async Task SettingFastMeter(IFastMeter meter, IUserInteractionService userMessageService)
    {
      await meter.ContinuityManager.SetContinuityModeAsync(userMessageService);
    }
  }
}

