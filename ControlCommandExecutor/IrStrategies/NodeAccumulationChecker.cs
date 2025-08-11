using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ControlCommandExecutor.Execution;
using Utilities;
using Utilities.Interface;
using Utilities.Models;

namespace ControlCommandExecutor.IrStrategies
{
  /// <summary>
  /// Класс для метода накапливающего узла режима СИ
  /// </summary>
  static internal class NodeAccumulationChecker
  {
    static private int step = 0;

    /// <summary>
    /// Выполняет последовательную проверку точек с накоплением на одной из них (узел).
    /// </summary>
    /// <param name="points">Список точек для проверки.</param>
    /// <param name="messageService">Сервис отображения сообщений.</param>
    /// <returns>Задача, представляющая выполнение проверки.</returns>
    static public async Task CheckSequenceAsync(List<PointModel> points, IUserMessageService messageService, double resistance)
    {
      foreach (var point in points)
      {
        messageService.GetCancellationToken().ThrowIfCancellationRequested();
        await messageService.ShowMessageAsync(new ShowMessageModel($"Проверка точки {(EquipmentService.GetPointKey(point))}"), IsBlockStart: true);
        await ConnectToBusAAsync(point, messageService);
        if (!await PerformMeasurementAsync(resistance, messageService))
        {
          step = 0;
          var localized = await LocalizeFaultyPointAsync(EquipmentService.GetPointsBefore(point), resistance, messageService);

          if (localized != null)
          {
            await messageService.ShowMessageAsync(new ShowMessageModel("Локализация завершена", message: $"Обнаружено замыкание точки {EquipmentService.GetPointKey(point)} c точкой {EquipmentService.GetPointKey(localized)}", type: ShowMessageModel.MessageType.Error) { IndentLevel = 3 });
          }
          else
          {
            await messageService.ShowMessageAsync(new ShowMessageModel("Локализация не удалась", message: "Не удалось точно определить неисправную точку", type: ShowMessageModel.MessageType.Error) { IndentLevel = 3 });
          }
        }
        await DisconnectFromBusAAsync(point, messageService);
        await ConnectToBusBAsync(point, messageService);
      }

      foreach (var point in points)
      {
        await DisconnectFromBusBAsync(point, messageService);
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
    /// Отключает указанную точку от шины A через соответствующий модуль коммутации.
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
    /// Выполняет измерение между уже подключёнными точками.
    /// Предполагается, что коммутация завершена заранее.
    /// </summary>
    /// <returns>Задача, представляющая измерение.</returns>
    private static async Task<bool> PerformMeasurementAsync(double resistance, IUserMessageService messageService)
    {
      var breadDown = EquipmentService.GetBreakdownTesterOrThrow(messageService);

      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var answer = await breadDown.IrManger.MeasureResistanceAsync(resistance);
        var result = !await AppConfiguration.Execution.ExecutionConfig.GetIsIdleModeEnabled() ? answer >= resistance : !await AppConfiguration.Execution.ExecutionConfig.GetIsErrorSimulationEnabled();

        await messageService.ShowMessageAsync(new ShowMessageModel("Результат измерения сопротивления изоляции", message: $"{answer} МОм", type: (result ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error)) { IndentLevel = 1 }, skipPause: true);
        return result;
      }, messageService);

      return result;
    }

    /// <summary>
    /// Локализует неисправную точку методом половинного деления.
    /// Одна точка остаётся на шине A (известная как бракованная), остальные проверяются на шине B.
    /// </summary>
    /// <param name="knownFaultPoint">Известная точка, оставляемая на шине A.</param>
    /// <param name="candidates">Кандидаты на локализацию на шине B.</param>
    /// <param name="resistance">Пороговое сопротивление для проверки.</param>
    /// <param name="messageService">Сервис сообщений.</param>
    /// <returns>Локализованная точка или null, если локализация не удалась.</returns>
    public static async Task<PointModel?> LocalizeFaultyPointAsync(
        List<PointModel> candidates,
        double resistance,
        IUserMessageService messageService)
    {
      PointModel errorPoint = null;
      step++;

      await messageService.ShowMessageAsync(new ShowMessageModel($"Выполенение шага {step}"));
      var (leftPart, rightPart) = SplitInHalf(candidates);

      await messageService.ShowMessageAsync(new ShowMessageModel("Отключение левой части группы точек"));
      await DisconnectAllFromBusBAsync(leftPart, messageService);

      if (!await PerformMeasurementAsync(resistance, messageService))
      {
        if (rightPart.Count > 1)
        {
          errorPoint = await LocalizeFaultyPointAsync(rightPart, resistance, messageService);
        }
        else
        {
          errorPoint = PointModel.ParsePointString($"{rightPart[0].DeviceNumber}.{rightPart[0].ModuleNumber}.{rightPart[0].PointNumber}");
          return errorPoint;
        }
      }
      else
      {
        await messageService.ShowMessageAsync(new ShowMessageModel("Отключение правой части группы точек"));
        await DisconnectAllFromBusBAsync(rightPart, messageService);

        await messageService.ShowMessageAsync(new ShowMessageModel("Подключение левой части группы точек"));
        await ConnectAllFromBusBAsync(leftPart, messageService);

        if (leftPart.Count > 1)
        {

          errorPoint =  await LocalizeFaultyPointAsync(leftPart, resistance, messageService);
        }
        else
        {
          if (!await PerformMeasurementAsync(resistance, messageService))
          {
            errorPoint = PointModel.ParsePointString($"{leftPart[0].DeviceNumber}.{leftPart[0].ModuleNumber}.{leftPart[0].PointNumber}");
            return errorPoint;
          }
          else
          {
            return errorPoint;
          }
        }
      }

      await ConnectAllFromBusBAsync(candidates, messageService);
      return errorPoint;
    }

    /// <summary>
    /// Делит список точек пополам.
    /// Если количество нечётное — первая часть будет на один элемент больше.
    /// </summary>
    /// <param name="points">Список точек.</param>
    /// <returns>Кортеж из двух списков: левая и правая половины.</returns>
    public static (List<PointModel> Left, List<PointModel> Right) SplitInHalf(List<PointModel> points)
    {
      int middle = (points.Count + 1) / 2; // первая половина длиннее, если нечётно
      var left = points.Take(middle).ToList();
      var right = points.Skip(middle).ToList();
      return (left, right);
    }

    private static async Task DisconnectAllFromBusBAsync(List<PointModel> points, IUserMessageService messageService)
    {
      foreach (var point in points)
      {
        await DisconnectFromBusBAsync(point, messageService);
      }
    }

    private static async Task ConnectAllFromBusBAsync(List<PointModel> points, IUserMessageService messageService)
    {
      foreach (var point in points)
      {
        await ConnectToBusBAsync(point, messageService);
      }
    }
  }
}
