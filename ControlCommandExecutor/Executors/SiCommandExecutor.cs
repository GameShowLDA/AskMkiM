using System;
using System.Collections.Generic;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using AppConfiguration.Enums;
using AppConfiguration.MeasurementError;
using ControlCommandAnalyser.Model;
using ControlCommandAnalyser.Model.Ok;
using ControlCommandExecutor.Execution;
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
      string nameCommand = $"{command.CommandNumber} {command.Mnemonic}";

      var time = ExtractFirstNumber(command.Time);
      var resistance = ExtractFirstNumber(command.Resistance);
      var voltage = ExtractFirstNumber(command.Voltage);

      await context.Console.ShowMessageAsync(new ShowMessageModel($"Выполнение команды {nameCommand}"));
      
      var points = PointModel.ConvertToPointModels(command.Points);
      await EquipmentService.ValidatePointsExistInAnalyzedPointsAsync(points, context.Console);

      var breakDown = EquipmentService.GetBreakdownTesterOrThrow(context.Console);


      await SettingBreakdown(breakDown, context.Console, time.Value, resistance.Value, voltage.Value);

      await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        await context.Console.ShowMessageAsync(new ShowMessageModel(header: "Выполнение измерения сопротивления изоляции"));
        var result = await breakDown.IrManger.MeasureResistanceAsync(resistance.Value + 10);

        bool error = result <= voltage.Value;
        await context.Console.ShowMessageAsync(new ShowMessageModel("Результат измерения сопротивления изоляции", message: $"{result} Ом", type: (error ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error)) { IndentLevel = 3 }, skipPause: true);
        return error;
      }, context.Console);
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
