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
using static ControlCommandExecutor.BaseStrategies.NodeAccumulationChecker;

namespace ControlCommandExecutor.BaseStrategies
{
  internal static class ConnectedPointChecker
  {
    internal delegate Task<bool> PerformMeasurementAsync(double value, IUserMessageService userMessageService, CancellationToken cancellationToken);

    /// <summary>
    /// Выполняет последовательную проверку точек с накоплением на одной из них (узел).
    /// </summary>
    /// <param name="points">Список точек для проверки.</param>
    /// <param name="messageService">Сервис отображения сообщений.</param>
    /// <returns>Задача, представляющая выполнение проверки.</returns>
    static public async Task CheckSequenceAsync(SchemeModel schemeModel, PerformMeasurementAsync performMeasurementAsync, CommandExecutionManager manager, BaseCommandModel baseCommandModel, IUserMessageService messageService, double resistance)
    {
      List<List<PointModel>> errorChain = new();
      var pointsList = schemeModel.GetPointsConnected();
      if (pointsList.Count == 0)
      {
        return;
      }

      await messageService.ShowMessageAsync(new ShowMessageModel($"Проверка сообщенных точек"));

      for (int i = 0; i < pointsList.Count; i++)
      {
        var chains = pointsList[i];

        for (int j = 0; j < chains.Count; j++)
        {
          var points = chains[j];
          var _basePoint = points[0];
          points.Remove(_basePoint);
          await messageService.ShowMessageAsync(new ShowMessageModel($"Подлючение точек"), IsBlockStart: true);
          await ConnectToBusBAsync(_basePoint, messageService);
          await messageService.ShowMessageAsync(new ShowMessageModel($"Выполнение измерений"), IsBlockStart: true);

          foreach (var point in points)
          {
            messageService.GetCancellationToken().ThrowIfCancellationRequested();
            await messageService.ShowMessageAsync(new ShowMessageModel($"Проверка {point.Mnemonic}"), IsBlockStart: true);
            await ConnectToBusAAsync(point, messageService);

            if (!await performMeasurementAsync(resistance, messageService, messageService.GetCancellationToken()))
            {
              errorChain.Add(new List<PointModel>() { _basePoint, point });
            }

            await DisconnectFromBusAAsync(point, messageService);
          }
          await DisconnectFromBusBAsync(_basePoint, messageService);
        }
      }

      if (errorChain.Count > 0)
      {
        await messageService.ShowMessageAsync(
          new ShowMessageModel($"Результаты проверки")
          { IndentLevel = 1 });

        for (int itemIndex = 0; itemIndex < errorChain.Count; itemIndex++)
        {
          var chainStr = string.Empty;
          var chain = errorChain[itemIndex];

          for (int i = 0; i < chain.Count; i++)
          {
            var point = chain[i].Mnemonic;
            chainStr += $"*{point}*";
          }


          await messageService.ShowMessageAsync(
            new ShowMessageModel($"{chainStr}",
                message: "Обнаружен разрыв цепи",
                type: ShowMessageModel.MessageType.Error)
            { IndentLevel = 3 });

          manager.AddErrorMethod(baseCommandModel.PointErrors.DisconnectChainError($"{baseCommandModel.CommandNumber} {baseCommandModel.Mnemonic}", chainStr));
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
  }
}
