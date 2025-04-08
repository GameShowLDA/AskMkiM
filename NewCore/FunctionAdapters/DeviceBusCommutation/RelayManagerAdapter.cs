using System;
using System.Threading.Tasks;
using NewCore.Base.Function.DBC;
using NewCore.Function.DeviceBusCommutation;
using NewCore.Function.Helpers;

namespace NewCore.FunctionAdapters.DeviceBusCommutation
{
  /// <summary>
  /// Адаптер управления подключением/отключением реле.
  /// </summary>
  internal class RelayManagerAdapter : IRelayDeviceBusCommutation
  {
    private readonly Device.DeviceBusCommutation _deviceBusCommutation;
    private readonly RelayManager _relayManager;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="RelayManagerAdapter"/>.
    /// </summary>
    /// <param name="deviceBusCommutation">Экземпляр устройства коммутации шин.</param>
    public RelayManagerAdapter(Device.DeviceBusCommutation deviceBusCommutation)
    {
      _deviceBusCommutation = deviceBusCommutation ?? throw new ArgumentNullException(nameof(deviceBusCommutation));
      _relayManager = new RelayManager(deviceBusCommutation);
    }

    /// <inheritdoc />
    public async Task<bool> ConnectRelay(int numberRelay)
    {
      var result = await _relayManager.ConnectRelay(numberRelay);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(
        _deviceBusCommutation,
        "Подключение реле",
        $"№{numberRelay}",
        result,
        1);
      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectRelay(int numberRelay)
    {
      var result = await _relayManager.DisconnectRelay(numberRelay);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(
        _deviceBusCommutation,
        "Отключение реле",
        $"№{numberRelay}",
        result,
        1);
      return result;
    }
  }
}
