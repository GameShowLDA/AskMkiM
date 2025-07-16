using NewCore.Base.Interface.Main;
using UI.Controls.ProtocolNew;
using Utilities;
using static NewCore.Enum.DeviceEnum;

namespace Mode.TestSuite.Metrology.MethodExecutor
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
    public async Task SetupAsync(ProtocolUI protocolUI, BusPoint bus)
    {
      var busSwitcher = _devices.OfType<ISwitchingDevice>().FirstOrDefault();
      var breakdown = _devices.OfType<IBreakdownTester>().FirstOrDefault();

      if (busSwitcher == null || breakdown == null)
      {
        throw new InvalidOperationException("Коммутационное устройство или ППУ не найдены в списке устройств.");
      }

      await UserActionHelper.RunWithUserRepeatAsync(() => busSwitcher.ConnectorManager.ConnectBreakdownTester(), protocolUI);

      var relayModules = _devices.OfType<IRelaySwitchModule>().ToList();

      foreach (var module in relayModules)
      {
        if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => module.BusManager.ConnectBusAsync(SwitchingBus.A1), protocolUI))
          throw AppConfiguration.Error.Device.ModuleRelayControl.BusExceptionFactory.ConnectFailed(SwitchingBus.A1.ToString(), module.Name, module.NumberChassis, module.Number);

        if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => module.BusManager.ConnectBusAsync(SwitchingBus.B1), protocolUI))
          throw AppConfiguration.Error.Device.ModuleRelayControl.BusExceptionFactory.ConnectFailed(SwitchingBus.B1.ToString(), module.Name, module.NumberChassis, module.Number);
      }
    }
  }
}
