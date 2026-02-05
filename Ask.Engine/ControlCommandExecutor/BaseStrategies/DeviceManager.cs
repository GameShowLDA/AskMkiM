using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandExecutor.Execution;

namespace Ask.Engine.ControlCommandExecutor.BaseStrategies
{
  public static class DeviceManager
  {
    /// <summary>
    /// Центральный модуль управления коммутацией реле.
    /// Объединяет менеджеры операций на уровне шин, отдельных точек, цепей и групп цепей, обеспечивая согласованное подключение, отключение и переключение между шинами A и B.
    /// Поддерживает асинхронное выполнение, реверсивную логику и обработку ошибок коммутации с участием пользователя.
    /// </summary>
    internal static class RelayModule
    {
      /// <summary>
      /// Менеджер операций коммутации на уровне шин.
      /// Обеспечивает подключение всех линий шин A и B для набора модулей коммутации реле.
      /// Используется для инициализации или восстановления общего состояния шин оборудования.
      /// </summary>
      internal static class BusManager
      {
        internal static async Task ConnectAllBusLinesAsync(IEnumerable<IRelaySwitchModule> relaySwitchModules, IUserInteractionService userMessageService)
        {
          foreach (var module in relaySwitchModules)
          {
            BusConverter.TrySplitAbBus(module.BusType, out SwitchingBus busA, out SwitchingBus busB);
            await module.BusManager.ConnectBusAsync(busA, userMessageService: userMessageService);
            await module.BusManager.ConnectBusAsync(busB, userMessageService: userMessageService);
          }
        }
      }

      /// <summary>
      /// Менеджер операций коммутации на уровне отдельной точки.
      /// Обеспечивает подключение, отключение и переключение точки между шинами A и B через соответствующий модуль реле.
      /// Поддерживает реверсивную логику выполнения операций и взаимодействие с пользователем при ошибках коммутации.
      /// </summary>
      internal static class PointManager
      {
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
        /// Выполняет сброс состояния всех точек коммутации.
        /// Отключает все точки во всех указанных модулях реле, приводя систему в исходное безопасное состояние.
        /// </summary>
        /// <param name="relaySwitchModules">
        /// Набор модулей коммутации реле, для которых требуется выполнить сброс точек.
        /// </param>
        /// <param name="userMessageService">
        /// Сервис отображения сообщений и взаимодействия с пользователем.
        /// </param>
        public static async Task ResetAllPointsAsync(IEnumerable<IRelaySwitchModule> relaySwitchModules, IUserInteractionService userMessageService)
        {
          await userMessageService.ShowMessageAsync(new ShowMessageModel("Сброс точек") { IndentLevel = 1 });

          foreach (var module in relaySwitchModules)
          {
            await module.PointManager.DisconnectingAllPoint(userMessageService);
          }
        }
      }

      /// <summary>
      /// Обеспечивает подключение, отключение и переключение всех точек цепи между шинами A и B через соответствующие модули реле. 
      /// Поддерживает реверсивную логику выполнения операций и взаимодействие с пользователем при ошибках коммутации.
      /// </summary>
      internal static class ChainManager
      {
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
      }

      /// <summary>
      /// Обеспечивает подключение и отключение всех точек, входящих в группу цепей, к шинам A и B через модули реле.
      /// Поддерживает реверсивную логику переключения шин и взаимодействие с пользователем при ошибках коммутации.
      /// </summary>
      internal static class GroupManager
      {
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
                await PointManager.ConnectPointToBusBAsync(item, messageService, revers);
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
                await PointManager.ConnectPointToBusAAsync(item, messageService, revers);
              }
            }
          }
          else
          {
            await ConnectAllFromBusBAsync(groupChains, messageService, false);
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
                await RelayModule.PointManager.DisconnectPointFromBusAAsync(item, messageService, revers);
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
                await RelayModule.PointManager.DisconnectPointFromBusBAsync(item, messageService, revers);
              }
            }
          }
          else
          {
            await DisconnectAlPointlFromBusAAsync(groupChains, messageService, false);
          }
        }
      }
    }

    /// <summary>
    /// Менеджер коммутации модулей и устройств.
    /// Определяет сценарии подключения измерительных и испытательных устройств к шинам A и B.
    /// </summary>
    internal static class SwitchModuleManager
    {
      /// <summary>
      /// Менеджер коммутации устройств на шины.
      /// Обеспечивает подключение и отключение измерительных и испытательных устройств к шинам A и B.
      /// </summary>
      internal static class DeviceConnectionManager
      {
        /// <summary>
        /// Выполняет коммутацию мультиметра на шины A и B. Подключает устройство к заданной конфигурации шин.
        /// </summary>
        /// <param name="dbc">Коммутируемое устройство.</param>
        /// <param name="userMessageService">
        /// Сервис отображения сообщений и взаимодействия с пользователем.
        /// </param>
        internal static async Task ConnectMultimeter(ISwitchingDevice dbc, IUserInteractionService userMessageService)
        {
          await dbc.ConnectorManager.ConnectMultimeter(SwitchingBusNew.AB1, userMessageService);
        }

        /// <summary>
        /// Выполняет коммутацию установки пробоя на шины.
        /// Подключает испытательное устройство к шинам A1B1.
        /// </summary>
        /// <param name="dbc">Коммутируемое устройство.</param>
        /// <param name="userMessageService">
        /// Сервис отображения сообщений и взаимодействия с пользователем.
        /// </param>
        internal static async Task ConnectBreakdownTester(ISwitchingDevice dbc, IUserInteractionService userMessageService)
        {
          await dbc.ConnectorManager.ConnectBreakdownTester(userMessageService);
        }
      }
    }
  }
}
