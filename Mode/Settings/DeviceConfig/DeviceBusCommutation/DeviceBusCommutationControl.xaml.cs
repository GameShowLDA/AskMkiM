using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NewCore.Interface;

namespace Mode.Settings.DeviceConfig.DeviceBusCommutation
{
  /// <summary>
  /// Логика взаимодействия для DeviceBusCommutationControl.xaml
  /// </summary>
  public partial class DeviceBusCommutationControl : UserControl
  {
    public event EventHandler RequestSave;
    public event EventHandler RequestClose;
    public DeviceBusCommutationControl()
    {
      InitializeComponent();
      InitializeSettings();
    }

    private void InitializeSettings()
    {
      DefaultSettingControl.IsIpPart3Enabled = false;
      DefaultSettingControl.LoadDeviceModels<ISwitchingDevice>();
      DefaultSettingControl.RequestSave += DefaultSettingControl_RequestSave;
      DefaultSettingControl.RequestClose += DefaultSettingControl_RequestClose;
    }

    private void DefaultSettingControl_RequestClose(object? sender, EventArgs e)
    {
      RequestClose?.Invoke(this, EventArgs.Empty);
    }

    private void DefaultSettingControl_RequestSave(object? sender, EventArgs e)
    {
      RequestSave?.Invoke(this, EventArgs.Empty);
    }
  }
}
