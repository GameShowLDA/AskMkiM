using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NewCore.Base.Interface.Main;

namespace NewCore.Communication
{
  /// <summary>
  /// Реализация <see cref="IDeviceProtocol"/> для общения с мультиметрами Keysight.
  /// </summary>
  public class KeysightDeviceProtocol : IDeviceProtocol
  {
    private readonly IFastMeter _device;
    private readonly int _port;

    /// <summary>
    /// Сетевой поток для передачи команд и получения данных.
    /// </summary>
    internal NetworkStream Stream { get; set; }

    /// <summary>
    /// TCP-клиент для установления соединения с устройством.
    /// </summary>
    internal TcpClient Client { get; set; }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="KeysightDeviceProtocol"/>.
    /// </summary>
    /// <param name="device">Устройство измерения, с которым устанавливается связь.</param>
    /// <param name="port">Порт, на котором выполняется подключение к устройству.</param>
    public KeysightDeviceProtocol(IFastMeter device, int port)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _port = port;
    }

    /// <inheritdoc />
    public async Task<string> QueryAsync(string command, int timeout = 0, int responseDelay = 0, int port = 0)
    {
      if (Client == null || !Client.Connected)
      {
        await EstablishConnection();
      }

      await SendCommandAsync(command);

      if (responseDelay > 0)
      {
        await Task.Delay(responseDelay);
      }

      if (timeout > 0)
      {
        byte[] buffer = new byte[1024];
        int bytesRead = await Stream.ReadAsync(buffer, 0, buffer.Length);
        return Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
      }

      return string.Empty;
    }

    /// <summary>
    /// Устанавливает TCP-соединение с устройством, если оно ещё не подключено.
    /// </summary>
    private async Task EstablishConnection()
    {
      Client = new TcpClient();
      await Client.ConnectAsync(_device.ConnectionDetails, _port);
      Stream = Client.GetStream();
    }

    /// <summary>
    /// Отправляет SCPI-команду без ожидания ответа.
    /// </summary>
    /// <param name="command">SCPI-команда для отправки.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    private async Task SendCommandAsync(string command)
    {
      byte[] data = Encoding.ASCII.GetBytes(command + "\n");
      await Stream.WriteAsync(data, 0, data.Length);
    }
  }
}
