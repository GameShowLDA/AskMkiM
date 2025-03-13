using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewCore.Device;

namespace NewCore.Function.Keysight3466new
{
  public class ContinuityMeasurement
  {
    private readonly KeysightDevice _device;
    private readonly KeysightCommunication _communication;

    public ContinuityMeasurement(KeysightDevice device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _communication = _device.KeysightCommunication;
    }

    /// <summary>
    /// Устанавливает прибор в режим прозвонки (Continuity Test).
    /// </summary>
    public async Task SetContinuityModeAsync()
    {
      if (!_device.IsConnected)
        throw new InvalidOperationException("Прибор не подключен.");

      await _communication.SendCommandAsync("CONF:CONT");
    }

    /// <summary>
    /// Проверяет проводимость (если есть связь – возвращает true, иначе false).
    /// </summary>
    public async Task<bool> CheckContinuityAsync()
    {
      if (!_device.IsConnected)
        throw new InvalidOperationException("Прибор не подключен.");

      string response = await _communication.QueryAsync("MEAS:CONT?");
      return response.Trim() == "1";
    }
  }
}
