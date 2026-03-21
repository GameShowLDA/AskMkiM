using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Device.Communication.Com
{
  /// <summary>
  /// Объект передачи данных (DTO) для конфигурации последовательного порта.
  /// </summary>
  public class SerialPortDTO
  {
    /// <summary>
    /// Имя порта (например, "COM1").
    /// </summary>
    public string PortName { get; set; }

    /// <summary>
    /// Скорость передачи данных в бодах.
    /// </summary>
    public int BaudRate { get; set; }

    /// <summary>
    /// Тип бита четности (None, Odd, Even, Mark, Space).
    /// </summary>
    public string Parity { get; set; }

    /// <summary>
    /// Количество бит данных (например, 7 или 8).
    /// </summary>
    public int DataBits { get; set; }

    /// <summary>
    /// Количество стоп-бит (None, One, OnePointFive, Two).
    /// </summary>
    public string StopBits { get; set; }

    /// <summary>
    /// Тип управления потоком (None, XOnXOff, RequestToSend, RequestToSendXOnXOff).
    /// </summary>
    public string Handshake { get; set; }

    /// <summary>
    /// Имя кодировки для передачи данных (например, "UTF-8", "ASCII").
    /// </summary>
    public string EncodingName { get; set; }
  }
}
