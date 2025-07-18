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
  /// Класс для метода накапливающего узла режима СИ
  /// </summary>
  static internal class NodeAccumulationChecker
  {
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
        await PerformMeasurementAsync(resistance, messageService);
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
    private static async Task PerformMeasurementAsync(double resistance, IUserMessageService messageService)
    {
      var breadDown = EquipmentService.GetBreakdownTesterOrThrow(messageService);

      await UserActionHelper.RunWithUserRepeatAsync(async () =>
      {
        var answer = await breadDown.IrManger.MeasureResistanceAsync(resistance);
        var result = answer >= resistance;

        await messageService.ShowMessageAsync(new ShowMessageModel("Результат измерения сопротивления изоляции", message: $"{answer} МОм", type: (result ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error)) { IndentLevel = 1 }, skipPause: true);
        return result;
      }, messageService);
    }
  }
}
