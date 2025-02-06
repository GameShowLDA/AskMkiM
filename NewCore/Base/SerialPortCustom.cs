using System.IO.Ports;
using System.Text.Json;

namespace NewCore.Base
{
  internal class SerialPortCustom : SerialPort
  {
    public override string ToString()
    {
      var dto = new SerialPortDTO()
      {
        PortName = this.PortName,
        BaudRate = this.BaudRate,
        Parity = this.Parity.ToString(),
        DataBits = this.DataBits,
        StopBits = this.StopBits.ToString(),
        Handshake = this.Handshake.ToString(),
        EncodingName = this.Encoding?.WebName,// Храним кодировку как строку
      };

      return JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
    }
    public static SerialPortCustom? ToObject(string str)
    {
      var data = JsonSerializer.Deserialize<SerialPortDTO>(str);
      if (data == null)
      {
        return null;
      }

      return new SerialPortCustom(data.PortName,
        data.BaudRate,
        (Parity)System.Enum.Parse(typeof(Parity), data.Parity),
        data.DataBits,
        (StopBits)System.Enum.Parse(typeof(StopBits), data.StopBits));
    }

    public SerialPortCustom(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
      : base(portName, baudRate, parity, dataBits, stopBits) { }
  }
}
