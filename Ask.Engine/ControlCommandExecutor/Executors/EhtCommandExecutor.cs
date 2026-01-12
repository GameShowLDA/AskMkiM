using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device.DeviceBusCommutation;
using Ask.Core.Services.Errors.Device.ModuleRelayControl;
using Ask.Core.Services.Errors.Device.Multimeter;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandExecutor.BaseStrategies;
using Ask.Engine.ControlCommandExecutor.Execution;
using Ask.Engine.ControlCommandExecutor.Executors.Interface;

namespace Ask.Engine.ControlCommandExecutor.Executors
{
  internal class EhtCommandExecutor : ICommandExecutor
  {
    public string Mnemonic => "ЭТ";
    private double firstValue = 0;
    private double secondValue = 1000;
    public async Task ExecuteAsync(CommandExecutionContext context, ProtocolModel protocolModel)
    {
      var command = context.Command as EhtCommandModel;
      context.TranslationControl.SetActiveLine(command.FormattedStartLineNumber);

      string nameCommand = $"{command.CommandNumber} {command.Mnemonic}";
      string message = string.Empty;

      foreach (var str in command.SourceLines)
      {
        message += "\r\n  " + str;
      }

      await context.Console.ShowMessageAsync(new ShowMessageModel($"\r\nВыполнение команды {nameCommand}", headerColor: ShowMessageModel.SuccessMessage.TitleColor, message: message, type: ShowMessageModel.MessageType.Command) { IndentLevel = 1 }, IsBlockStart: true);

      List<ShowMessageModel> errorMessage = new();

      var points = command.Scheme?.GroupModels?
            .SelectMany(chain => chain?.ChainModels ?? Enumerable.Empty<ChainModel>())
            .SelectMany(part => part?.PointModels ?? Enumerable.Empty<PointModel>())
            .ToList()
            ?? new List<PointModel>();

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
      await SettingFastMeter(meter, context.Console, command.AlgorithmKey.Contains("Б"));

      if (command.LowerLimitResistance.HasValue)
      {
        firstValue = command.LowerLimitResistance.Value;
      }

      if (command.HigherLimitResistance.HasValue)
      {
        secondValue = command.HigherLimitResistance.Value;
      }

      var cabelResistance = command.CabelResistance != null ? command.CabelResistance.Value : 0;

      var errMes = await PairwiseFirstPointCheckerAlt.CheckSequenceAsync(command.Scheme, context.CommandExecutionManager, command, context.Console, (firstValue + secondValue) / 2, cabelResistance);
      errorMessage.AddRange(errMes);

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
        if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => module.BusManager.ConnectBusAsync(SwitchingBus.A1, userMessageService: userMessageService), userMessageService))
        {
          throw BusExceptionFactory.ConnectFailed(SwitchingBus.A1.ToString(), module.Name, module.NumberChassis, module.Number);
        }
        if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => module.BusManager.ConnectBusAsync(SwitchingBus.B1, userMessageService: userMessageService), userMessageService))
        {
          throw BusExceptionFactory.ConnectFailed(SwitchingBus.B1.ToString(), module.Name, module.NumberChassis, module.Number);
        }
      }
    }

    private async Task SettingsDeviceBusCommutatuion(ISwitchingDevice dbc, IUserInteractionService userMessageService)
    {
      if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => dbc.ConnectorManager.ConnectMultimeter(SwitchingBusNew.AB1, userMessageService), userMessageService))
      {
        throw ConnectorExceptionFactory.ConnectMultiMeterFailed(dbc.Name, dbc.NumberChassis, dbc.Number);
      }
    }

    private async Task SettingFastMeter(IFastMeter meter, IUserInteractionService userMessageService, bool fast = false)
    {
      string name = meter.Name;
      int numberChassis = meter.NumberChassis;
      int number = meter.Number;

      if (!fast)
      {
        if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => await meter.ResistanceManager.SetResistanceModeAsync(userMessageService), userMessageService))
        {
          throw ResistanceExceptionFactory.SetModeFailed(name, numberChassis, number);
        }
      }
      else
      {
        if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => await meter.ContinuityManager.SetContinuityModeAsync(userMessageService), userMessageService))
        {
          throw ContinuityExceptionFactory.SetModeFailed(name, numberChassis, number);
        }
      }
    }
  }
}
