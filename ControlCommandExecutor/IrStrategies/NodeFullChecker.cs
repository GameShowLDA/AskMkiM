using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlCommandExecutor.Execution;
using Utilities;
using Utilities.Interface;
using Utilities.Models;

namespace ControlCommandExecutor.IrStrategies
{
  /// <summary>
  /// Класс для метода полного узла режима СИ
  /// </summary>
  internal static class NodeFullChecker
  {

    static private List<PointModel> ErrorsPoints = new List<PointModel>();

    /// <summary>
    /// Выполняет последовательную проверку точек с накоплением на одной из них (узел).
    /// </summary>
    /// <param name="points">Список точек для проверки.</param>
    /// <param name="messageService">Сервис отображения сообщений.</param>
    /// <returns>Задача, представляющая выполнение проверки.</returns>
    static public async Task CheckSequenceAsync(List<PointModel> points, IUserMessageService messageService, double resistance)
    {
      ErrorsPoints = new List<PointModel>();

      await messageService.ShowMessageAsync(new ShowMessageModel($"Подлючение точек"), IsBlockStart: true);

      foreach (var point in points)
      {
        messageService.GetCancellationToken().ThrowIfCancellationRequested();
        await ConnectToBusBAsync(point, messageService);
      }

      foreach (var point in points)
      {
        messageService.GetCancellationToken().ThrowIfCancellationRequested();
        await messageService.ShowMessageAsync(new ShowMessageModel($"Проверка точки {(EquipmentService.GetPointKey(point))}"), IsBlockStart: true);

        await DisconnectFromBusBAsync(point, messageService);
        await ConnectToBusAAsync(point, messageService);

        if (!await PerformMeasurementAsync(resistance, messageService))
        {
          ErrorsPoints.Add(point);
        }

        await DisconnectFromBusAAsync(point, messageService);
        await ConnectToBusBAsync(point, messageService);
      }

      if (ErrorsPoints.Count > 0)
      {
        await messageService.ShowMessageAsync(new ShowMessageModel($"Бракованные точки"), IsBlockStart: true);
        foreach (var point in ErrorsPoints)
        {
          await messageService.ShowMessageAsync(new ShowMessageModel($"Найден брак в точке", message: point.ToString(), type: ShowMessageModel.MessageType.Error) { IndentLevel = 1 }, IsBlockStart: true);
        }

        await messageService.ShowMessageAsync(new ShowMessageModel("Анализ на наличие короткого замыкания между точками"), IsBlockStart: true);

        //var errors = new List<ShowMessageModel>();

        var chains = await FindAllShortCircuitChainsAsync(ErrorsPoints, resistance, messageService);

        foreach (var chain in chains)
        {
          var chainStr = string.Join(", ", chain.Select(p => EquipmentService.GetPointKey(p)));
          await messageService.ShowMessageAsync(
              new ShowMessageModel("Цепь короткого замыкания найдена",
                  message: $"Обнаружена замкнутая цепь: {chainStr}",
                  type: ShowMessageModel.MessageType.Error)
              { IndentLevel = 3 });
        }
      }

      //foreach (var point in ErrorsPoints.ToList())
      //{
      //  await DisconnectFromBusBAsync(point, messageService);
      //  await ConnectToBusAAsync(point, messageService);

      //  ErrorsPoints.Remove(point);

      //  if (ErrorsPoints.Count <=1)
      //    break;

      //  await messageService.ShowMessageAsync(new ShowMessageModel($"Локализация точки {point}") { IndentLevel = 1 }, IsBlockStart: true);

      //  var localized = await LocalizeFaultyPointAsync(ErrorsPoints, resistance, messageService);

      //  if (localized != null)
      //  {
      //    errors.Add(new ShowMessageModel("Локализация завершена", message: $"Обнаружено замыкание точки {EquipmentService.GetPointKey(point)} c точкой {EquipmentService.GetPointKey(localized)}", type: ShowMessageModel.MessageType.Error) { IndentLevel = 3 });
      //  }
      //  else
      //  {
      //    errors.Add(new ShowMessageModel("Локализация не удалась", message: "Не удалось точно определить неисправную точку", type: ShowMessageModel.MessageType.Error) { IndentLevel = 3 });
      //  }

      //  await DisconnectFromBusAAsync(point, messageService);
      //  await ConnectToBusBAsync(point, messageService);
      //}

      //await messageService.ShowMessageAsync(new ShowMessageModel("Результаты анализа на наличие короткого замыкания между точками"), IsBlockStart: true);

      //foreach (var message in errors)
      //{
      //  await messageService.ShowMessageAsync(message);
      //}
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

          errorPoint = await LocalizeFaultyPointAsync(leftPart, resistance, messageService);
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

    /// <summary>
    /// Ищет все цепи КЗ среди списка бракованных точек с минимальным числом измерений.
    /// </summary>
    /// <param name="faultyPoints">Список бракованных точек.</param>
    /// <param name="resistance">Порог сопротивления для определения КЗ.</param>
    /// <param name="messageService">Сервис сообщений.</param>
    /// <returns>Список цепей (каждая цепь — список связанных точек).</returns>
    public static async Task<List<List<PointModel>>> FindAllShortCircuitChainsAsync(
        List<PointModel> faultyPoints,
        double resistance,
        IUserMessageService messageService)
    {
      var chains = new List<List<PointModel>>();
      var visited = new HashSet<PointModel>();

      foreach (var point in faultyPoints)
      {
        if (visited.Contains(point))
          continue;

        // Найти всю компоненту связности, к которой относится эта точка
        var chain = await FindChainAsync(point, faultyPoints, resistance, messageService, visited);

        if (chain.Count > 1)
          chains.Add(chain);
      }

      return chains;
    }

    /// <summary>
    /// Для заданной стартовой точки ищет всю цепь КЗ (все связанные с ней точки) методом BFS.
    /// </summary>
    /// <param name="start">Стартовая точка.</param>
    /// <param name="allPoints">Список всех бракованных точек.</param>
    /// <param name="resistance">Порог сопротивления для определения КЗ.</param>
    /// <param name="messageService">Сервис сообщений.</param>
    /// <param name="visited">Множество уже посещённых точек (будет обновлено).</param>
    /// <returns>Список всех точек, связанных с данной через КЗ.</returns>
    private static async Task<List<PointModel>> FindChainAsync(
        PointModel start,
        List<PointModel> allPoints,
        double resistance,
        IUserMessageService messageService,
        HashSet<PointModel> visited)
    {
      var queue = new Queue<PointModel>();
      var chain = new List<PointModel>();

      queue.Enqueue(start);
      visited.Add(start);
      chain.Add(start);

      while (queue.Count > 0)
      {
        var current = queue.Dequeue();

        foreach (var candidate in allPoints)
        {
          if (visited.Contains(candidate) || candidate.Equals(current))
            continue;

          // Проверяем только потенциально ещё не найденные связи!
          bool isConnected = await IsShortCircuitedAsync(current, candidate, resistance, messageService);
          if (isConnected)
          {
            queue.Enqueue(candidate);
            visited.Add(candidate);
            chain.Add(candidate);
          }
        }
      }
      return chain;
    }

    /// <summary>
    /// Проверяет, замкнуты ли две точки между собой.
    /// </summary>
    private static async Task<bool> IsShortCircuitedAsync(PointModel a, PointModel b, double resistance, IUserMessageService messageService)
    {
      var allPoints = ErrorsPoints;
      await DisconnectAllFromBusAAsync(allPoints, messageService);
      await DisconnectAllFromBusBAsync(allPoints, messageService);

      await ConnectToBusAAsync(a, messageService);
      await ConnectToBusBAsync(b, messageService);

      bool result = !await PerformMeasurementAsync(resistance, messageService);

      await DisconnectFromBusAAsync(a, messageService);
      await DisconnectFromBusBAsync(b, messageService);

      return result;
    }

    private static async Task DisconnectAllFromBusAAsync(List<PointModel> points, IUserMessageService messageService)
    {
      foreach (var point in points)
      {
        await DisconnectFromBusAAsync(point, messageService);
      }
    }
  }
}
