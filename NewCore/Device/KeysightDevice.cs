using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NewCore.Base;
using NewCore.Function.Keysight3466new;
using NewCore.Interface;

namespace NewCore.Device
{
  public class KeysightDevice : DeviceWithIP, IFastMeter
  {
    public IPAddress IP { get; set; }
    public int NumberChassis { get; set; }

    public bool IsConnected { get; set; }

    public KeysightCommunication KeysightCommunication;
    public KeysightConnection Connection; 
    public ContinuityMeasurement ContinuityMeasurement;
    public ResistanceMeasurement ResistanceMeasurement;
    public CapacitanceMeasurement CapacitanceMeasurement;
    public VoltageMeasurement VoltageMeasurement;

    public KeysightDevice(IPAddress ip) : this()
    {
      IP = ip;
    }

    public KeysightDevice()
    {
      Name = "Keysight 3466 new";
      Description = "Реализовать описание в NewCore.Device.KeysightDevice";
      IsConnected = false;

      KeysightCommunication = new KeysightCommunication(this);
      Connection = new KeysightConnection(this);
      ContinuityMeasurement = new ContinuityMeasurement(this);
      ResistanceMeasurement = new ResistanceMeasurement(this);
      CapacitanceMeasurement = new CapacitanceMeasurement(this);
      VoltageMeasurement = new VoltageMeasurement(this);
    }

    public override Task<bool> Initialize() => Connection.ConnectAsync();
  }
}
