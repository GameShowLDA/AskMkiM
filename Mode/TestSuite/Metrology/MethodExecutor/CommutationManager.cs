using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NewCore.Base.Interface.Main;
using static NewCore.Enum.DeviceEnum;
using UI.Controls.Protocol;
using Utilities.Models;

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

      await protocolUI.ShowMessageAsync(new ShowMessageModel(
          $"Подключение {breakdown.Name}({breakdown.Number}) к {busSwitcher.Name}({busSwitcher.Number})"));

      await busSwitcher.ConnectorManager.ConnectBreakdownTester();

      var relayModules = _devices.OfType<IRelaySwitchModule>().ToList();

      foreach (var module in relayModules)
      {
        await module.BusManager.ConnectBusAsync(SwitchingBus.A1);
        await module.BusManager.ConnectBusAsync(SwitchingBus.B1);
      }
    }
  }
}
