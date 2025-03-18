using System.Text;
using NewCore.Base.Function.FastMeter;
using NewCore.Device;

namespace NewCore.Function.Keysight3466new
{

  public class KeysightCommunication : ICommunication
  {
    private readonly KeysightDevice _device;


    public KeysightCommunication(KeysightDevice device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    /// <summary>
    /// Отправляет SCPI-команду без ожидания ответа.
    /// </summary>
    public async Task SendCommandAsync(string command)
    {
      byte[] data = Encoding.ASCII.GetBytes(command + "\n");
      await _device.Stream.WriteAsync(data, 0, data.Length);
    }

    /// <summary>
    /// Отправляет SCPI-команду и получает ответ.
    /// </summary>
    public async Task<string> QueryAsync(string command)
    {
      await SendCommandAsync(command);

      byte[] buffer = new byte[1024];
      int bytesRead = await _device.Stream.ReadAsync(buffer, 0, buffer.Length);
      return Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
    }
  }
}