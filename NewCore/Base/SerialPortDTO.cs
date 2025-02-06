using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewCore.Base
{
  public class SerialPortDTO
  {
    public string PortName { get; set; }
    public int BaudRate { get; set; }
    public string Parity { get; set; }
    public int DataBits { get; set; }
    public string StopBits { get; set; }
    public string Handshake { get; set; }
    public string EncodingName { get; set; }
  }
}
