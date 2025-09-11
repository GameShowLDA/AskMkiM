using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using ControlCommandAnalyser.Model;
using ControlCommandAnalyser.Model.Chains;
using ControlCommandExecutor.Execution;
using Utilities;
using Utilities.Interface;
using Utilities.Models;

namespace ControlCommandExecutor.BaseStrategies
{
  internal static class PairwiseFirstPointChecker
  {
    static private ChainModel _basePoint;

    /// <summary>
    /// Выполняет последовательную проверку точек с накоплением на одной из них (узел).
    /// </summary>
    /// <param name="points">Список точек для проверки.</param>
    /// <param name="messageService">Сервис отображения сообщений.</param>
    /// <returns>Задача, представляющая выполнение проверки.</returns>
    static public async Task CheckSequenceAsync(SchemeModel schemeModel, NodeAccumulationChecker.PerformMeasurementAsync performMeasurementAsync, CommandExecutionManager manager, BaseCommandModel baseCommandModel, IUserMessageService messageService, double resistance = 0)
    {
      List<List<ChainModel>> errorChain = new();
      var pointsList = schemeModel.GetPointsDisconnected();
      if (pointsList.Count == 0)
      {
        return;
      }

      _basePoint = new ChainModel(pointsList.FirstOrDefault());
      await messageService.ShowMessageAsync(new ShowMessageModel($"Проверка разобщённых точек"));
      await messageService.ShowMessageAsync(new ShowMessageModel($"Подлючение точек"), IsBlockStart: true);

      await ConnectToBusBAsync(_basePoint, messageService);
      pointsList.Remove(_basePoint.PointModels);
      await messageService.ShowMessageAsync(new ShowMessageModel($"Выполнение измерений"), IsBlockStart: true);

      foreach (var points in pointsList)
      {
        messageService.GetCancellationToken().ThrowIfCancellationRequested();
        var chain = new ChainModel(points);

        string pointStr = string.Empty;
        var str = string.Empty;
        foreach (var point in points)
        {
          str += $"{(EquipmentService.GetPointKey(point))},";
        }
        str = str.Remove(str.Length - 1);
        await messageService.ShowMessageAsync(new ShowMessageModel($"Проверка {str}"), IsBlockStart: true);
        await ConnectToBusAAsync(chain, messageService);

        if (!await performMeasurementAsync(resistance, messageService, messageService.GetCancellationToken()))
        {
          errorChain.Add(new List<ChainModel>() { _basePoint, chain });
        }

        await DisconnectFromBusAAsync(chain, messageService);
      }

      if (errorChain.Count > 0)
      {
        await messageService.ShowMessageAsync(
          new ShowMessageModel($"Результаты проверки")
          { IndentLevel = 1 });

        foreach (var chain in errorChain)
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
    /// Подключает указанную точку к шине B через соответствующий модуль коммутации.
    /// В случае неудачи предлагает пользователю повторить попытку.
    /// </summary>
    /// <param name="point">Точка, которую необходимо подключить к шине B.</param>
    /// <param name="messageService">Сервис для отображения сообщений и взаимодействия с пользователем.</param>
    /// <exception cref="RelayControlException">
    /// Выбрасывается при невозможности подключения точки после всех попыток.
    /// </exception>
    private static async Task ConnectToBusBAsync(ChainModel points, IUserMessageService messageService)
    {
      foreach (var point in points.PointModels)
      {
        var module = EquipmentService.GetModuleByPoint(point);
        if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => module.PointManager.ConnectRelayAsync(bus: NewCore.Enum.DeviceEnum.BusPoint.B, point.PointNumber), messageService))
        {
          throw AppConfiguration.Error.Device.ModuleRelayControl.RelayExceptionFactory.ConnectPointFailed(point.PointNumber.ToString(), module.Name, module.NumberChassis, module.Number);
        }
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
    private static async Task ConnectToBusAAsync(ChainModel points, IUserMessageService messageService)
    {
      foreach (var point in points.PointModels)
      {
        var module = EquipmentService.GetModuleByPoint(point);
        if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => module.PointManager.ConnectRelayAsync(bus: NewCore.Enum.DeviceEnum.BusPoint.A, point.PointNumber), messageService))
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
    private static async Task DisconnectFromBusAAsync(ChainModel points, IUserMessageService messageService)
    {
      foreach (var point in points.PointModels)
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
    private static async Task DisconnectFromBusBAsync(ChainModel points, IUserMessageService messageService)
    {
      foreach (var point in points.PointModels)
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
