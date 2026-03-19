using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Ks;
using Ask.Engine.ControlCommandExecutor.BaseStrategies;
using Ask.Engine.ControlCommandExecutor.BaseStrategies.Data;
using Ask.Engine.ControlCommandExecutor.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Engine.ControlCommandExecutor.Executors
{
  internal class NeCommandExecutor : CommandExecutorBase, ICommandExecutor
  {
    public string Mnemonic => EnumExtensions.GetDisplayInfo(MeasurementTypeCommand.NE).DisplayName;
    private double firstValue = 0;
    private double secondValue = -1;

    public async Task ExecuteAsync(CommandExecutionContext context, ProtocolModel protocolModel)
    {
      firstValue = 0;
      secondValue = 10000000;

      var command = GetRequiredCommand<NeCommandModel>(context);
      var nameCommand = $"{command.CommandNumber} {command.Mnemonic}";
      var message = BuildSourceLinesMessage(command);
      SetActiveLine(context, command);

      await context.Console.ShowMessageAsync(ExecutorMessageBuilder.BuildCommandExecutionMessage(nameCommand, message), IsBlockStart: true);

      List<ShowMessageModel> errorMessage = new();
      List<ShowMessageModel> infoMessage = new();

      await DeviceManager.ShowDevicesPreparationMessageIfNeededAsync(context);

      var points = DeviceManager.RelayModule.PointManager.CollectPoints(command);
      await EquipmentService.ValidatePointsExistInAnalyzedPointsAsync(points, context.Console);

      var relayModules = DeviceManager.RelayModule.PrepareRelayModules(points, context);
      await DeviceManager.RelayModule.BusManager.ConnectAllBusLinesAsync(relayModules, context.Console);

      var dbc = EquipmentService.GetSwitchingDevice();
      await DeviceManager.SwitchModuleManager.DeviceConnectionManager.ConnectMultimeter(dbc, context.Console);

      var meter = EquipmentService.GetFastMeterOrThrow(context.Console);
      await SettingMeter(meter, context.Console);

      if (command.LowerLimitVoltage.HasValue)
      {
        firstValue = command.LowerLimitVoltage.Value;
      }

      if (command.HigherLimitVoltage.HasValue)
      {
        secondValue = command.HigherLimitVoltage.Value;
      }

      ConnectedPointChecker.PerformMeasurementAsync measure;
      measure = DioideMeasure;


      ConnectedPointContext pointContext = new ConnectedPointContext();
      pointContext.SchemeModel = command.Scheme;
      pointContext.CommandManager = context.CommandExecutionManager;
      pointContext.CommandModel = command;
      pointContext.MessageService = context.Console;
      pointContext.LowerLimit = firstValue;
      pointContext.HigherLimit = secondValue;
      pointContext.PerformMeasurementAsync = measure;
      pointContext.Unit = "В";
      pointContext.UnitMnemonic = "Г";
      pointContext.TypeCommand = MeasurementTypeCommand.NE;

      if (secondValue != -1)
      {
        pointContext.Value = (firstValue + secondValue) / 2;
      }
      else
      {
        pointContext.Value = firstValue + 10;
      }

      var messageResult = await ConnectedPointChecker.CheckSequenceAsync(pointContext);
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

    /// <summary>
    /// Выполняет измерение между уже подключёнными точками.
    /// Предполагается, что коммутация завершена заранее.
    /// </summary>
    /// <returns>Задача, представляющая измерение.</returns>
    private async Task<(bool, double)> DioideMeasure(double value, IUserInteractionService messageService, CancellationToken cancellationToken, double errorResistance = 0)
    {
      var meter = EquipmentService.GetFastMeterOrThrow(messageService);
      double answer = 0;

      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        answer = await meter.DiodeManager.CheckDiodeAsync(value, firstValue, secondValue);

        if (answer < 0)
        {
          answer = 0;
        }

        return await MessageManager.ShowMeasurementResultAsync(messageService, MeasurementTypeCommand.NE, firstValue, secondValue, answer);
      }, messageService);

      return result;
    }

    private async Task SettingMeter(IFastMeter meter, IUserInteractionService userMessageService)
    {
      await meter.DiodeManager.SetDiodeModeAsync(userMessageService);
    }
  }
}