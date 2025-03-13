using System.IO.Ports;
using NewCore.Base;
using NewCore.Function.GPT;
using NewCore.Interface;

namespace NewCore.Device
{
  public class GPT79904 : DeviceWithCOM, IBreakdownTester
  {
    public GPT79904() { }
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
    }

    public string Name { get => "GPT79904"; }
    public string Description { get => "Реализовать описание в NewCore.Device.GPT79904"; }

    public IIrMode IrMode => new IrMode(this);

    // TODO : нужен интерфейс
    public AcwMode AcwMode => new AcwMode(this);
    // TODO : нужен интерфейс
    public DcwMode DcwMode => new DcwMode(this);

    public int NumberChassis { get ; set ; }
  }
}
