using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Entity.Settings;
using System.Windows;
using System.Windows.Controls;

namespace UI.Controls.Settings.DeviceInfo
{
  /// <summary>
  /// Логика взаимодействия для DeviceDisplaySettingsControl.xaml
  /// </summary>
  public partial class DeviceDisplaySettingsControl : UserControl
  {
    /// <summary>
    /// Базовая (сохранённая) модель протокола, считанная при загрузке.
    /// Используется как эталон для сравнения с текущими значениями UI.
    /// </summary>
    private DeviceDisplaySettingsModel _baseModel { get; set; }

    public event Action<bool> DeviceDisplayModelChanged;

    public DeviceDisplaySettingsControl()
    {
      InitializeComponent();
      Loaded += ProtocolControl_Loaded;
    }

    /// <summary>
    /// Обработчик события загрузки контрола.
    /// Подгружает сохранённую модель, заполняет UI и подписывается на изменения.
    /// </summary>
    private async void ProtocolControl_Loaded(object sender, RoutedEventArgs e)
    {
      _baseModel = await DeviceDisplayConfig.GetDeviceDisplayModel();
      DefalultData();

      MachineAddressesCard.CheckedChanged += CheckedChanged;
      ConnectionInfo.CheckedChanged += CheckedChanged;
      DeviceExecutionParameters.CheckedChanged += CheckedChanged;
      MeasurementResults.CheckedChanged += CheckedChanged;
    }

    /// <summary>
    /// Унифицированный обработчик изменений любого переключателя.
    /// Сравнивает текущую модель с сохранённой и показывает/скрывает индикаторы.
    /// </summary>
    private async void CheckedChanged(object? sender, bool e)
    {
      if (!ProtocolEquals(_baseModel, GetModel()))
      {
        DeviceDisplayModelChanged?.Invoke(true);
      }
      else
      {
        DeviceDisplayModelChanged?.Invoke(false);
      }
    }

    /// <summary>
    /// Формирует модель протокола из текущих значений элементов UI.
    /// </summary>
    internal DeviceDisplaySettingsModel GetModel()
    {
      var model = new DeviceDisplaySettingsModel
      {
        ShowMachineAddresses = MachineAddressesCard.IsChecked,
        ShowConnectionInfo = ConnectionInfo.IsChecked,
        ShowDeviceExecutionParameters = DeviceExecutionParameters.IsChecked,
        ShowMeasurementResults = MeasurementResults.IsChecked,
      };

      return model;
    }

    internal async Task SetBaseModel()
    {
      _baseModel = await DeviceDisplayConfig.GetDeviceDisplayModel();
    }

    /// <summary>
    /// Заполняет элементы UI значениями из базовой (сохранённой) модели.
    /// </summary>
    internal void DefalultData()
    {
      MachineAddressesCard.IsChecked = _baseModel.ShowMachineAddresses;
      ConnectionInfo.IsChecked = _baseModel.ShowConnectionInfo;
      DeviceExecutionParameters.IsChecked = _baseModel.ShowDeviceExecutionParameters;
      MeasurementResults.IsChecked = _baseModel.ShowMeasurementResults;
    }

    /// <summary>
    /// Сравнивает две модели протокола по всем флагам.
    /// </summary>
    private static bool ProtocolEquals(DeviceDisplaySettingsModel a, DeviceDisplaySettingsModel b) =>
      a.ShowMachineAddresses == b.ShowMachineAddresses &&
      a.ShowConnectionInfo == b.ShowConnectionInfo &&
      a.ShowDeviceExecutionParameters == b.ShowDeviceExecutionParameters &&
      a.ShowMeasurementResults == b.ShowMeasurementResults;
  }
}
