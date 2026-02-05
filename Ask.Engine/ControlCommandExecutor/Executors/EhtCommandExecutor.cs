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
using Ask.Engine.ControlCommandExecutor.Executors.Interface;

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
      SetActiveLine(context, command);

      BreakpointHandler.Handle(command, context.Console);
      await context.Console.ShowMessageAsync(new ShowMessageModel($"\r\nВыполнение команды {nameCommand}", headerColor: ShowMessageModel.SuccessMessage.TitleColor, message: message, type: ShowMessageModel.MessageType.Command) { IndentLevel = 1 }, IsBlockStart: true);

      List<ShowMessageModel> errorMessage = new();
      List<ShowMessageModel> infoMessage = new();


      var points = command.Scheme?.GroupModels?
            .SelectMany(chain => chain?.ChainModels ?? Enumerable.Empty<ChainModel>())
            .SelectMany(part => part?.PointModels ?? Enumerable.Empty<PointModel>())
            .ToList()
            ?? new List<PointModel>();

      if (DeviceDisplayConfig.GetExecutionParametersVisibility())
      {
        await context.Console.ShowMessageAsync(ExecutorMessageBuilder.BuildDevicesPreparationMessage());
      }

      var modules = points
         .Select(EquipmentService.GetModuleByPoint)
         .Where(m => m != null)
         .DistinctBy(m => (m.NumberChassis, m.Number))
         .ToList();

      await DeviceManager.RelayModule.BusManager.ConnectAllBusLinesAsync(modules, context.Console);

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

      PairwiseFirstPointAltContext pairwiseFirstPointCheckerAlt = new PairwiseFirstPointAltContext();
      pairwiseFirstPointCheckerAlt.SchemeModel = command.Scheme;
      pairwiseFirstPointCheckerAlt.CommandManager = context.CommandExecutionManager;
      pairwiseFirstPointCheckerAlt.CommandModel = command;
      pairwiseFirstPointCheckerAlt.MessageService = context.Console;
      pairwiseFirstPointCheckerAlt.Value = (firstValue + secondValue) / 2;
      pairwiseFirstPointCheckerAlt.CabelResistance = cabelResistance;
      pairwiseFirstPointCheckerAlt.TypeCommand = MeasurementTypeCommand.EHT;
      pairwiseFirstPointCheckerAlt.LowerLimit = command.LowerLimitResistance.Value;
      pairwiseFirstPointCheckerAlt.HigherLimit = command.HigherLimitResistance.Value;

      if (command.AlgorithmKey.Contains("Д"))
      {
        pairwiseFirstPointCheckerAlt.IsProtocolAttribute = true;
      }

      var messageResult = await PairwiseFirstPointCheckerAlt.CheckSequenceAsync(pairwiseFirstPointCheckerAlt);
      errorMessage.AddRange(messageResult.errorMessage);
      infoMessage.AddRange(messageResult.infoMessage);

      await DeviceManager.RelayModule.PointManager.ResetAllPointsAsync(modules, context.Console);

      if (errorMessage.Count > 0)
      {
        protocolModel.Errors.Add(nameCommand, errorMessage);
      }
      if (infoMessage.Count > 0)
      {
        protocolModel.Info.Add(nameCommand, infoMessage);
      }
    }
    private async Task SettingFastMeter(IFastMeter meter, IUserInteractionService userMessageService)
    {
      await meter.ContinuityManager.SetContinuityModeAsync(userMessageService);
    }
  }
}
