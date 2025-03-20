using System.Net;
using System.Windows.Media;
using Mode.Models;
using NewCore.Base.Function.DBC;
using NewCore.Base.Interface.Main;
using Utilities.Models;
using static AppConfig.Config.ExecutionConfig;
using static NewCore.Enum.DeviceEnum;
using static Utilities.DelegateManager;
using static Utilities.Models.ShowMessageModel;

namespace Mode.Metrology.Base
{
  /// <summary>
  /// Класс для организации коммуникации с устройствами метрологии,
  /// включая подключение шин и точек МКР.
  /// </summary>
  internal class MetrologyDeviceCommunication
  {
    static private readonly Tuple<string, Color> goodText = SuccessMessage;
    static private readonly Tuple<string, Color> errorText = ErrorMessage;

    /// <summary>
    /// Производит подключение шин А2В2 на УКШ.
    /// </summary>
    /// <param name="token">Токен отмены операции.</param>
    /// <param name="messageDelegate">Делегат для отображения сообщений.</param>
    /// <param name="model">Модель устройства для подключения шин.</param>
    /// <returns>Задача асинхронного выполнения операции.</returns>
    static internal async Task DeviceBusCommutationConnectBus(CancellationToken token, MessageDelegate messageDelegate, ISwitchingDevice model)
    {
      bool result = !await GetIsIdleModeEnabled() ? await model.ConnectorManager.ConnectMultimeter(SwitchingBusNew.AB2) : true;
      if (result)
      {
        await messageDelegate(new ShowMessageModel($"\tЗамыкание шин {SwitchingBusNew.AB2}:", null, $"[{goodText.Item1}]", goodText.Item2));
      }
      else
      {
        await messageDelegate(new ShowMessageModel($"\tЗамыкание шин {SwitchingBusNew.AB2}:", null, $"[{errorText.Item1}]", errorText.Item2));
      }
    }

    /// <summary>
    /// Подключает шин(у/ы) МКР на шину.
    /// </summary>
    /// <param name="model">Экземпляр устройства МКР.</param>
    /// <param name="messageDelegate">Делегат для отображения сообщений.</param>
    /// <returns>Задача асинхронного выполнения операции.</returns>
    static internal async Task ModuleRelayControl_ConnectBusesAsync(IRelaySwitchModule model, MessageDelegate messageDelegate)
    {
      bool result = await GetIsIdleModeEnabled() || await model.BusManager.ConnectBusAsync(SwitchingBus.AB2, true);
      if (result)
      {
        await messageDelegate(new ShowMessageModel($"\tЗамыкание шин {SwitchingBus.AB2}:", null, $"[{goodText.Item1}]", goodText.Item2));
      }
      else
      {
        await messageDelegate(new ShowMessageModel($"\tЗамыкание шин {SwitchingBus.AB2}:", null, $"[{errorText.Item1}]", errorText.Item2));
      }
    }

    /// <summary>
    /// Подключает точки МКР. 
    /// </summary>
    /// <param name="firstModel">Первая точка измерения.</param>
    /// <param name="secondModel">Вторая точка измерения.</param>
    /// <param name="firstModelRelayControl">Модель устройства для первой точки МКР.</param>
    /// <param name="secondModelRelayControl">Модель устройства для второй точки МКР.</param>
    /// <param name="messageDelegate">Делегат для отображения сообщений.</param>
    /// <returns>Задача асинхронного выполнения операции.</returns>
    static internal async Task ModuleRelayControl_ConnectRelayAsync(PointModel firstModel, PointModel secondModel, IRelaySwitchModule firstModelRelayControl, IRelaySwitchModule secondModelRelayControl, MessageDelegate messageDelegate)
    {
      await messageDelegate(new ShowMessageModel("Подключение точек МКР", goodText.Item2));

      await ConnectRelayPointAsync(firstModelRelayControl, BusPoint.A, firstModel.PointNumber, messageDelegate);
      await ConnectRelayPointAsync(secondModelRelayControl, BusPoint.B, secondModel.PointNumber, messageDelegate);
    }

    /// <summary>
    /// Подключает одну точку МКР и отображает результат.
    /// </summary>
    /// <param name="model">Экземпляр устройства МКР.</param>
    /// <param name="busPoint">Точка шины (A или B).</param>
    /// <param name="pointNumber">Номер точки для подключения.</param>
    /// <returns>Задача асинхронного выполнения операции.</returns>
    static private async Task ConnectRelayPointAsync(IRelaySwitchModule model, BusPoint busPoint, int pointNumber, MessageDelegate messageDelegate)
    {
      bool result = await GetIsIdleModeEnabled() || await model.PointManager.ConnectRelayAsync(busPoint, pointNumber);

      string pointName = $"\tТочка {pointNumber}:";
      string status = result ? goodText.Item1 : errorText.Item1;
      Color color = result ? goodText.Item2 : errorText.Item2;

      await messageDelegate(new ShowMessageModel(pointName, null, $"[{status}]", color));
    }
  }
}
