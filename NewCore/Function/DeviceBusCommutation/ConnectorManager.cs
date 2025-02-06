using NewCore.Communication;

namespace NewCore.Function.DeviceBusCommutation
{
  public class ConnectorManager
  {
    /// <summary>
    /// Устройство коммутации шин.
    /// </summary>
    private readonly Device.DeviceBusCommutation _deviceBusCommutation;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ConnectorManager"/>.
    /// </summary>
    /// <param name="deviceBusCommutation">Экземпляр устройства коммутации шин.</param>
    public ConnectorManager(Device.DeviceBusCommutation deviceBusCommutation) => _deviceBusCommutation = deviceBusCommutation;

    /// <summary>
    /// Замыкает разъёмы XS4 и XS9.
    /// </summary>
    /// <param name="_deviceBusCommutation.IPAddress">"Ip адрес УКШ.".</param>
    /// <returns>Задача (Task), представляющая асинхронную операцию.</returns>
    public async Task ConnectXs9ToXs4()
    {
      DeviceCommand cmd = new DeviceCommand(7, 1, 0, 0);
      await DeviceCommandSender.SendCommandAsync(_deviceBusCommutation.IPAddress, cmd).ConfigureAwait(true);
    }

    /// <summary>
    /// Размыкает разъёмы XS4 и XS9.
    /// </summary>
    /// <param name="_deviceBusCommutation.IPAddress">"Ip адрес УКШ.".</param>
    /// <returns>Задача (Task), представляющая асинхронную операцию.</returns>
    public async Task DisconnectXs9ToXs4()
    {
      DeviceCommand cmd = new DeviceCommand(7, 2, 0, 0);
      await DeviceCommandSender.SendCommandAsync(_deviceBusCommutation.IPAddress, cmd).ConfigureAwait(true);
    }

    /// <summary>
    /// Подключает пробойную установку к шинам.
    /// </summary>
    /// <remarks>
    /// Отправляет команду с кодом <c>10, 1</c> на IP-адрес УКШ.
    /// </remarks>
    public async Task ConnectToBreakdownTester()
    {
      await DeviceCommandSender.SendCommandAsync(_deviceBusCommutation.IPAddress, new DeviceCommand(10, 1));
    }

    /// <summary>
    /// Отключает пробойную установку от шин.
    /// </summary>
    /// <remarks>
    /// Отправляет команду с кодом <c>10, 0</c> на IP-адрес УКШ.
    /// </remarks>
    public async Task DisconnectToBreakdownTester()
    {
      await DeviceCommandSender.SendCommandAsync(_deviceBusCommutation.IPAddress, new DeviceCommand(10, 0));
    }

  }
}
