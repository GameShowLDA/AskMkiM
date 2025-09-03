using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ControlCommandAnalyser.Model;
using ControlCommandAnalyser.Model.Chains;
using ControlCommandAnalyser.Model.Ok;
using ControlCommandExecutor.Execution;
using Utilities;
using Utilities.Interface;
using Utilities.Models;

namespace ControlCommandExecutor.BaseStrategies
{
  static internal class NodeAccumulationChecker
  {
    /// <summary>
    /// Делегат для выполнения измерений.
    /// </summary>
    /// <param name="value">Ожидаемое значение.</param>
    /// <param name="userMessageService">Элемент управления для вывода сообщений.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    internal delegate Task<bool> PerformMeasurementAsync(double value, IUserMessageService userMessageService, CancellationToken cancellationToken);
    static private int step = 0;

    /// <summary>
    /// Выполняет последовательную проверку точек с накоплением на одной из них (узел).
    /// </summary>
    /// <param name="points">Список точек для проверки.</param>
    /// <param name="messageService">Сервис отображения сообщений.</param>
    /// <returns>Задача, представляющая выполнение проверки.</returns>
    static public async Task CheckSequenceAsync(SchemeModel schemeModel, CommandExecutionManager manager, BaseCommandModel baseCommandModel, PerformMeasurementAsync performMeasurementAsync, IUserMessageService messageService, double resistance, CancellationToken cancellationToken)
    {
      List<PointModel> points = schemeModel.GetAllPoints();
      foreach (var point in points)
      {
        messageService.GetCancellationToken().ThrowIfCancellationRequested();

        List<PointModel> pairsPoint = null;

        if (schemeModel.TryCommunicatedPointAllChain(point, out List<PointModel> result))
        {
          pairsPoint = result;
          if (result[0] != point)
          {
            continue;
          }

          string pointStr = string.Empty;
          foreach (var item in pairsPoint)
          {
            pointStr += $"{(EquipmentService.GetPointKey(item))} ";
          }

          await messageService.ShowMessageAsync(new ShowMessageModel($"Проверка точек {pointStr}"), IsBlockStart: true);
          foreach (var pointPair in result)
          {
            if (pointPair.PointNumber < point.PointNumber)
            {
              await DisconnectFromBusBAsync(pointPair, messageService);
            }
          }

          foreach (var pointPair in result)
          {
            await ConnectToBusAAsync(pointPair, messageService);
          }
        }
        else
        {
          await messageService.ShowMessageAsync(new ShowMessageModel($"Проверка точки {(EquipmentService.GetPointKey(point))}"), IsBlockStart: true);
          await ConnectToBusAAsync(point, messageService);
        }


        if (!await performMeasurementAsync(resistance, messageService, cancellationToken))
        {
          step = 0;
          var localized = await LocalizeFaultyPointAsync(performMeasurementAsync, EquipmentService.GetPointsBefore(points, point), resistance, messageService, cancellationToken);

          if (localized != null)
          {

            if (baseCommandModel.PointErrors != null)
            {
              manager.AddErrorMethod(baseCommandModel.PointErrors.PairError($"{baseCommandModel.CommandNumber} {baseCommandModel.Mnemonic}", EquipmentService.GetPointKey(point), EquipmentService.GetPointKey(localized)));
            }

            await messageService.ShowMessageAsync(new ShowMessageModel("Локализация завершена", message: $"Обнаружено замыкание точки {EquipmentService.GetPointKey(point)} c точкой {EquipmentService.GetPointKey(localized)}", type: ShowMessageModel.MessageType.Error) { IndentLevel = 3 });
          }
          else
          {
            await messageService.ShowMessageAsync(new ShowMessageModel("Локализация не удалась", message: "Не удалось точно определить неисправную точку", type: ShowMessageModel.MessageType.Error) { IndentLevel = 3 });
          }
        }

        if (pairsPoint != null)
        {
          foreach (var item in pairsPoint)
          {
            await DisconnectFromBusAAsync(item, messageService);
            await ConnectToBusBAsync(item, messageService);
          }
        }
        else
        {
          await DisconnectFromBusAAsync(point, messageService);
          await ConnectToBusBAsync(point, messageService);
        }
      }

      foreach (var point in points)
      {
        await DisconnectFromBusBAsync(point, messageService);
      }
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
      PerformMeasurementAsync performMeasurementAsync,
        List<PointModel> candidates,
        double resistance,
        IUserMessageService messageService,
        CancellationToken cancellationToken
        )
    {
      PointModel errorPoint = null;
      step++;

      await messageService.ShowMessageAsync(new ShowMessageModel($"Выполенение шага {step}"));
      var (leftPart, rightPart) = SplitInHalf(candidates);

      await messageService.ShowMessageAsync(new ShowMessageModel("Отключение левой части группы точек"));
      await DisconnectAllFromBusBAsync(leftPart, messageService);

      if (!await performMeasurementAsync(resistance, messageService, cancellationToken))
      {
        if (rightPart.Count > 1)
        {
          errorPoint = await LocalizeFaultyPointAsync(performMeasurementAsync, rightPart, resistance, messageService, cancellationToken);
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
          errorPoint = await LocalizeFaultyPointAsync(performMeasurementAsync, leftPart, resistance, messageService, cancellationToken);
        }
        else
        {
          if (!await performMeasurementAsync(resistance, messageService, cancellationToken))
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
