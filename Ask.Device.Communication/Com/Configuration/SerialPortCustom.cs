using System.IO.Ports;
using System.Text;
using System.Text.Json;

namespace Ask.Device.Communication.Com.Configuration
{
  /// <summary>
  /// Представляет последовательный порт, который умеет сериализовать и восстанавливать свои настройки.
  /// </summary>
  public class SerialPortCustom : SerialPort
  {
    /// <summary>
    /// Преобразует текущие настройки порта в JSON-строку.
    /// </summary>
    /// <returns>JSON-строка с настройками порта.</returns>
    public override string ToString()
    {
      var dto = new SerialPortSettingsDto
      {
        PortName = PortName,
        BaudRate = BaudRate,
        Parity = Parity.ToString(),
        DataBits = DataBits,
        StopBits = StopBits.ToString(),
        Handshake = Handshake.ToString(),
        EncodingName = Encoding?.WebName ?? string.Empty,
      };

      return JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Восстанавливает экземпляр <see cref="SerialPortCustom"/> из JSON-строки.
    /// </summary>
    /// <param name="str">JSON-строка с настройками порта.</param>
    /// <returns>Настроенный экземпляр <see cref="SerialPortCustom"/> или <see langword="null"/>, если строка невалидна.</returns>
    public static SerialPortCustom? ToObject(string str)
    {
      if (string.IsNullOrWhiteSpace(str))
      {
        return null;
      }

      try
      {
        var data = JsonSerializer.Deserialize<SerialPortSettingsDto>(str);
        return data == null
          ? null
          : new SerialPortCustom(
            data.PortName,
            data.BaudRate,
            System.Enum.TryParse(data.Parity, out Parity parity) ? parity : Parity.None,
            data.DataBits,
            System.Enum.TryParse(data.StopBits, out StopBits stopBits) ? stopBits : StopBits.One)
          {
            Handshake = System.Enum.TryParse(data.Handshake, out Handshake handshake) ? handshake : Handshake.None,
            Encoding = string.IsNullOrWhiteSpace(data.EncodingName) ? Encoding.ASCII : Encoding.GetEncoding(data.EncodingName),
          };
      }
      catch (JsonException)
      {
        return null;
      }
      catch (ArgumentException)
      {
        return null;
      }
    }

    /// <summary>
    /// Создаёт экземпляр <see cref="SerialPortCustom"/> на основе существующего <see cref="SerialPort"/>.
    /// </summary>
    /// <param name="port">Исходный последовательный порт.</param>
    /// <returns>Новый экземпляр <see cref="SerialPortCustom"/> с теми же настройками.</returns>
    /// <exception cref="ArgumentNullException">Выбрасывается, если <paramref name="port"/> равен <see langword="null"/>.</exception>
    public static SerialPortCustom FromSerialPort(SerialPort port)
    {
      if (port == null)
      {
        throw new ArgumentNullException(nameof(port), "Переданный SerialPort не должен быть null.");
      }

      return new SerialPortCustom(port.PortName, port.BaudRate, port.Parity, port.DataBits, port.StopBits)
      {
        Handshake = port.Handshake,
        Encoding = port.Encoding,
        ReadTimeout = port.ReadTimeout,
        WriteTimeout = port.WriteTimeout,
        DtrEnable = port.DtrEnable,
        RtsEnable = port.RtsEnable,
        NewLine = port.NewLine,
      };
    }

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="SerialPortCustom"/>.
    /// </summary>
    /// <param name="portName">Имя порта, например <c>COM1</c>.</param>
    /// <param name="baudRate">Скорость передачи данных.</param>
    /// <param name="parity">Режим чётности.</param>
    /// <param name="dataBits">Количество бит данных.</param>
    /// <param name="stopBits">Количество стоп-бит.</param>
    public SerialPortCustom(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
      : base(portName, baudRate, parity, dataBits, stopBits)
    {
    }
  }
}
