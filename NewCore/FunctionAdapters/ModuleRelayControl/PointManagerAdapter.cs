using System;
using System.Threading.Tasks;
using NewCore.Base.Function.ModuleRelayControl;
using NewCore.Base.Interface.Main;
using NewCore.Function.Helpers;
using NewCore.Function.ModuleRelayControl;
using static NewCore.Enum.DeviceEnum;

namespace NewCore.FunctionAdapters.ModuleRelayControl
{
  /// <summary>
  /// Адаптер для управления точками (реле) модуля МКР с отображением сообщений.
  /// </summary>
  internal class PointManagerAdapter : IPointManager
  {
    private readonly IRelaySwitchModule _moduleRelayControl;
    private readonly PointManager _pointManager;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="PointManagerAdapter"/>.
    /// </summary>
    /// <param name="moduleRelayControl">Экземпляр модуля реле.</param>
    public PointManagerAdapter(IRelaySwitchModule moduleRelayControl)
    {
      _moduleRelayControl = moduleRelayControl ?? throw new ArgumentNullException(nameof(moduleRelayControl));
      _pointManager = new PointManager(moduleRelayControl);
    }

    /// <inheritdoc />
    public async Task<bool> ConnectRelayAsync(BusPoint bus, int number)
    {
      var result = await _pointManager.ConnectRelayAsync(bus, number);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(
        _moduleRelayControl,
        $"Подключение точки {number} к шине [{bus}]",
        result,
        1);
      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectRelayAsync(BusPoint bus, int number)
    {
      var result = await _pointManager.DisconnectRelayAsync(bus, number);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(
        _moduleRelayControl,
        $"Отключение точки {number} от шины [{bus}]",
        result, 
        1);
      return result;
    }

    /// <inheritdoc />
    public async Task<bool> ConnectRelayGroupAsync(BusPoint bus, int firstPoint, int lastPoint)
    {
      var result = await _pointManager.ConnectRelayGroupAsync(bus, firstPoint, lastPoint);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(
        _moduleRelayControl,
        $"Подключение диапазона точек {firstPoint}-{lastPoint} к шине [{bus}]",
        result,
        1);
      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectRelayGroupAsync(BusPoint bus, int firstPoint, int lastPoint)
    {
      var result = await _pointManager.DisconnectRelayGroupAsync(bus, firstPoint, lastPoint);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(
        _moduleRelayControl,
        $"Отключение диапазона точек {firstPoint}-{lastPoint} от шины [{bus}]",
        result,
        1);
      return result;
    }

    /// <inheritdoc />
    public async Task<string> CheckPoint(int numberPoint)
    {
      // TODO : Обработка команды
      return await _pointManager.CheckPoint(numberPoint);
    }
  }
}
