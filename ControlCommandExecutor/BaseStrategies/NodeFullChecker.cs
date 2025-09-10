using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppConfiguration.Error.Translation;
using ControlCommandAnalyser.Model;
using ControlCommandAnalyser.Model.Chains;
using ControlCommandAnalyser.Model.Ok;
using ControlCommandExecutor.Execution;
using Utilities;
using Utilities.Interface;
using Utilities.Models;

namespace ControlCommandExecutor.BaseStrategies
{
  internal static class NodeFullChecker
  {
    internal delegate Task<(bool Result, double Value)> PerformMeasurementAsync(double value, IUserMessageService userMessageService, CancellationToken cancellationToken, VoltageEnum.Type typeVoltage = VoltageEnum.Type.ACW);

    static private List<ChainModel> ErrorsPoints = new List<ChainModel>();

    /// <summary>
    /// Выполняет последовательную проверку точек с накоплением на одной из них (узел).
    /// </summary>
    /// <param name="points">Список точек для проверки.</param>
    /// <param name="messageService">Сервис отображения сообщений.</param>
    /// <returns>Задача, представляющая выполнение проверки.</returns>
    static public async Task CheckSequenceAsync(SchemeModel schemeModel, PerformMeasurementAsync performMeasurementAsync, CommandExecutionManager manager, BaseCommandModel baseCommandModel, IUserMessageService messageService, double resistance)
    {
      var pointsList = schemeModel.GetPointsDisconnected();
      if (pointsList.Count == 0)
      {
        return;
      }
      ErrorsPoints = new List<ChainModel>();

      await messageService.ShowMessageAsync(new ShowMessageModel($"Проверка разобщённых точек"));

      foreach (var point in pointsList)
      {
        var chainModels = new ChainModel(point);
        messageService.GetCancellationToken().ThrowIfCancellationRequested();
        await ConnectToBusBAsync(chainModels, messageService);
      }

      foreach (var point in pointsList)
      {
        messageService.GetCancellationToken().ThrowIfCancellationRequested();
        var chainModels = new ChainModel(point);


        await messageService.ShowMessageAsync(new ShowMessageModel($"Проверка {chainModels.ToString()}"), IsBlockStart: true);
        await DisconnectFromBusBAsync(chainModels, messageService);
        await ConnectToBusAAsync(chainModels, messageService);

        var answer = await performMeasurementAsync(resistance, messageService, messageService.GetCancellationToken());

        if (!answer.Result)
        {
          manager.AddErrorMethod(baseCommandModel.PointErrors.NodeExecutePointError($"{baseCommandModel.CommandNumber} {baseCommandModel.Mnemonic}", chainModels.PointModels, ($"{answer.Value} МОм (>{resistance} МОм)")));
          ErrorsPoints.Add(chainModels);
        }

        await DisconnectFromBusAAsync(chainModels, messageService);
        await ConnectToBusBAsync(chainModels, messageService);
      }

      if (ErrorsPoints.Count > 0)
      {
        await messageService.ShowMessageAsync(new ShowMessageModel($"Бракованные точки"), IsBlockStart: true);
        foreach (var point in ErrorsPoints)
        {
          await messageService.ShowMessageAsync(new ShowMessageModel($"Найден брак при проверке цепи", message: point.ToString(), type: ShowMessageModel.MessageType.Error) { IndentLevel = 1 }, IsBlockStart: true);
        }

        await messageService.ShowMessageAsync(new ShowMessageModel("Анализ на наличие короткого замыкания между точками"), IsBlockStart: true);

        var chains = await FindAllShortCircuitChainsAsync(performMeasurementAsync, ErrorsPoints, resistance, messageService);


        await messageService.ShowMessageAsync(
           new ShowMessageModel($"Результаты проверки")
           { IndentLevel = 1 });

        foreach (var chain in chains)
        {
          var chainStr = string.Empty;
          for (int itemIndex = 0; itemIndex < chain.Count; itemIndex++)
          { 
            var item = chain[itemIndex];
            for (int i = 0; i < item.PointModels.Count; i++)
            {
              var point = item.PointModels[i].Mnemonic;

              if (itemIndex > 0 && i == 0)
              { 
                chainStr += $" ## ";
              }

              if (item.PointModels.Count == 1)
              {
                chainStr += $"*{point}*";
                continue;
              }

              if (i == 0)
              {
                chainStr += $"*{point}";
              }
              else if (i + 1 == item.PointModels.Count)
              {
                chainStr += $"#{point}*";
              }
              else
              { 
                chainStr += $"#{point}";
              }
            }
          }


          await messageService.ShowMessageAsync(
             new ShowMessageModel($"{chainStr}",
                 message: "Обнаружено замыкание",
                 type: ShowMessageModel.MessageType.Error)
             { IndentLevel = 3 });

          manager.AddErrorMethod(baseCommandModel.PointErrors.ChainError($"{baseCommandModel.CommandNumber} {baseCommandModel.Mnemonic}", chainStr));
        }
      }
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
    private static async Task DisconnectAllFromBusBAsync(List<ChainModel> points, IUserMessageService messageService)
    {
      foreach (var point in points)
      {
        await DisconnectFromBusBAsync(point, messageService);
      }
    }


    private static async Task ConnectAllFromBusBAsync(List<ChainModel> points, IUserMessageService messageService)
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
    public static async Task<List<List<ChainModel>>> FindAllShortCircuitChainsAsync(
      PerformMeasurementAsync performMeasurementAsync,
        List<ChainModel> faultyPoints,
        double resistance,
        IUserMessageService messageService)
    {
      var chains = new List<List<ChainModel>>();
      var visited = new HashSet<ChainModel>();

      foreach (var point in faultyPoints)
      {
        if (visited.Contains(point))
          continue;

        // Найти всю компоненту связности, к которой относится эта точка
        var chain = await FindChainAsync(performMeasurementAsync, point, faultyPoints, resistance, messageService, visited);

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
    private static async Task<List<ChainModel>> FindChainAsync(
      PerformMeasurementAsync performMeasurementAsync,
        ChainModel start,
        List<ChainModel> allPoints,
        double resistance,
        IUserMessageService messageService,
        HashSet<ChainModel> visited)
    {
      var queue = new Queue<ChainModel>();
      var chain = new List<ChainModel>();

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
          bool isConnected = await IsShortCircuitedAsync(performMeasurementAsync, current, candidate, resistance, messageService);
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
    private static async Task<bool> IsShortCircuitedAsync(PerformMeasurementAsync performMeasurementAsync, ChainModel a, ChainModel b, double resistance, IUserMessageService messageService)
    {
      var allPoints = ErrorsPoints;
      await DisconnectAllFromBusAAsync(allPoints, messageService);
      await DisconnectAllFromBusBAsync(allPoints, messageService);

      await ConnectToBusAAsync(a, messageService);
      await ConnectToBusBAsync(b, messageService);

      bool result = !(await performMeasurementAsync(resistance, messageService, messageService.GetCancellationToken())).Result;

      await DisconnectFromBusAAsync(a, messageService);
      await DisconnectFromBusBAsync(b, messageService);

      return result;
    }

    private static async Task DisconnectAllFromBusAAsync(List<ChainModel> points, IUserMessageService messageService)
    {
      foreach (var point in points)
      {
        await DisconnectFromBusAAsync(point, messageService);
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
    private static async Task ConnectToBusAAsync(ChainModel chain, IUserMessageService messageService)
    {
      foreach (var point in chain.PointModels)
      {
        var module = EquipmentService.GetModuleByPoint(point);
        if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => module.PointManager.ConnectRelayAsync(bus: NewCore.Enum.DeviceEnum.BusPoint.A, point.PointNumber), messageService))
        {
          throw AppConfiguration.Error.Device.ModuleRelayControl.RelayExceptionFactory.ConnectPointFailed(point.PointNumber.ToString(), module.Name, module.NumberChassis, module.Number);
        }
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
    private static async Task ConnectToBusBAsync(ChainModel chain, IUserMessageService messageService)
    {
      foreach (var point in chain.PointModels)
      {
        var module = EquipmentService.GetModuleByPoint(point);
        if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => module.PointManager.ConnectRelayAsync(bus: NewCore.Enum.DeviceEnum.BusPoint.B, point.PointNumber), messageService))
        {
          throw AppConfiguration.Error.Device.ModuleRelayControl.RelayExceptionFactory.ConnectPointFailed(point.PointNumber.ToString(), module.Name, module.NumberChassis, module.Number);
        }
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
    private static async Task DisconnectFromBusAAsync(ChainModel chain, IUserMessageService messageService)
    {
      foreach (var point in chain.PointModels)
      {
        var module = EquipmentService.GetModuleByPoint(point);
        if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => module.PointManager.DisconnectRelayAsync(bus: NewCore.Enum.DeviceEnum.BusPoint.A, point.PointNumber), messageService))
        {
          throw AppConfiguration.Error.Device.ModuleRelayControl.RelayExceptionFactory.DisconnectPointFailed(point.PointNumber.ToString(), module.Name, module.NumberChassis, module.Number);
        }
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
    private static async Task DisconnectFromBusBAsync(ChainModel chain, IUserMessageService messageService)
    {
      foreach (var point in chain.PointModels)
      {
        var module = EquipmentService.GetModuleByPoint(point);
        if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => module.PointManager.DisconnectRelayAsync(bus: NewCore.Enum.DeviceEnum.BusPoint.B, point.PointNumber), messageService))
        {
          throw AppConfiguration.Error.Device.ModuleRelayControl.RelayExceptionFactory.DisconnectPointFailed(point.PointNumber.ToString(), module.Name, module.NumberChassis, module.Number);
        }
      }
    }
  }
}
