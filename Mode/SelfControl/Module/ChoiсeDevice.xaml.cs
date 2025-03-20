using System.Windows;
using System.Windows.Controls;
using Core.Model;

namespace Mode.SelfControl.Module
{
  /// <summary>
  /// Пользовательский интерфейс для выбора устройства.
  /// Отображает список устройств и позволяет выбрать активное устройство.
  /// </summary>
  public partial class ChoiсeDevice : UserControl
  {
    /// <summary>
    /// Инициализирует компонент и настройки.
    /// </summary>
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
    public delegate void DeviceSelectedEventHandler();

    /// <summary>
    /// Событие, возникающее при выборе устройства.
    /// </summary>
    public event DeviceSelectedEventHandler DeviceSelected;

    /// <summary>
    /// Возвращает выбранное устройство.
    /// </summary>
    /// <returns>Выбранное устройство.</returns>
    internal DeviceModel GetActiveDevice()
    {
      return deviceModels;
    }

    /// <summary>
    /// Инициализирует настройки компонента.
    /// </summary>
    private void InitializeSettings()
    {
      deviceList.SelectionChanged += MkrListView_SelectionChanged;
      deviceList.ItemContainerStyle = FindResource(typeof(ListViewItem)) as Style;
      popup.Width = toggleButton.Width;
    }

    /// <summary>
    /// Добавляет устройство в список.
    /// </summary>
    /// <param name="deviceModel">Устройство для добавления.</param>
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

    /// <summary>
    /// Обрабатывает изменение выбранного элемента в списке устройств.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void MkrListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (deviceList.SelectedItem != null)
      {
        var selectedItem = deviceList.SelectedItem as ListViewItem;
        toggleButton.Content = selectedItem.Content.ToString();
        toggleButton.IsChecked = false;
        deviceModels = selectedItem.Tag as DeviceModel;
        DeviceSelected?.Invoke();
      }
    }
  }
}
