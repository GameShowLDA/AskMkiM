using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppManager.DataBase.Services;
using Mode.Models;
using NewCore.Base.Device;

namespace Mode.TestSuite.Metrology.MethodExecutor
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
      var breakdownRepo = new BreakdownTesterServices();

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
    public async Task<(bool Connect, string Message)> ConnectAllAsync()
    {
      foreach (var device in Devices)
      {
        if (device is IDevice connectable)
        {
          var (connected, message) = await connectable.ConnectableManager.ConnectAsync();
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
