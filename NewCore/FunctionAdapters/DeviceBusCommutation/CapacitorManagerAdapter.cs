using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppConfiguration.Error.Device.DeviceBusCommutation;
using AppConfiguration.Services;
using NewCore.Base.Device;
using NewCore.Base.Function.DBC;
using NewCore.Function.DeviceBusCommutation;
using NewCore.Function.Helpers;
using Utilities;

namespace NewCore.FunctionAdapters.DeviceBusCommutation
{
  internal class CapacitorManagerAdapter : ICapacitorDeviceBusCommutation
  {
    /// <summary>
    /// Устройство коммутации шин.
    /// </summary>
    private readonly Device.DeviceBusCommutation _deviceBusCommutation;

    private readonly CapacitorManager _capacitorManager;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="BusManager"/>.
    /// </summary>
    /// <param name="deviceBusCommutation">Экземпляр устройства коммутации шин.</param>
    public CapacitorManagerAdapter(Device.DeviceBusCommutation deviceBusCommutation)
    {
      _deviceBusCommutation = deviceBusCommutation ?? throw new ArgumentNullException(nameof(deviceBusCommutation));
      _capacitorManager = new CapacitorManager(deviceBusCommutation);
    }

    /// <inheritdoc />
    public async Task<bool> ConnectCapacitor(string number)
    {
      var result = await _capacitorManager.ConnectCapacitor(number);

      await DeviceMessageBuilder.ShowConnectionMessageAsync(
          _deviceBusCommutation,
          "Подключение конденсатора",
          number,
          result,
          1);

      if (!result)
        throw CapacitorExceptionFactory.ConnectFailed(number);

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectCapacitor(string number)
    {
      var result = await _capacitorManager.DisconnectCapacitor(number);

      await DeviceMessageBuilder.ShowConnectionMessageAsync(
          _deviceBusCommutation,
          "Отключение конденсатора",
          number,
          result,
          1);

      if (!result)
        throw CapacitorExceptionFactory.DisconnectFailed(number);

      return result;
    }
  }
}
