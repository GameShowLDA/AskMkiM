using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Engine.ControlCommandAnalyser;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandExecutor.BaseStrategies;
using Ask.Engine.ControlCommandExecutor.BaseStrategies.Data;
using Ask.Engine.ControlCommandExecutor.Execution;

namespace Ask.Engine.ControlCommandExecutor.Executors
{
  internal class SiCommandExecutor : CommandExecutorBase, ICommandExecutor
  {
    public string Mnemonic => EnumExtensions.GetDisplayInfo(MeasurementTypeCommand.SI).DisplayName;

    private double firstValue = 0;
    public async Task ExecuteAsync(CommandExecutionContext context, ProtocolModel protocolModel)
    {
      var command = GetRequiredCommand<SiCommandModel>(context);
      var nameCommand = $"{command.CommandNumber} {command.Mnemonic}";
      var message = string.Empty;
      SetActiveLine(context, command);

      if (context.IsInvokedByAnotherCommand)
      {
        try
        {
          nameCommand = $"{command.CommandNumber.Split(' ').First()} ПИ/{command.Mnemonic}{command.CommandNumber.Split(' ').Last()}";
        }
        catch
        {
          nameCommand = $"{command.CommandNumber} ПИ/{command.Mnemonic}";
        }

        message = nameCommand;
      }

      message += BuildSourceLinesMessage(command);
      await context.Console.ShowMessageAsync(ExecutorMessageBuilder.BuildCommandExecutionMessage(nameCommand, message), IsBlockStart: true);
      await DeviceManager.ShowDevicesPreparationMessageIfNeededAsync(context);

      var points = DeviceManager.RelayModule.PointManager.CollectPoints(command);
      await EquipmentService.ValidatePointsExistInAnalyzedPointsAsync(points, context.Console);

      var relayModules = DeviceManager.RelayModule.PrepareRelayModules(points, context);
      await DeviceManager.RelayModule.BusManager.ConnectAllBusLinesAsync(relayModules, context.Console);

      var dbc = EquipmentService.GetSwitchingDevice();
      await DeviceManager.SwitchModuleManager.DeviceConnectionManager.ConnectBreakdownTester(dbc, context.Console);

      var breakDown = await EquipmentService.GetBreakdownTesterOrThrow(context.Console);
      await SettingBreakdown(breakDown, context.Console, command.Time.Value, command.Resistance.Value, command.Voltage.Value);

      List<ShowMessageModel> errorMessage = new();

      NodeFullContext nodeFullContext = new NodeFullContext(context, command, command, command.Resistance.Value + 1, command.Resistance.Value, -1);
      nodeFullContext.IsInvokedByAnotherCommand = context.IsInvokedByAnotherCommand;

      MethodExecutionContext methodExecutionContext = nodeFullContext.CreateChild<MethodExecutionContext>();
      NodeAccumulationContext nodeAccumulationContext = nodeFullContext.CreateChild<NodeAccumulationContext>();
      PairwiseFirstPointContext pairwiseFirstPointContext = nodeFullContext.CreateChild<PairwiseFirstPointContext>();
      nodeFullContext.PerformMeasurementAsync = NodeFullPerformMeasurementAsync;
      methodExecutionContext.PerformMeasurementAsync = NodeFullPerformMeasurementAsync;
      pairwiseFirstPointContext.PerformMeasurementAsync = NodeAccumulationPerformMeasurementAsync;
      nodeAccumulationContext.PerformMeasurementAsync = NodeAccumulationPerformMeasurementAsync;
      firstValue = nodeFullContext.LowerLimit;

      DisconnectionCheckRequest disconnectionCheckRequest = new DisconnectionCheckRequest()
      {
        AlgorithmKey = command.AlgorithmKey,
        NodeFullContext = nodeFullContext,
        MethodExecutionContext = methodExecutionContext,
        PairwiseFirstPointContext = pairwiseFirstPointContext,
        NodeAccumulationContext = nodeAccumulationContext
      };

      var messageResult = await DisconnectionCheckExecutor.ExecuteAsync(disconnectionCheckRequest);
      errorMessage.AddRange(messageResult.Errors);

      await PointFormater.MessageResult(errorMessage, context.Console);

      if (errorMessage.Count > 0)
      {
        protocolModel.AddErrors(nameCommand, errorMessage);
      }

      await CompleteProtocolCommandAsync(context, protocolModel, nameCommand);
    }
    private async Task SettingBreakdown(IBreakdownTester breakDown, IUserInteractionService userMessageService, double time, double resistance, double voltage)
    {
      string name = breakDown.Name;
      int numberChassis = breakDown.NumberChassis;
      int number = breakDown.Number;

      if (DeviceDisplayConfig.GetExecutionParametersVisibility())
      {
        await userMessageService.ShowMessageAsync(ExecutorMessageBuilder.BuildBreakdownTesterSetupMessage());
      }

      await breakDown.IrManger.Mode.SetModeAsync(userMessageService);
      await breakDown.IrManger.Time.SetTestTimeAsync(time, userMessageService);
      await breakDown.IrManger.ResistanceLimits.SetLowResistanceLimitAsync(resistance, userMessageService);
      await breakDown.IrManger.Voltage.SetVoltageAsync(voltage, userMessageService);
    }

    /// <summary>
    /// Выполняет измерение между уже подключёнными точками.
    /// Предполагается, что коммутация завершена заранее.
    /// </summary>
    /// <returns>Задача, представляющая измерение.</returns>
    private async Task<(bool, double)> NodeAccumulationPerformMeasurementAsync(double value, IUserInteractionService messageService, CancellationToken cancellationToken, double errorResistance = 0, VoltageEnum.Type typeVoltage = VoltageEnum.Type.ACW)
    {
      var breadDown = await EquipmentService.GetBreakdownTesterOrThrow(messageService);

      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var answer = (await breadDown.IrManger.Measure.MeasureAsync(value, firstValue)).value;
        var result = await MessageManager.ShowMeasurementResultAsync(messageService, MeasurementTypeCommand.SI, firstValue, -1, answer);

        return result;
      }, messageService);

      return result;
    }

    /// <summary>
    /// Выполняет измерение между уже подключёнными точками.
    /// Предполагается, что коммутация завершена заранее.
    /// </summary>
    /// <returns>Задача, представляющая измерение.</returns>
    private async Task<(bool, double)> NodeFullPerformMeasurementAsync(double value, IUserInteractionService messageService, CancellationToken cancellationToken, double errorResistance = 0, VoltageEnum.Type typeVoltage = VoltageEnum.Type.ACW)
    {
      var breadDown = await EquipmentService.GetBreakdownTesterOrThrow(messageService);
      (double Value, string Unit) answer = (-1, string.Empty);
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        messageService.GetCancellationToken().ThrowIfCancellationRequested();
        answer = await breadDown.IrManger.Measure.MeasureAsync(value, value, 60000);

        var result = await MessageManager.ShowMeasurementResultAsync(messageService, MeasurementTypeCommand.SI, firstValue, -1, answer.Value);
        return result;

      }, messageService);

      return result;
    }
  }
}
