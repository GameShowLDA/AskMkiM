using System.Net;
using System.Net.Sockets;
using NewCore.Base.Device;
using NewCore.Base.Function.FastMeter;
using NewCore.Base.Interface.Main;
using NewCore.Function.Keysight3466new;

namespace NewCore.Device
{
  public class KeysightDevice : DeviceWithIP, IFastMeter
  {
    public IPAddress IP { get; set; }
    public int NumberChassis { get; set; }

    public bool IsConnected { get; set; }
    public ICapacitanceMeasurement CapacitanceManager { get; set; }
    public IConnection ConnectionManager { get; set; }
    public IContinuityMeasurement ContinuityManager { get; set; }
    public IAcVoltageMeasurement AcVoltageManager { get; set; }
    public IDcVoltageMeasurement DcVoltageManager { get; set; }
    public IResistanceMeasurement ResistanceManager { get; set; }
    public ICommunication CommunicationManager { get; set; }

    internal readonly int Port = 5025;
    internal TcpClient Client;
    internal NetworkStream Stream;

    public KeysightDevice(IPAddress ip) : this()
    {
      IP = ip;
    }

    public KeysightDevice()
    {
      Name = "Keysight 3466 new";
      Description = "Реализовать описание в NewCore.Device.KeysightDevice";
      DeviceClass = GetType().FullName;

      IsConnected = false;

      CapacitanceManager = new CapacitanceMeasurement(this);
      CommunicationManager = new KeysightCommunication(this);
      ConnectionManager = new KeysightConnection(this);
      ContinuityManager = new ContinuityMeasurement(this);
      ResistanceManager = new ResistanceMeasurement(this);
      AcVoltageManager = new AcVoltageMeasurement(this);
      DcVoltageManager = new DcVoltageMeasurement(this);
    }
  }
}
