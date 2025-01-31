using NewCore.Base;
using NewCore.Function.GPT;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace NewCore.Device
{
  public class GPT79904 : DeviceWithCOM
  {
    public GPT79904(string VID, string PID)
    {
      string portName = FindComPort(VID, PID);
      COMPort = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One)
      {
        ReadTimeout = 2000,
        WriteTimeout = 2000,
        DtrEnable = true,
        RtsEnable = true,
        Handshake = Handshake.None
      };

      Name = "GPT79904";
      Description = "Реализовать описание в NewCore.Device.GPT79904";
    }

    public IIrMode IrMode => new IrMode(this);

    // TODO : нужен интерфейс
    public AcwMode AcwMode => new AcwMode(this);
    // TODO : нужен интерфейс
    public DcwMode DcwMode => new DcwMode(this);
  }
}
