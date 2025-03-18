using System.IO.Ports;
using NewCore.Base;
using NewCore.Base.Device;
using NewCore.Base.Function.Breakdown;
using NewCore.Base.Interface.Main;
using NewCore.Function.GPT;
using NewCore.Function.GPT.Command;

namespace NewCore.Device
{
  public class GPT79904 : DeviceWithCOM, IBreakdownTester
  {
    public GPT79904()
    {
      BaudRate = 115200;
      StopBits = StopBits.One;
      DataBits = 8;
      Parity = Parity.None;
      DeviceClass = GetType().FullName;
      AcwManger = new AcwMode(this);
      DcwManger = new DcwMode(this);
      IrManger = new IrMode(this);
      SystemManger = new SystemSettings(this);
    }
    public GPT79904(string VID, string PID) : this()
    {
      string portName = FindComPort(VID, PID);


      COMPort = new SerialPort(portName, BaudRate, Parity, DataBits, StopBits)
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

    public int NumberChassis { get; set; }
    public IAcwModeBreakdown AcwManger { get ; set ; }
    public IDcwModeBreakdown DcwManger { get; set; }
    public IIrModeBreakdown IrManger { get; set; }
    public ISystemSettingsBreakdown SystemManger {  get ; set ; }
  }
}
