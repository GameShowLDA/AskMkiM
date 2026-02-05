using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandAnalyser.Model.Ie;
using Ask.Engine.ControlCommandExecutor.BaseStrategies;
using Ask.Engine.ControlCommandExecutor.BaseStrategies.Data;
using Ask.Engine.ControlCommandExecutor.Execution;

namespace Ask.Engine.ControlCommandExecutor.Executors
{
  internal class IeCommandExecutor : CommandExecutorBase, ICommandExecutor
  {
    public string Mnemonic => EnumExtensions.GetDisplayInfo(MeasurementTypeCommand.IE).DisplayName;
    private double firstValue = 0;
    private double secondValue = 1000;

    public async Task ExecuteAsync(CommandExecutionContext context, ProtocolModel protocolModel)
    {
      var command = GetRequiredCommand<IeCommandModel>(context);
      var nameCommand = $"{command.CommandNumber} {command.Mnemonic}";
      var message = BuildSourceLinesMessage(command);
      List<ShowMessageModel> errorMessage = new();
      List<ShowMessageModel> infoMessage = new();
      
      SetActiveLine(context, command);
      BreakpointHandler.Handle(command, context.Console);

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

      if (command.LowerLimitCapacity.HasValue)
      {
        firstValue = command.LowerLimitCapacity.Value;
      }

      if (command.HigherLimitCapacity.HasValue)
      {
        secondValue = command.HigherLimitCapacity.Value;
      }

      ConnectedPointChecker.PerformMeasurementAsync measure = ResistanceMeasure;

      ConnectedPointContext pointContext = new ConnectedPointContext();
      pointContext.SchemeModel = command.Scheme;
      pointContext.CommandManager = context.CommandExecutionManager;
      pointContext.CommandModel = command;
      pointContext.MessageService = context.Console;
      pointContext.Value = (firstValue + secondValue) / 2;
      pointContext.LowerLimit = firstValue;
      pointContext.HigherLimit = secondValue;
      pointContext.PerformMeasurementAsync = measure;
      pointContext.Unit = "пкф";
      pointContext.UnitMnemonic = "C";
      pointContext.TypeCommand = MeasurementTypeCommand.IE;

      if (command.AlgorithmKey.Contains("Д"))
      {
        pointContext.IsProtocolAttribute = true;
      }

      var messageResult = await ConnectedPointChecker.CheckSequenceAsync(pointContext);
      errorMessage.AddRange(messageResult.errorMessage);
      infoMessage.AddRange(messageResult.infoMessage);

      await DeviceManager.RelayModule.PointManager.ResetAllPointsAsync(relayModules, context.Console);

      if (errorMessage.Count > 0)
      {
        protocolModel.Errors.Add(nameCommand, errorMessage);
      }
      if (infoMessage.Count > 0)
      {
        protocolModel.Info.Add(nameCommand, infoMessage);
      }
    }

    /// <summary>
    /// Выполняет измерение между уже подключёнными точками.
    /// Предполагается, что коммутация завершена заранее.
    /// </summary>
    /// <returns>Задача, представляющая измерение.</returns>
    private async Task<(bool, double)> ResistanceMeasure(double value, IUserInteractionService messageService, CancellationToken cancellationToken, double errorResistance = 0)
    {
      var meter = EquipmentService.GetFastMeterOrThrow(messageService);
      double answer = 0;

      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        answer = await meter.CapacitanceManager.MeasureCapacitanceAsync(value, userMessageService: messageService);
        return await MessageManager.ShowMeasurementResultAsync(messageService, MeasurementTypeCommand.IE, firstValue, answer, answer);

      }, messageService);

      return result;
    }

    private async Task SettingFastMeter(IFastMeter meter, IUserInteractionService userMessageService)
    {
      await meter.CapacitanceManager.SetCapacitanceModeAsync(userMessageService);
    }
  }
}
