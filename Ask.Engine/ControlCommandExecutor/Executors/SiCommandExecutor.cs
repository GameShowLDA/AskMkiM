using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device.Breakdown;
using Ask.Core.Services.Errors.Device.DeviceBusCommutation;
using Ask.Core.Services.Errors.Device.ModuleRelayControl;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Engine.ControlCommandAnalyser;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandExecutor.BaseStrategies;
using Ask.Engine.ControlCommandExecutor.BaseStrategies.Data;
using Ask.Engine.ControlCommandExecutor.Execution;
using Ask.Engine.ControlCommandExecutor.Executors.Interface;

namespace Ask.Engine.ControlCommandExecutor.Executors
{
  internal class SiCommandExecutor : ICommandExecutor
  {
    public string Mnemonic => EnumExtensions.GetDisplayInfo(MeasurementTypeCommand.SI).DisplayName;

    private double firstValue = 0;
    public async Task ExecuteAsync(CommandExecutionContext context, ProtocolModel protocolModel)
    {
      var command = context.Command as SiCommandModel;
      context.TranslationControl.SetActiveLine(command.FormattedStartLineNumber);

      string nameCommand = $"{command.CommandNumber} {command.Mnemonic}";

      string message = string.Empty;

      foreach (var str in command.SourceLines)
      {
        message += "\r\n  " + str;
      }

      if (!string.IsNullOrEmpty(message))
      {
        await context.Console.ShowMessageAsync(new ShowMessageModel($"\r\nВыполнение команды {nameCommand}", headerColor: ShowMessageModel.SuccessMessage.TitleColor, message: message, type: ShowMessageModel.MessageType.Command) { IndentLevel = 1 }, IsBlockStart: true);
      }

      var points = command.Scheme?.GroupModels?
                  .SelectMany(chain => chain?.ChainModels ?? Enumerable.Empty<ChainModel>())
                  .SelectMany(part => part?.PointModels ?? Enumerable.Empty<PointModel>())
                  .ToList()
                  ?? new List<PointModel>();

      await EquipmentService.ValidatePointsExistInAnalyzedPointsAsync(points, context.Console);

      if (await DeviceDisplayConfig.GetExecutionParametersVisibilityAsync())
      {
        await context.Console.ShowMessageAsync(ExecutorMessageBuilder.BuildDevicesPreparationMessage());
      }

      var modules = points
          .Select(EquipmentService.GetModuleByPoint)
          .Where(m => m != null)
          .DistinctBy(m => (m.NumberChassis, m.Number))
          .ToList();

      await SettingModuleRelayControl(modules, context.Console);

      var dbc = EquipmentService.GetSwitchingDevice();

      await SettingsDeviceBusCommutatuion(dbc, context.Console);

      var breakDown = await EquipmentService.GetBreakdownTesterOrThrow(context.Console);
      await SettingBreakdown(breakDown, context.Console, command.Time.Value, command.Resistance.Value, command.Voltage.Value);

      List<ShowMessageModel> errorMessage = new();

      NodeFullContext nodeFullContext = new NodeFullContext();
      nodeFullContext.SchemeModel = command.Scheme;
      nodeFullContext.CommandManager = context.CommandExecutionManager;
      nodeFullContext.CommandModel = command;
      nodeFullContext.MessageService = context.Console;
      nodeFullContext.Value = command.Resistance.Value;
      nodeFullContext.LowerLimit = command.Resistance.Value;
      nodeFullContext.HigherLimit = -1;
      nodeFullContext.Unit = "МОм";
      nodeFullContext.UnitMnemonic = "R";
      nodeFullContext.TypeCommand = MeasurementTypeCommand.SI;

      MethodExecutionContext methodExecutionContext = nodeFullContext.CreateChild<MethodExecutionContext>();
      NodeAccumulationContext nodeAccumulationContext = nodeFullContext.CreateChild<NodeAccumulationContext>();

      firstValue = nodeFullContext.LowerLimit;

      if (command.AlgorithmKey.Contains("К"))
      {
        nodeFullContext.PerformMeasurementAsync = NodeFullPerformMeasurementAsync;
        var errMes = await NodeFullChecker.CheckSequenceAsync(nodeFullContext);
        errorMessage.AddRange(errMes);
      }
      else if (command.AlgorithmKey.Contains("Г"))
      {
        methodExecutionContext.PerformMeasurementAsync = NodeFullPerformMeasurementAsync;
        var errMes = await MethodExecutor.CheckSequenceAsync(methodExecutionContext);
        errorMessage.AddRange(errMes);
      }
      else if (command.AlgorithmKey.Contains("Т1"))
      {
        NodeAccumulationChecker.PerformMeasurementAsync measure = NodeAccumulationPerformMeasurementAsync;
        var errMes = await PairwiseFirstPointChecker.CheckSequenceAsync(command.Scheme, measure, context.CommandExecutionManager, command, context.Console, command.Resistance.Value);
        errorMessage.AddRange(errMes);
      }
      else
      {
        nodeAccumulationContext.PerformMeasurementAsync = NodeAccumulationPerformMeasurementAsync;
        var errMes = await NodeAccumulationChecker.CheckSequenceAsync(nodeAccumulationContext);
        errorMessage.AddRange(errMes);
      }
      await PointFormater.MessageResult(errorMessage, context.Console);


      await context.Console.ShowMessageAsync(new ShowMessageModel("Сброс точек") { IndentLevel = 1 });
      foreach (var item in modules)
      {
        await item.PointManager.DisconnectingAllPoint(context.Console);
      }
      if (errorMessage.Count > 0)
      {
        protocolModel.Errors.Add(nameCommand, errorMessage);
      }
    }

    private async Task SettingsDeviceBusCommutatuion(ISwitchingDevice dbc, IUserInteractionService userMessageService)
    {
      if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => dbc.ConnectorManager.ConnectBreakdownTester(userMessageService), userMessageService))
      {
        throw ConnectorExceptionFactory.ConnectBreakdownFailed(dbc.Name, dbc.NumberChassis, dbc.Number);
      }
    }
    private async Task SettingModuleRelayControl(List<IRelaySwitchModule> relaySwitchModules, IUserInteractionService userMessageService)
    {
      foreach (var module in relaySwitchModules)
      {
        await module.BusManager.ConnectBusAsync(SwitchingBus.A1, userMessageService: userMessageService);
        await module.BusManager.ConnectBusAsync(SwitchingBus.B1, userMessageService: userMessageService);
      }
    }

    private async Task SettingBreakdown(IBreakdownTester breakDown, IUserInteractionService userMessageService, double time, double resistance, double voltage)
    {
      string name = breakDown.Name;
      int numberChassis = breakDown.NumberChassis;
      int number = breakDown.Number;

      if (await DeviceDisplayConfig.GetExecutionParametersVisibilityAsync())
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
    private async Task<(bool, double)> NodeAccumulationPerformMeasurementAsync(double value, IUserInteractionService messageService, CancellationToken cancellationToken, VoltageEnum.Type typeVoltage = VoltageEnum.Type.ACW)
    {
      var breadDown = await EquipmentService.GetBreakdownTesterOrThrow(messageService);

      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var answer = (await breadDown.IrManger.Measure.MeasureAsync(value)).value;
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
    private async Task<(bool, double)> NodeFullPerformMeasurementAsync(double value, IUserInteractionService messageService, CancellationToken cancellationToken, VoltageEnum.Type typeVoltage = VoltageEnum.Type.ACW)
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
