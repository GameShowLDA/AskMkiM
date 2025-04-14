using System;
using System.Threading.Tasks;
using NewCore.Base.Function.DBC;
using NewCore.Function.DeviceBusCommutation;
using NewCore.Function.Helpers;
using Utilities.Error.Device.DeviceBusCommutation;

namespace NewCore.FunctionAdapters.DeviceBusCommutation
{
  /// <summary>
  /// Адаптер управления подключением/отключением резисторов.
  /// </summary>
  internal class ResistorManagerAdapter : IResistorDeviceBusCommutation
  {
    private readonly Device.DeviceBusCommutation _deviceBusCommutation;
    private readonly ResistorManager _resistorManager;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ResistorManagerAdapter"/>.
    /// </summary>
    /// <param name="deviceBusCommutation">Экземпляр устройства коммутации шин.</param>
    public ResistorManagerAdapter(Device.DeviceBusCommutation deviceBusCommutation)
    {
      _deviceBusCommutation = deviceBusCommutation ?? throw new ArgumentNullException(nameof(deviceBusCommutation));
      _resistorManager = new ResistorManager(deviceBusCommutation);
    }

    /// <inheritdoc />
    public async Task<bool> ConnectResistor(string number)
    {
      var result = await _resistorManager.ConnectResistor(number);

      await DeviceMessageBuilder.ShowConnectionMessageAsync(
          _deviceBusCommutation,
          "Подключение резистора",
          $"№{number}",
          result,
          1);

      if (!result)
        throw ResistorExceptionFactory.ConnectFailed(number);

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectResistor(string number)
    {
      var result = await _resistorManager.DisconnectResistor(number);

      await DeviceMessageBuilder.ShowConnectionMessageAsync(
          _deviceBusCommutation,
          "Отключение резистора",
          $"№{number}",
          result,
          1);

      if (!result)
        throw ResistorExceptionFactory.DisconnectFailed(number);

      return result;
    }
  }
}
