using System;
using System.Collections.Generic;
using System.IO.Packaging;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using AppConfiguration.Enums;
using AppConfiguration.MeasurementError;
using ControlCommandAnalyser.Model;
using ControlCommandAnalyser.Model.Ok;
using ControlCommandExecutor.Execution;
using ControlCommandExecutor.IrStrategies;
using NewCore.Base.Interface.Main;
using Utilities;
using Utilities.Interface;
using Utilities.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ControlCommandExecutor.Executors
{
  internal class SiCommandExecutor : ICommandExecutor
  {
    public string Mnemonic => "СИ";

    public async Task ExecuteAsync(CommandExecutionContext context)
    {
      if (!await AppConfiguration.Execution.ExecutionConfig.GetIsIdleModeEnabled())
      {
        await NewCore.Communication.DeviceCommandSender.ResetAllSystem();
      }

      var command = context.Command as SiCommandModel;
      context.TranslationControl.SetActiveLine(command.FormattedStartLineNumber);

      string nameCommand = $"{command.CommandNumber} {command.Mnemonic}";

      var time = ExtractFirstNumber(command.Time);
      var resistance = ExtractFirstNumber(command.Resistance);
      var voltage = ExtractFirstNumber(command.Voltage);
      string message = string.Empty;

      foreach (var str in command.SourceLines)
      { 
        message += "\r\n  " + str;
      }

      await context.Console.ShowMessageAsync(new ShowMessageModel($"\r\nВыполнение команды {nameCommand}", headerColor: ShowMessageModel.SuccessMessage.TitleColor, message: message) { IndentLevel = 1}, IsBlockStart: true);

      var points = PointModel.ConvertToPointModels(command.Points);
      await EquipmentService.ValidatePointsExistInAnalyzedPointsAsync(points, context.Console);


      await context.Console.ShowMessageAsync(new ShowMessageModel($"Подготовка устройств"));

      var modules = points
          .Select(EquipmentService.GetModuleByPoint)
          .Where(m => m != null)
          .DistinctBy(m => (m.NumberChassis, m.Number))
          .ToList();
      await SettingModuleRelayControl(modules, context.Console);

      var dbc = EquipmentService.GetSwitchingDevice();
      await SettingsDeviceBusCommutatuion(dbc, context.Console);

      var breakDown = EquipmentService.GetBreakdownTesterOrThrow(context.Console);
      await SettingBreakdown(breakDown, context.Console, time.Value, resistance.Value, voltage.Value);

      if (command.AlgorithmKey.Contains("К"))
      {
        await NodeFullChecker.CheckSequenceAsync(points, context.Console, resistance.Value);
      }
      else
      {
        await NodeAccumulationChecker.CheckSequenceAsync(points, context.Console, resistance.Value);
      }

      if (!await AppConfiguration.Execution.ExecutionConfig.GetIsIdleModeEnabled())
      {
        await NewCore.Communication.DeviceCommandSender.ResetAllSystem();
      }
    }

    private async Task SettingsDeviceBusCommutatuion(ISwitchingDevice dbc, IUserMessageService userMessageService)
    {
      if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => dbc.ConnectorManager.ConnectBreakdownTester(), userMessageService))
      {
        throw AppConfiguration.Error.Device.DeviceBusCommutation.ConnectorExceptionFactory.ConnectBreakdownFailed(dbc.Name, dbc.NumberChassis, dbc.Number);
      }
    }
    private async Task SettingModuleRelayControl(List<IRelaySwitchModule> relaySwitchModules, IUserMessageService userMessageService)
    {
      foreach (var module in relaySwitchModules)
      {
        if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => module.BusManager.ConnectBusAsync(NewCore.Enum.DeviceEnum.SwitchingBus.A1), userMessageService))
        {
          throw AppConfiguration.Error.Device.ModuleRelayControl.BusExceptionFactory.ConnectFailed(NewCore.Enum.DeviceEnum.SwitchingBus.A1.ToString(), module.Name, module.NumberChassis, module.Number);
        }
        if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => module.BusManager.ConnectBusAsync(NewCore.Enum.DeviceEnum.SwitchingBus.B1), userMessageService))
        {
          throw AppConfiguration.Error.Device.ModuleRelayControl.BusExceptionFactory.ConnectFailed(NewCore.Enum.DeviceEnum.SwitchingBus.B1.ToString(), module.Name, module.NumberChassis, module.Number);
        }
      }
    }

    private async Task SettingBreakdown(IBreakdownTester breakDown, IUserMessageService userMessageService, double time, double resistance, double voltage)
    {
      string name = breakDown.Name;
      int numberChassis = breakDown.NumberChassis;
      int number = breakDown.Number;

      await userMessageService.ShowMessageAsync(new ShowMessageModel("Настройка пробойной установки"));
      if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.ConnectableManager.ConnectAsync()).Connect, userMessageService))
      {
        throw AppConfiguration.Error.Device.ConnectionExceptionFactory.ConnectFailed(name, numberChassis, number);
      }

      if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.IrManger.SetModeAsync()).Success, userMessageService))
      {
        throw AppConfiguration.Error.Device.Breakdown.IrExceptionFactory.SetModeFailed(name, numberChassis, number);
      }

      if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.IrManger.SetTestTimeAsync(time)).Success, userMessageService))
      {
        throw AppConfiguration.Error.Device.Breakdown.IrExceptionFactory.SetTestTimeFailed(name, numberChassis, number);
      }

      if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.IrManger.SetLowResistanceLimitAsync(resistance)).Success, userMessageService))
      {
        throw AppConfiguration.Error.Device.Breakdown.IrExceptionFactory.SetLowLimitFailed(name, numberChassis, number);
      }

      if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.IrManger.SetVoltageAsync(voltage)).Success, userMessageService))
      {
        throw AppConfiguration.Error.Device.Breakdown.IrExceptionFactory.SetVoltageFailed(name, numberChassis, number);
      }
    }

    /// <summary>
    /// Извлекает первое число из строки.
    /// </summary>
    /// <param name="input">Исходная строка.</param>
    /// <returns>Первое найденное число или null, если число не найдено.</returns>
    private static double? ExtractFirstNumber(string input)
    {
      var match = Regex.Match(input, @"-?\d+([.,]\d+)?");
      if (match.Success && double.TryParse(match.Value.Replace(',', '.'), out double result))
        return result;

      return null;
    }
  }
}
