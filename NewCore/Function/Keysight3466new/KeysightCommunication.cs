using System.Text;
using NewCore.Base.Function.FastMeter;
using NewCore.Device;

namespace NewCore.Function.Keysight3466new
{
  /// <summary>
  /// Класс для управления коммуникацией с прибором Keysight через SCPI-команды.
  /// </summary>
  public class KeysightCommunication : ICommunication
  {
    /// <summary>
    /// Экземпляр прибора Keysight.
    /// </summary>
    private readonly KeysightDevice _device;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="KeysightCommunication"/>.
    /// </summary>
    /// <param name="device">Экземпляр устройства Keysight.</param>
    /// <exception cref="ArgumentNullException">Выбрасывается, если переданный прибор равен <c>null</c>.</exception>
    public KeysightCommunication(KeysightDevice device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    /// <summary>
    /// Отправляет SCPI-команду без ожидания ответа.
    /// </summary>
    /// <param name="command">SCPI-команда для отправки.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    public async Task SendCommandAsync(string command)
    {
      byte[] data = Encoding.ASCII.GetBytes(command + "\n");
      await _device.Stream.WriteAsync(data, 0, data.Length);
    }

    /// <summary>
    /// Отправляет SCPI-команду и получает ответ от прибора.
    /// </summary>
    /// <param name="command">SCPI-команда для запроса данных.</param>
    /// <returns>Ответ от прибора в виде строки.</returns>
    public async Task<string> QueryAsync(string command)
    {
      await SendCommandAsync(command);

      byte[] buffer = new byte[1024];
      int bytesRead = await _device.Stream.ReadAsync(buffer, 0, buffer.Length);
      return Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
    }
  }
}
