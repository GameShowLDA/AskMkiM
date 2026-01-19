using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Engine.ControlCommandAnalyser;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandAnalyser.Model.Pr;
using Ask.Engine.ControlCommandExecutor.BaseStrategies;
using Ask.Engine.ControlCommandExecutor.BaseStrategies.Data;
using Ask.Engine.ControlCommandExecutor.Execution;
using Ask.Engine.ControlCommandExecutor.Executors.Interface;

namespace Ask.Engine.ControlCommandExecutor.Executors
{
  internal class PrCommandExecutor : ICommandExecutor
  {
    public string Mnemonic => EnumExtensions.GetDisplayInfo(MeasurementTypeCommand.PR).DisplayName;
    private double firstValue = 0;
    private double secondValue = -1;
    private bool continuityManager = true;

    public async Task ExecuteAsync(CommandExecutionContext context, ProtocolModel protocolModel)
    {
      var command = context.Command as PrCommandModel;

      context.TranslationControl.SetActiveLine(command.FormattedStartLineNumber);

      string nameCommand = $"{command.CommandNumber} {command.Mnemonic}";
      string message = string.Empty;

      foreach (var str in command.SourceLines)
      {
        message += "\r\n  " + str;
      }

      await context.Console.ShowMessageAsync(new ShowMessageModel($"\r\nВыполнение команды {nameCommand}", headerColor: ShowMessageModel.SuccessMessage.TitleColor, message: message, type: ShowMessageModel.MessageType.Command) { IndentLevel = 1 }, IsBlockStart: true);

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

      var meter = EquipmentService.GetFastMeterOrThrow(context.Console);

      double resistance = 0;
      if (command.LowerLimitResistance.HasValue)
      {
        firstValue = command.LowerLimitResistance.Value;
        resistance = command.LowerLimitResistance.Value;
      }
      else
      {
        firstValue = 0;
      }

      if (command.HigherLimitResistance.HasValue)
      {
        secondValue = command.HigherLimitResistance.Value;
      }
      else
      {
        secondValue = meter.MaxContinuityResistance;
      }

      if (secondValue >= 1000)
      {
        continuityManager = false;
      }

      await SettingMeter(meter, context.Console);

      MethodExecutionContext methodExecutionContext = new MethodExecutionContext();
      methodExecutionContext.SchemeModel = command.Scheme;
      methodExecutionContext.CommandManager = context.CommandExecutionManager;
      methodExecutionContext.CommandModel = command;
      methodExecutionContext.MessageService = context.Console;
      methodExecutionContext.Value = resistance;
      methodExecutionContext.LowerLimit = command.LowerLimitResistance.Value;
      methodExecutionContext.HigherLimit = command.HigherLimitResistance != null ? command.HigherLimitResistance.Value : -1;
      methodExecutionContext.Unit = "Ом";
      methodExecutionContext.UnitMnemonic = "R";
      methodExecutionContext.TypeCommand = MeasurementTypeCommand.PR;

      NodeAccumulationContext nodeAccumulationContext = methodExecutionContext.CreateChild<NodeAccumulationContext>();
      PairwiseFirstPointContext pairwiseFirstPointContext = methodExecutionContext.CreateChild<PairwiseFirstPointContext>();

      List<ShowMessageModel> errorMessage = new();

      if (!command.AlgorithmKey.Contains("ЗС"))
      {
        ConnectedPointChecker.PerformMeasurementAsync measurePointConnected = ConnectedPointCheckerMeasurementAsync;

        ConnectedPointContext connectedPointContext = methodExecutionContext.CreateChild<ConnectedPointContext>();
        connectedPointContext.PerformMeasurementAsync = measurePointConnected;

        var connectErrMes = await ConnectedPointChecker.CheckSequenceAsync(connectedPointContext);
        errorMessage.AddRange(connectErrMes);
      }
      if (!command.AlgorithmKey.Contains("ЗР"))
      {
        if (command.AlgorithmKey.Contains("К"))
        {
          NodeFullChecker.PerformMeasurementAsync measure = NodeFullPerformMeasurementAsync;
          NodeFullContext nodeFullContext = methodExecutionContext.CreateChild<NodeFullContext>();
          nodeFullContext.PerformMeasurementAsync = measure;

          var errMes = await NodeFullChecker.CheckSequenceAsync(nodeFullContext);
          errorMessage.AddRange(errMes);
        }
        else if (command.AlgorithmKey.Contains("Г"))
        {
          NodeFullChecker.PerformMeasurementAsync measure = NodeFullPerformMeasurementAsync;
          methodExecutionContext.PerformMeasurementAsync = measure;

          var errMes = await MethodExecutor.CheckSequenceAsync(methodExecutionContext);
          errorMessage.AddRange(errMes);
        }
        else if (command.AlgorithmKey.Contains("Т1"))
        {
          pairwiseFirstPointContext.PerformMeasurementAsync = NodeAccumulationPerformMeasurementAsync;
          var errMes = await PairwiseFirstPointChecker.CheckSequenceAsync(pairwiseFirstPointContext);
          errorMessage.AddRange(errMes);
        }
        else
        {
          nodeAccumulationContext.PerformMeasurementAsync = NodeAccumulationPerformMeasurementAsync;
          var errMes = await NodeAccumulationChecker.CheckSequenceAsync(nodeAccumulationContext);
          errorMessage.AddRange(errMes);
        }
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


    private async Task SettingModuleRelayControl(List<IRelaySwitchModule> relaySwitchModules, IUserInteractionService userMessageService)
    {
      foreach (var module in relaySwitchModules)
      {
        await module.BusManager.ConnectBusAsync(SwitchingBus.A1, userMessageService: userMessageService);
        await module.BusManager.ConnectBusAsync(SwitchingBus.B1, userMessageService: userMessageService);
      }
    }

    private async Task SettingsDeviceBusCommutatuion(ISwitchingDevice dbc, IUserInteractionService userMessageService)
    {
      await dbc.ConnectorManager.ConnectMultimeter(SwitchingBusNew.AB1, userMessageService);
    }

    private async Task SettingMeter(IFastMeter meter, IUserInteractionService userMessageService)
    {
      string name = meter.Name;
      int numberChassis = meter.NumberChassis;
      int number = meter.Number;

      if (await DeviceDisplayConfig.GetExecutionParametersVisibilityAsync())
      {
        await userMessageService.ShowMessageAsync(ExecutorMessageBuilder.BuildMultimeterSetupMessage());
      }

      if (continuityManager)
      {
        await meter.ContinuityManager.SetContinuityModeAsync(userMessageService);
      }
      else
      {
        await meter.ResistanceManager.SetResistanceModeAsync(userMessageService);
      }
    }

    /// <summary>
    /// Выполняет измерение между уже подключёнными точками метод накапливающего узла.
    /// Предполагается, что коммутация завершена заранее.
    /// </summary>
    /// <returns>Задача, представляющая измерение.</returns>
    private async Task<(bool, double)> NodeAccumulationPerformMeasurementAsync(double resistance, IUserInteractionService messageService, CancellationToken cancellationToken, VoltageEnum.Type type = VoltageEnum.Type.ACW)
    {
      var fastMeter = EquipmentService.GetFastMeterOrThrow(messageService);

      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        double answer = 0;

        if (continuityManager)
        {
          answer = await fastMeter.ContinuityManager.CheckContinuityAsync(resistance);
        }
        else
        {
          answer = await fastMeter.ResistanceManager.MeasureResistanceAsync(resistance);
        }

        return await MessageManager.ShowMeasurementResultAsync(messageService, MeasurementTypeCommand.PR, firstValue, -1, answer);

      }, messageService);

      return result;
    }

    /// <summary>
    /// Выполняет измерение между уже подключёнными точками метод полного узла.
    /// Предполагается, что коммутация завершена заранее.
    /// </summary>
    /// <returns>Задача, представляющая измерение.</returns>
    private async Task<(bool, double)> NodeFullPerformMeasurementAsync(double resistance, IUserInteractionService messageService, CancellationToken cancellationToken, VoltageEnum.Type type = VoltageEnum.Type.ACW)
    {
      var fastMeter = EquipmentService.GetFastMeterOrThrow(messageService);
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        double answer = -1;

        if (continuityManager)
        {
          answer = await fastMeter.ContinuityManager.CheckContinuityAsync(resistance, messageService);
        }
        else
        {
          answer = await fastMeter.ResistanceManager.MeasureResistanceAsync(resistance);
        }

        return await MessageManager.ShowMeasurementResultAsync(messageService, MeasurementTypeCommand.PR, firstValue, -1, answer);
      }, messageService);

      return result;
    }

    /// <summary>
    /// Выполняет измерение между уже подключёнными точками методом первой точки.
    /// Предполагается, что коммутация завершена заранее.
    /// </summary>
    /// <returns>Задача, представляющая измерение.</returns>
    private async Task<(bool, double)> ConnectedPointCheckerMeasurementAsync(double resistance, IUserInteractionService messageService, CancellationToken cancellationToken)
    {
      var fastMeter = EquipmentService.GetFastMeterOrThrow(messageService);
      double answer = -1;

      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        if (await ExecutionConfig.GetIsIdleModeEnabled() && await ExecutionConfig.GetIsErrorSimulationEnabled())
        {
          answer = new Random().Next(0, (int)secondValue + 1000);
        }
        else
        {
          if (continuityManager)
          {
            answer = await fastMeter.ContinuityManager.CheckContinuityAsync(resistance, messageService);
          }
          else
          {
            answer = await fastMeter.ResistanceManager.MeasureResistanceAsync(resistance);
          }
        }

        var result = answer >= firstValue && answer <= secondValue;

        if (!result || await DeviceDisplayConfig.GetMeasurementResultsVisibilityAsync())
        {
          await messageService.ShowMessageAsync(new ShowMessageModel("Результат измерения сопротивления", message: $"{answer} Ом", type: result ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error) { IndentLevel = 1 }, skipPause: true);
        }

        if (!result)
        {
          await messageService.ShowMessageAsync(new ShowMessageModel("Диапазон допускаемых значений", message: $"от {firstValue} до {secondValue} Ом") { IndentLevel = 2 }, skipPause: true);
        }

        return result;
      }, messageService);

      return (result, answer);
    }
  }
}
