using System.Windows;
using System.Windows.Controls;
using Core.Model;

namespace Mode.SelfControl.Module
{
  /// <summary>
  /// Логика взаимодействия для ChoiсeDevice.xaml
  /// </summary>
  public partial class ChoiсeDevice : UserControl
  {
    public ChoiсeDevice()
    {
      InitializeComponent();
      InitializeSettings();
    }

    /// <summary>
    /// Модель выбранного устройства.
    /// </summary>
    DeviceModel deviceModels;

    /// <summary>
    /// Делегат для события изменения выбранного устройства.
    /// </summary>
    /// <param name="sender">Объект, вызвавший событие.</param>
    /// <param name="device">Выбранное устройство.</param>
    public delegate void DeviceSelectedEventHandler();

    /// <summary>
    /// Событие, возникающее при выборе устройства.
    /// </summary>
    public event DeviceSelectedEventHandler DeviceSelected;

    internal DeviceModel GetActiveDevice()
    {
      return deviceModels;
    }

    private void InitializeSettings()
    {
      deviceList.SelectionChanged += MkrListView_SelectionChanged;
      deviceList.ItemContainerStyle = FindResource(typeof(ListViewItem)) as Style;
      popup.Width = toggleButton.Width;
    }

    public void AddDevice(DeviceModel deviceModel)
    {
      ListViewItem listViewItem = new ListViewItem()
      {
        Content = deviceModel.Name + " " + deviceModel.Number,
        Height = 40,
        Tag = deviceModel,
      };

      deviceList.Items.Add(listViewItem);
      if (deviceList.Height > 200)
      {
        deviceList.Height = 200;
      }
    }

    private void MkrListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (deviceList.SelectedItem != null)
      {
        var selectedItem = deviceList.SelectedItem as ListViewItem;
        toggleButton.Content = selectedItem.Content.ToString();
        toggleButton.IsChecked = false;
        deviceModels = (selectedItem.Tag) as DeviceModel;
        DeviceSelected?.Invoke();
      }
    }
  }
}
