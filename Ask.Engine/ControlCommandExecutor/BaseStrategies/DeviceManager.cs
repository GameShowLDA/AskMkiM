using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandExecutor.Execution;

namespace Ask.Engine.ControlCommandExecutor.BaseStrategies
{
  public static class DeviceManager
  {

    #region Подключение.

    #region Точка.

    /// <summary>
    /// Подключает указанную точку к шине A через соответствующий модуль коммутации.
    /// В случае неудачи предлагает пользователю повторить попытку.
    /// </summary>
    /// <param name="point">Точка, которую необходимо подключить к шине A.</param>
    /// <param name="messageService">Сервис для отображения сообщений и взаимодействия с пользователем.</param>
    /// <exception cref="RelayControlException">
    /// Выбрасывается при невозможности подключения точки после всех попыток.
    /// </exception>
    public static async Task ConnectPointToBusAAsync(PointModel point, IUserInteractionService messageService, bool revers)
    {
      if (!revers)
      {
        var module = EquipmentService.GetModuleByPoint(point);
        await module.PointManager.ConnectRelayAsync(bus: BusPoint.A, point.PointNumber, messageService);
      }
      else
      {
        await ConnectPointToBusBAsync(point, messageService, false);
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
    public static async Task ConnectPointToBusBAsync(PointModel point, IUserInteractionService messageService, bool revers)
    {
      if (!revers)
      {
        var module = EquipmentService.GetModuleByPoint(point);
        await module.PointManager.ConnectRelayAsync(bus: BusPoint.B, point.PointNumber, messageService);
      }
      else
      {
        await ConnectPointToBusAAsync(point, messageService, false);
      }
    }

    #endregion

    #region Цепь.

    /// <summary>
    /// Подключает указанную точку к шине A через соответствующий модуль коммутации.
    /// В случае неудачи предлагает пользователю повторить попытку.
    /// </summary>
    /// <param name="point">Точка, которую необходимо подключить к шине A.</param>
    /// <param name="messageService">Сервис для отображения сообщений и взаимодействия с пользователем.</param>
    /// <exception cref="RelayControlException">
    /// Выбрасывается при невозможности подключения точки после всех попыток.
    /// </exception>
    public static async Task ConnectChainToBusAAsync(ChainModel points, IUserInteractionService messageService, bool revers)
    {
      if (!revers)
      {
        foreach (var point in points.PointModels)
        {
          var module = EquipmentService.GetModuleByPoint(point);
          await module.PointManager.ConnectRelayAsync(bus: BusPoint.A, point.PointNumber, messageService);
        }
      }
      else
      {
        await ConnectChainToBusBAsync(points, messageService, false);
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
    public static async Task ConnectChainToBusBAsync(ChainModel points, IUserInteractionService messageService, bool revers)
    {
      if (!revers)
      {
        foreach (var point in points.PointModels)
        {
          var module = EquipmentService.GetModuleByPoint(point);
          await module.PointManager.ConnectRelayAsync(bus: BusPoint.B, point.PointNumber, messageService);
        }
      }
      else
      {
        await ConnectChainToBusAAsync(points, messageService, false);
      }
    }

    #endregion

    /// <summary>
    /// Подключает указанные точки к шине A через соответствующий модуль коммутации.
    /// В случае неудачи предлагает пользователю повторить попытку.
    /// </summary>
    /// <param name="groupChains">Группа цепей, которую необходимо подключить к шине B.</param>
    /// <param name="messageService">Сервис для отображения сообщений и взаимодействия с пользователем.</param>
    /// <exception cref="RelayControlException">
    /// Выбрасывается при невозможности подключения точки после всех попыток.
    public static async Task ConnectAllFromBusBAsync(GroupModel groupChains, IUserInteractionService messageService, bool revers)
    {
      if (!revers)
      {
        foreach (var chain in groupChains.ChainModels)
        {
          foreach (var item in chain.PointModels)
          {
            await DeviceManager.ConnectPointToBusBAsync(item, messageService, revers);
          }
        }
      }
      else
      {
        await ConnectAllFromBusAAsync(groupChains, messageService, false);
      }
    }


    /// <summary>
    /// Подключает указанные точки к шине A через соответствующий модуль коммутации.
    /// В случае неудачи предлагает пользователю повторить попытку.
    /// </summary>
    /// <param name="groupChains">Группа цепей, которую необходимо подключить к шине B.</param>
    /// <param name="messageService">Сервис для отображения сообщений и взаимодействия с пользователем.</param>
    /// <exception cref="RelayControlException">
    /// Выбрасывается при невозможности подключения точки после всех попыток.
    public static async Task ConnectAllFromBusAAsync(GroupModel groupChains, IUserInteractionService messageService, bool revers)
    {
      if (!revers)
      {
        foreach (var chain in groupChains.ChainModels)
        {
          foreach (var item in chain.PointModels)
          {
            await DeviceManager.ConnectPointToBusAAsync(item, messageService, revers);
          }
        }
      }
      else
      {
        await ConnectAllFromBusBAsync(groupChains, messageService, false);
      }
    }

    #endregion

    #region Отключение.

    #region Точка.

    /// <summary>
    /// Отключает указанную точку от шины A через соответствующий модуль коммутации.
    /// В случае неудачи предлагает пользователю повторить попытку.
    /// </summary>
    /// <param name="point">Точка, которую необходимо отключить от шины A.</param>
    /// <param name="messageService">Сервис для отображения сообщений и взаимодействия с пользователем.</param>
    /// <exception cref="RelayControlException">
    /// Выбрасывается при невозможности отключить точку после всех попыток.
    /// </exception>
    public static async Task DisconnectPointFromBusAAsync(PointModel point, IUserInteractionService messageService, bool revers)
    {
      if (!revers)
      {
        var module = EquipmentService.GetModuleByPoint(point);
        await module.PointManager.DisconnectRelayAsync(bus: BusPoint.A, point.PointNumber, messageService);
      }
      else
      {
        await DisconnectPointFromBusBAsync(point, messageService, false);
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
    public static async Task DisconnectPointFromBusBAsync(PointModel point, IUserInteractionService messageService, bool revers)
    {
      if (!revers)
      {
        var module = EquipmentService.GetModuleByPoint(point);
        await module.PointManager.DisconnectRelayAsync(bus: BusPoint.B, point.PointNumber, messageService);
      }
      else
      {
        await DisconnectPointFromBusAAsync(point, messageService, false);
      }
    }

    /// <summary>
    /// Отключает указанные точки от шины A через соответствующий модуль коммутации.
    /// В случае неудачи предлагает пользователю повторить попытку.
    /// </summary>
    /// <param name="groupChains">Группа цепей, которую необходимо отключить от шины A.</param>
    /// <param name="messageService">Сервис для отображения сообщений и взаимодействия с пользователем.</param>
    /// <exception cref="RelayControlException">
    /// Выбрасывается при невозможности отключить точку после всех попыток.
    /// </exception>
    public static async Task DisconnectAlPointlFromBusAAsync(GroupModel groupChains, IUserInteractionService messageService, bool revers)
    {
      if (!revers)
      {
        foreach (var chain in groupChains.ChainModels)
        {
          foreach (var item in chain.PointModels)
          {
            await DisconnectPointFromBusAAsync(item, messageService, revers);
          }
        }
      }
      else
      {
        await DisconnectAllPointFromBusBAsync(groupChains, messageService, false);
      }
    }

    /// <summary>
    /// Отключает указанные точки от шины B через соответствующий модуль коммутации.
    /// В случае неудачи предлагает пользователю повторить попытку.
    /// </summary>
    /// <param name="groupChains">Группа цепей, которую необходимо отключить от шины A.</param>
    /// <param name="messageService">Сервис для отображения сообщений и взаимодействия с пользователем.</param>
    /// <exception cref="RelayControlException">
    /// Выбрасывается при невозможности отключить точку после всех попыток.
    /// </exception>
    public static async Task DisconnectAllPointFromBusBAsync(GroupModel groupChains, IUserInteractionService messageService, bool revers)
    {
      if (!revers)
      {
        foreach (var chain in groupChains.ChainModels)
        {
          foreach (var item in chain.PointModels)
          {
            await DisconnectPointFromBusBAsync(item, messageService, revers);
          }
        }
      }
      else
      {
        await DisconnectAlPointlFromBusAAsync(groupChains, messageService, false);
      }
    }

    #endregion

    #region Цепь.

    /// <summary>
    /// Отключает указанную точку от шины B через соответствующий модуль коммутации.
    /// В случае неудачи предлагает пользователю повторить попытку.
    /// </summary>
    /// <param name="point">Точка, которую необходимо отключить от шины A.</param>
    /// <param name="messageService">Сервис для отображения сообщений и взаимодействия с пользователем.</param>
    /// <exception cref="RelayControlException">
    /// Выбрасывается при невозможности отключить точку после всех попыток.
    /// </exception>
    public static async Task DisconnectChainFromBusBAsync(ChainModel points, IUserInteractionService messageService, bool revers)
    {
      if (!revers)
      {
        foreach (var point in points.PointModels)
        {
          var module = EquipmentService.GetModuleByPoint(point);
          await module.PointManager.DisconnectRelayAsync(bus: BusPoint.B, point.PointNumber, messageService);
        }
      }
      else
      {
        await DisconnectChainFromBusAAsync(points, messageService, false);
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
    public static async Task DisconnectChainFromBusAAsync(ChainModel points, IUserInteractionService messageService, bool revers)
    {
      if (!revers)
      {
        foreach (var point in points.PointModels)
        {
          var module = EquipmentService.GetModuleByPoint(point);
          await module.PointManager.DisconnectRelayAsync(bus: BusPoint.A, point.PointNumber, messageService);
        }
      }
      else
      {
        await DisconnectChainFromBusBAsync(points, messageService, false);
      }
    }

    #endregion

    #endregion

    #region Переподключение точек.

    /// <summary>
    /// Отключает указанную точку от шины B через соответствующий модуль коммутации.
    /// В случае неудачи предлагает пользователю повторить попытку.
    /// </summary>
    /// <param name="point">Точка, которую необходимо отключить от шины A.</param>
    /// <param name="messageService">Сервис для отображения сообщений и взаимодействия с пользователем.</param>
    /// <exception cref="RelayControlException">
    /// Выбрасывается при невозможности отключить точку после всех попыток.
    /// </exception>
    public static async Task SwitchPointFromBusAToBAsync(PointModel point, IUserInteractionService messageService, bool revers)
    {
      if (!revers)
      {
        var module = EquipmentService.GetModuleByPoint(point);
        await module.PointManager.ConnectingPointToNewBus(bus: BusPoint.B, point.PointNumber, messageService);
      }
      else
      {
        await SwitchPointFromBusBToAAsync(point, messageService, false);
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
    public static async Task SwitchPointFromBusBToAAsync(PointModel point, IUserInteractionService messageService, bool revers)
    {
      if (!revers)
      {
        var module = EquipmentService.GetModuleByPoint(point);
        await module.PointManager.ConnectingPointToNewBus(bus: BusPoint.A, point.PointNumber, messageService);
      }
      else
      {
        await SwitchPointFromBusAToBAsync(point, messageService, false);
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
    public static async Task SwitchChainFromBusAToBAsync(ChainModel chain, IUserInteractionService messageService, bool revers)
    {
      if (!revers)
      {
        foreach (var point in chain.PointModels)
        {
          var module = EquipmentService.GetModuleByPoint(point);
          await module.PointManager.ConnectingPointToNewBus(bus: BusPoint.B, point.PointNumber, messageService);
        }
      }
      else
      {
        await SwitchChainFromBusBToAAsync(chain, messageService, false);
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
    public static async Task SwitchChainFromBusBToAAsync(ChainModel chain, IUserInteractionService messageService, bool revers)
    {
      if (!revers)
      {
        foreach (var point in chain.PointModels)
        {
          var module = EquipmentService.GetModuleByPoint(point);
          await module.PointManager.ConnectingPointToNewBus(bus: BusPoint.A, point.PointNumber, messageService);
        }
      }
      else
      {
        await SwitchChainFromBusAToBAsync(chain, messageService, false);
      }
    }

    #endregion

  }
}
