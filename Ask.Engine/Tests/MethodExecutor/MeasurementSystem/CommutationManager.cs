using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace Ask.Engine.Tests.MethodExecutor.MeasurementSystem
{
  /// <summary>
  /// Отвечает за коммутацию оборудования перед началом измерения.
  /// </summary>
  public class CommutationManager
  {
    private readonly List<object> _devices;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="CommutationManager"/>.
    /// </summary>
    /// <param name="devices">Список доступных устройств, участвующих в коммутации.</param>
    public CommutationManager(List<object> devices)
    {
      _devices = devices ?? throw new ArgumentNullException(nameof(devices));
    }

    /// <summary>
    /// Выполняет подключение ППУ к коммутационному устройству и подключает A1/B1 ко всем модулям.
    /// </summary>
    /// <param name="protocolUI">Интерфейс для отображения сообщений в протоколе.</param>
    /// <param name="bus">Активная шина подключения.</param>
    public async Task SetupAsync(IUserInteractionService protocolUI, BusPoint bus)
    {
      var busSwitcher = _devices.OfType<ISwitchingDevice>().FirstOrDefault();
      var breakdown = _devices.OfType<IBreakdownTester>().FirstOrDefault();

      if (busSwitcher == null || breakdown == null)
      {
        throw new InvalidOperationException("Коммутационное устройство или ППУ не найдены в списке устройств.");
      }

      await busSwitcher.ConnectorManager.ConnectBreakdownTester(protocolUI);

      var relayModules = _devices.OfType<IRelaySwitchModule>().ToList();

      foreach (var module in relayModules)
      {
        await module.BusManager.ConnectBusAsync(SwitchingBus.A1, userMessageService: protocolUI);
        await module.BusManager.ConnectBusAsync(SwitchingBus.B1, userMessageService: protocolUI);
      }
    }
  }
}
