using Ask.Core.Services.App;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Engine.Tests.Base;
using DataBaseConfiguration.Services.Device;

namespace Ask.Engine.Tests.MethodExecutor.MeasurementSystem
{
  /// <summary>
  /// Отвечает за сбор и подключение устройств, необходимых для теста.
  /// </summary>
  public class DeviceCollector
  {
    /// <summary>
    /// Список собранных устройств.
    /// </summary>
    public List<object> Devices { get; } = [];

    /// <summary>
    /// Выполняет сбор всех необходимых устройств по диапазону точек.
    /// </summary>
    /// <param name="startPoint">Начальная точка диапазона (A.B.C).</param>
    /// <param name="endPoint">Конечная точка диапазона (A.B.C).</param>
    public void Collect(PointModel startPoint, PointModel endPoint)
    {
      Devices.Clear();

      var deviceBusCommutationRepo = new SwitchingDeviceServices();
      var breakdownRepo = ServiceLocator.GetRequired<BreakdownTesterServices>();

      var relayModules = RelayModuleHelper.GetModulesByRange(startPoint.DeviceNumber, startPoint.ModuleNumber, endPoint.ModuleNumber);

      Devices.AddRange(relayModules);

      var deviceBusCommutation = deviceBusCommutationRepo.GetDevicesByNumberChassis(startPoint.DeviceNumber).FirstOrDefault();
      if (deviceBusCommutation != null)
      {
        Devices.Add(deviceBusCommutation);
      }

      var breakdown = breakdownRepo.GetDevicesByNumberChassis(startPoint.DeviceNumber).FirstOrDefault();
      if (breakdown != null)
      {
        Devices.Add(breakdown);
      }
    }

    /// <summary>
    /// Подключает все устройства, поддерживающие интерфейс <see cref="IDevice"/>.
    /// </summary>
    /// <returns>Результат подключения.</returns>
    public async Task<(bool Connect, string Message)> ConnectAllAsync(IUserInteractionService messageService)
    {
      foreach (var device in Devices)
      {
        if (device is IDevice connectable)
        {
          var (connected, message) = await connectable.ConnectableManager.ConnectAsync(messageService);
          if (!connected)
          {
            return (false, $"Не удалось подключить устройство {connectable.Name}({connectable.Number}) - {message}");
          }
        }
      }

      return (true, string.Empty);
    }
  }
}
