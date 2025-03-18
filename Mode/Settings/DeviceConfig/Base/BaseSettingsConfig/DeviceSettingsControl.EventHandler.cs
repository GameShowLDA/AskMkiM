using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NewCore.Base.Device;

namespace Mode.Settings.DeviceConfig.Base.BaseSettingsConfig
{
  partial class DeviceSettingsControl
  {
    private void ChassisModelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {

    }

    private void RacksNumberBorder_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {

    }

    private void DeviceModelSelectionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {

      if (DeviceModelSelectionBox.SelectedItem is not string selectedModel ||
          !DeviceModelMap.TryGetValue(selectedModel, out Type selectedType))
        return;

      try
      {
        Type baseClass = GetBaseDeviceType(selectedType);

        ConnectionTypeIPItem.Visibility = baseClass == typeof(DeviceWithIP) ? Visibility.Visible : Visibility.Collapsed;
        ConnectionTypeCOMItem.Visibility = baseClass == typeof(DeviceWithCOM) ? Visibility.Visible : Visibility.Collapsed;

        DeviceNumberContainer.Visibility = Visibility.Visible;

        if (baseClass == typeof(DeviceWithCOM))
        {
          object deviceModel = Activator.CreateInstance(selectedType);
          ApplyCOMSettingsFromModel(deviceModel);
        }
      }
      catch (InvalidOperationException ex)
      {
        MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    /// <summary>
    /// Обрабатывает изменение типа подключения, настраивая доступные параметры.
    /// При выборе IP скрывает COM-настройки и наоборот.
    /// </summary>
    /// <param name="sender">Источник события (обычно ComboBox).</param>
    /// <param name="e">Аргументы события, содержащие информацию об изменении выбора.</param>
    private void ConnectionTypeSelectionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (ConnectionTypeSelectionBox.SelectedItem is ComboBoxItem selectedItem)
      {
        if (AdditionalSettingsContainer != null)
        {
          AdditionalSettingsContainer.Visibility = Visibility.Visible;
        }

        string selectedType = selectedItem.Content.ToString().ToLower();

        if (selectedType.Contains("ip"))
        {
          ShowIP();
        }
        else if (selectedType.Contains("com"))
        {
          COMContainer.Visibility = Visibility.Visible;
          PopulateCOMPorts();
        }
      }
    }

    private void NumberDevice_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
    }

    private void NumberDevice_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (sender is TextBox textBox)
      {
        if (int.TryParse(textBox.Text, out int number))
        {
          if (number >= 1 && number <= 250)
          {
            ConnectionTypeContainer.Visibility = Visibility.Visible;
          }
        }
      }
    }

    /// <summary>
    /// Обработчик изменений свойства <see cref="AdditionalSettings"/>.
    /// Обновляет содержимое контейнера дополнительных настроек.
    /// </summary>
    private static void OnAdditionalSettingsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is DeviceSettingsControl control)
      {
        control.AdditionalSettingsContainer.Content = e.NewValue;
      }
    }

    private void SaveButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      SaveEvent?.Invoke(this, e);
    }

    private void CancelButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      RequestClose?.Invoke(this, e);
    }

    private void COMPortSelectionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (COMPortSelectionBox.SelectedItem is string selectedPort)
      {
        GetVidPidForPort(selectedPort);
      }
    }
  }
}
