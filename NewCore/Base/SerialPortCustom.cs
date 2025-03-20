using System.IO.Ports;
using System.Text.Json;

namespace NewCore.Base
{
  /// <summary>
  /// Кастомный класс для работы с последовательным портом, расширяющий функциональность <see cref="SerialPort"/>.
  /// </summary>
  public class SerialPortCustom : SerialPort
  {
    /// <summary>
    /// Преобразует объект SerialPortCustom в JSON-строку.
    /// </summary>
    /// <returns>JSON-строка с настройками порта.</returns>
    public override string ToString()
    {
      var dto = new SerialPortDTO
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
    /// Преобразует JSON-строку в объект SerialPortCustom.
    /// </summary>
    /// <param name="str">JSON-строка.</param>
    /// <returns>Объект SerialPortCustom или null, если десериализация не удалась.</returns>
    public static SerialPortCustom? ToObject(string str)
    {
      var data = JsonSerializer.Deserialize<SerialPortDTO>(str);
      return data == null ? null : new SerialPortCustom(data.PortName, data.BaudRate,
          System.Enum.TryParse(data.Parity, out Parity parity) ? parity : Parity.None,
          data.DataBits,
          System.Enum.TryParse(data.StopBits, out StopBits stopBits) ? stopBits : StopBits.One);
    }

    /// <summary>
    /// Создаёт экземпляр SerialPortCustom на основе объекта SerialPort.
    /// </summary>
    /// <param name="port">Экземпляр SerialPort, из которого будут скопированы настройки.</param>
    /// <returns>Новый экземпляр SerialPortCustom с настройками из переданного объекта.</returns>
    /// <exception cref="ArgumentNullException">Выбрасывается, если переданный `port` равен null.</exception>
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
    /// Создаёт новый экземпляр SerialPortCustom.
    /// </summary>
    /// <param name="portName">Имя порта (например, "COM1").</param>
    /// <param name="baudRate">Скорость передачи данных.</param>
    /// <param name="parity">Режим четности.</param>
    /// <param name="dataBits">Количество бит данных.</param>
    /// <param name="stopBits">Количество стоп-бит.</param>
    public SerialPortCustom(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        : base(portName, baudRate, parity, dataBits, stopBits) { }
  }
}
