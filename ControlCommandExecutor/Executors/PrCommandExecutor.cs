using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AppConfiguration.Error.Translation;
using ControlCommandAnalyser.Model;
using ControlCommandExecutor.Execution;
using NewCore.Base.Interface.Main;
using Utilities;
using Utilities.Interface;
using Utilities.Models;
using static ControlCommandExecutor.BaseStrategies.NodeAccumulationChecker;
using static NewCore.Enum.DeviceEnum;

namespace ControlCommandExecutor.Executors
{
  internal class PrCommandExecutor : ICommandExecutor
  {
    public string Mnemonic => "ПР";
    static private PointModel _basePoint;


    public async Task ExecuteAsync(CommandExecutionContext context)
    {
      if (!await AppConfiguration.Execution.ExecutionConfig.GetIsIdleModeEnabled())
      {
        await NewCore.Communication.DeviceCommandSender.ResetAllSystem();
      }

      var command = context.Command as PrCommandModel;
      context.TranslationControl.SetActiveLine(command.FormattedStartLineNumber);

      string nameCommand = $"{command.CommandNumber} {command.Mnemonic}";

      var lowerLimit = ExtractFirstNumber(command.LowerLimitResistance);
      var higherLimit = ExtractFirstNumber(command.HigherLimitResistance);
      string message = string.Empty;

      foreach (var str in command.SourceLines)
      {
        message += "\r\n  " + str;
      }

      await context.Console.ShowMessageAsync(new ShowMessageModel($"\r\nВыполнение команды {nameCommand}", headerColor: ShowMessageModel.SuccessMessage.TitleColor, message: message) { IndentLevel = 1 }, IsBlockStart: true);

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

      var meter = EquipmentService.GetFastMeterOrThrow(context.Console);
      await SettingMeter(meter, context.Console);

      await context.Console.ShowMessageAsync(new ShowMessageModel($"Выполнение измерений"), IsBlockStart: true);

      if (command.LowerLimitResistance != null && command.HigherLimitResistance == null)
      {
        double resistance = ExtractNumberFrimString(command.LowerLimitResistance);

        if (command.AlgorithmKey.Contains("К"))
        {
          // await NodeFullChecker.CheckSequenceAsync(context.CommandExecutionManager, command, points, context.Console, resistance.Value);
          BaseStrategies.NodeFullChecker.PerformMeasurementAsync measure = NodeFullPerformMeasurementAsync;
          await BaseStrategies.NodeFullChecker.CheckSequenceAsync(measure, context.CommandExecutionManager, command, points, context.Console, resistance);
        }

        else
        {
          //await CheckSequenceAsync(context.CommandExecutionManager, command, points, context.Console, resistance);
          PerformMeasurementAsync measure = NodeAccumulationPerformMeasurementAsync;
          await BaseStrategies.NodeAccumulationChecker.CheckSequenceAsync(context.CommandExecutionManager, command, measure, points, context.Console, resistance, context.Console.GetCancellationToken());
        }
      }
    }

    /// <summary>
    /// Извлекает первое число из строки.
    /// </summary>
    /// <param name="input">Исходная строка.</param>
    /// <returns>Первое найденное число или null, если число не найдено.</returns>
    private static double? ExtractFirstNumber(string input)
    {
      if (string.IsNullOrEmpty(input))
      {
        return null;
      }

      var match = Regex.Match(input, @"-?\d+([.,]\d+)?");
      if (match.Success && double.TryParse(match.Value.Replace(',', '.'), out double result))
        return result;

      return null;
    }

    private async Task SettingModuleRelayControl(List<IRelaySwitchModule> relaySwitchModules, IUserMessageService userMessageService)
    {
      foreach (var module in relaySwitchModules)
      {
        await module.ConnectableManager.ResetAsync();
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

    private async Task SettingsDeviceBusCommutatuion(ISwitchingDevice dbc, IUserMessageService userMessageService)
    {
      await dbc.ConnectableManager.ResetAsync(userMessageService);

      if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => dbc.ConnectorManager.ConnectMultimeter(SwitchingBusNew.AB1), userMessageService))
      {
        throw AppConfiguration.Error.Device.DeviceBusCommutation.ConnectorExceptionFactory.ConnectMultiMeterFailed(dbc.Name, dbc.NumberChassis, dbc.Number);
      }
    }

    private async Task SettingMeter(IFastMeter meter, IUserMessageService userMessageService)
    {
      string name = meter.Name;
      int numberChassis = meter.NumberChassis;
      int number = meter.Number;

      await userMessageService.ShowMessageAsync(new ShowMessageModel("Настройка мультиметра"));
      if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await meter.ConnectableManager.ConnectAsync()).Connect, userMessageService))
      {
        throw AppConfiguration.Error.Device.Breakdown.IrExceptionFactory.SetVoltageFailed(name, numberChassis, number);
      }
      if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await meter.ContinuityManager.SetContinuityModeAsync()), userMessageService))
      {
        throw AppConfiguration.Error.Device.Breakdown.IrExceptionFactory.SetVoltageFailed(name, numberChassis, number);
      }
    }

    ///// <summary>
    ///// Выполняет последовательную проверку точек с накоплением на одной из них (узел).
    ///// </summary>
    ///// <param name="points">Список точек для проверки.</param>
    ///// <param name="messageService">Сервис отображения сообщений.</param>
    ///// <returns>Задача, представляющая выполнение проверки.</returns>
    //static public async Task CheckSequenceAsync(CommandExecutionManager manager, PrCommandModel siCommandModel, List<PointModel> points, IUserMessageService messageService, double resistance)
    //{
    //  if (points == null || points.Count <= 0)
    //  {
    //    return;
    //  }

    //  _basePoint = points.FirstOrDefault();
    //  await messageService.ShowMessageAsync(new ShowMessageModel($"Подлючение точек"), IsBlockStart: true);
    //  await ConnectToBusBAsync(_basePoint, messageService);
    //  points.Remove(_basePoint);

    //  await messageService.ShowMessageAsync(new ShowMessageModel($"Выполнение измерений"), IsBlockStart: true);

    //  foreach (var point in points)
    //  {
    //    await messageService.ShowMessageAsync(new ShowMessageModel($"Проверка пары {_basePoint.ToString()} и {point.ToString()}") { IndentLevel = 1 }, IsBlockStart: true);
    //    await ConnectToBusAAsync(point, messageService);
    //    if (!await PerformMeasurementAsync(resistance, messageService))
    //    {
    //      await messageService.ShowMessageAsync(new ShowMessageModel("Обнаружено замыкание между", message: $"{_basePoint.ToString()}, {point.ToString()}", type: ShowMessageModel.MessageType.Error)
    //      { IndentLevel = 3 });
    //      manager.AddErrorMethod(SiErrors.PairError($"{siCommandModel.CommandNumber} {siCommandModel.Mnemonic}", _basePoint.ToString(), point.ToString()));
    //    }
    //    await DisconnectFromBusAAsync(point, messageService);
    //  }
    // }

    /// <summary>
    /// Подключает указанную точку к шине B через соответствующий модуль коммутации.
    /// В случае неудачи предлагает пользователю повторить попытку.
    /// </summary>
    /// <param name="point">Точка, которую необходимо подключить к шине B.</param>
    /// <param name="messageService">Сервис для отображения сообщений и взаимодействия с пользователем.</param>
    /// <exception cref="RelayControlException">
    /// Выбрасывается при невозможности подключения точки после всех попыток.
    /// </exception>
    private static async Task ConnectToBusBAsync(PointModel point, IUserMessageService messageService)
    {
      var module = EquipmentService.GetModuleByPoint(point);
      if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => module.PointManager.ConnectRelayAsync(bus: NewCore.Enum.DeviceEnum.BusPoint.B, point.PointNumber), messageService))
      {
        throw AppConfiguration.Error.Device.ModuleRelayControl.RelayExceptionFactory.ConnectPointFailed(point.PointNumber.ToString(), module.Name, module.NumberChassis, module.Number);
      }
    }

    /// <summary>
    /// Подключает указанную точку к шине A через соответствующий модуль коммутации.
    /// В случае неудачи предлагает пользователю повторить попытку.
    /// </summary>
    /// <param name="point">Точка, которую необходимо подключить к шине A.</param>
    /// <param name="messageService">Сервис для отображения сообщений и взаимодействия с пользователем.</param>
    /// <exception cref="RelayControlException">
    /// Выбрасывается при невозможности подключения точки после всех попыток.
    /// </exception>
    private static async Task ConnectToBusAAsync(PointModel point, IUserMessageService messageService)
    {
      var module = EquipmentService.GetModuleByPoint(point);
      if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => module.PointManager.ConnectRelayAsync(bus: NewCore.Enum.DeviceEnum.BusPoint.A, point.PointNumber), messageService))
      {
        throw AppConfiguration.Error.Device.ModuleRelayControl.RelayExceptionFactory.ConnectPointFailed(point.PointNumber.ToString(), module.Name, module.NumberChassis, module.Number);
      }
    }

    /// <summary>
    /// Отключает указанную точку от шины A через соответствующий модуль коммутации.
    /// В случае неудачи предлагает пользователю повторить попытку.
    /// </summary>
    /// <param name="point">Точка, которую необходимо отключить от шины A.</param>
    /// <param name="messageService">Сервис для отображения сообщений и взаимодействия с пользователем.</param>
    /// <exception cref="RelayControlException">
    /// Выбрасывается при невозможности отключить точку после всех попыток.
    /// </exception>
    private static async Task DisconnectFromBusAAsync(PointModel point, IUserMessageService messageService)
    {
      var module = EquipmentService.GetModuleByPoint(point);
      if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => module.PointManager.DisconnectRelayAsync(bus: NewCore.Enum.DeviceEnum.BusPoint.A, point.PointNumber), messageService))
      {
        throw AppConfiguration.Error.Device.ModuleRelayControl.RelayExceptionFactory.DisconnectPointFailed(point.PointNumber.ToString(), module.Name, module.NumberChassis, module.Number);
      }
    }

    /// <summary>
    /// Отключает указанную точку от шины B через соответствующий модуль коммутации.
    /// В случае неудачи предлагает пользователю повторить попытку.
    /// </summary>
    /// <param name="point">Точка, которую необходимо отключить от шины A.</param>
    /// <param name="messageService">Сервис для отображения сообщений и взаимодействия с пользователем.</param>
    /// <exception cref="RelayControlException">
    /// Выбрасывается при невозможности отключить точку после всех попыток.
    /// </exception>
    private static async Task DisconnectFromBusBAsync(PointModel point, IUserMessageService messageService)
    {
      var module = EquipmentService.GetModuleByPoint(point);
      if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => module.PointManager.DisconnectRelayAsync(bus: NewCore.Enum.DeviceEnum.BusPoint.B, point.PointNumber), messageService))
      {
        throw AppConfiguration.Error.Device.ModuleRelayControl.RelayExceptionFactory.DisconnectPointFailed(point.PointNumber.ToString(), module.Name, module.NumberChassis, module.Number);
      }
    }

    /// <summary>
    /// Выполняет измерение между уже подключёнными точками метод накапливающего узла.
    /// Предполагается, что коммутация завершена заранее.
    /// </summary>
    /// <returns>Задача, представляющая измерение.</returns>
    private static async Task<bool> NodeAccumulationPerformMeasurementAsync(double resistance, IUserMessageService messageService, CancellationToken cancellationToken)
    {
      var fastMeter = EquipmentService.GetFastMeterOrThrow(messageService);

      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var answer = await fastMeter.ContinuityManager.CheckContinuityAsync(resistance);
        var result = !await AppConfiguration.Execution.ExecutionConfig.GetIsIdleModeEnabled() ? answer > resistance : !await AppConfiguration.Execution.ExecutionConfig.GetIsErrorSimulationEnabled();

        await messageService.ShowMessageAsync(new ShowMessageModel("Результат измерения сопротивления", message: $"{(answer > 1000 ? ">" : "")}{answer} Ом", type: (result ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error)) { IndentLevel = 2 }, skipPause: true);
        return result;

      }, messageService);

      return result;
    }

    /// <summary>
    /// Выполняет измерение между уже подключёнными точками метод полного узла.
    /// Предполагается, что коммутация завершена заранее.
    /// </summary>
    /// <returns>Задача, представляющая измерение.</returns>
    private static async Task<(bool, double)> NodeFullPerformMeasurementAsync(double resistance, IUserMessageService messageService, CancellationToken cancellationToken)
    {
      var fastMeter = EquipmentService.GetFastMeterOrThrow(messageService);
      double answer = -1;
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        answer = await fastMeter.ContinuityManager.CheckContinuityAsync(resistance);
        var result = !await AppConfiguration.Execution.ExecutionConfig.GetIsIdleModeEnabled() ? answer > resistance : !await AppConfiguration.Execution.ExecutionConfig.GetIsErrorSimulationEnabled();

        await messageService.ShowMessageAsync(new ShowMessageModel("Результат измерения сопротивления", message: $"{(answer > 1000 ? ">" : "")}{answer} Ом", type: (result ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error)) { IndentLevel = 2 }, skipPause: true);
        return result;

      }, messageService);

      return (result, answer);
    }

    private static double ExtractNumberFrimString(string input)
    {
      Match match = Regex.Match(input.Trim(), @"^(\d+(?:\.\d+)?)");
      if (!match.Success || !double.TryParse(match.Groups[1].Value, out double result))
      {
        return -1;
      }

      return result;
    }
  }
}
