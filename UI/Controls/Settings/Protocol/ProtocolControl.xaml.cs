using AppConfiguration.Protocol;
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
using static AppConfiguration.Protocol.ProtocolConfig;


namespace UI.Controls.Settings.Protocol
{
  /// <summary>
  /// Логика взаимодействия для ProtocolControl.xaml
  /// </summary>
  public partial class ProtocolControl : UserControl, ISettingsManager<ProtocolModel>
  {
    public ProtocolControl()
    {
      InitializeComponent();
      Loaded += ProtocolControl_Loaded;
    }

    public event EventHandler ChangedData;

    private async void ProtocolControl_Loaded(object sender, RoutedEventArgs e)
    {
      var baseModel = await GetProtocolModel();

      DeviceInfo.IsChecked = baseModel.ShowDeviceInfo;
      DeviceInfo.CheckedChanged += CheckedChanged;

      AutoSave.IsChecked = baseModel.AutoSaveProtocol;
      AutoSave.CheckedChanged += CheckedChanged;

      AutoPrint.IsChecked = baseModel.AutoPrintProtocol;
      AutoPrint.CheckedChanged += CheckedChanged;

      OperationTime.IsChecked = baseModel.DisplayOperationTime;
      OperationTime.CheckedChanged += CheckedChanged;
    }

    private void CheckedChanged(object? sender, bool e)
    {
      ChangedData?.Invoke(this, EventArgs.Empty);
    }

    public ProtocolModel GetActiveModel()
    {
      return new ProtocolModel
      {
        ShowDeviceInfo = DeviceInfo.IsChecked,
        AutoSaveProtocol = AutoSave.IsChecked,
        AutoPrintProtocol = AutoPrint.IsChecked,
        DisplayOperationTime = OperationTime.IsChecked
      };
    }
  }
}
