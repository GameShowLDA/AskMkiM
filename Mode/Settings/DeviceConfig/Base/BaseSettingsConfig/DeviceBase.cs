using Mode.Settings.DeviceConfig.WindowSettings;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Mode.Settings.DeviceConfig.Base.BaseSettingsConfig
{
  public class DeviceBase
  {
    DeviceSettingsControl deviceSettingsControl;

    public DeviceBase(DeviceSettingsControl deviceSettingsControl)
    {
      this.deviceSettingsControl = deviceSettingsControl;
    }

    #region Property

    public int IpPart1Value => deviceSettingsControl.IpPart1Value;
    public int IpPart2Value => deviceSettingsControl.IpPart2Value;
    public int IpPart3Value => deviceSettingsControl.IpPart3Value;
    public int IpPart4Value => deviceSettingsControl.IpPart4Value;
    public int BaudRateValue => deviceSettingsControl.BaudRateValue;
    public int DataBitsValue => deviceSettingsControl.DataBitsValue;
    public Parity ParityValue => deviceSettingsControl.ParityValue;
    public StopBits StopBitsValue => deviceSettingsControl.StopBitsValue;
    public string PortName => deviceSettingsControl.PortName;

    #endregion
  }
}
