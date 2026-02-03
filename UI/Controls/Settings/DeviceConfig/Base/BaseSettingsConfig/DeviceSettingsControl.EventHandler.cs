using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Message;
using NewCore.Base.Device;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace UI.Controls.Settings.DeviceConfig.Base.BaseSettingsConfig
{
  /// <summary>
  /// Частичный класс управления настройками устройства.
  /// </summary>
  public partial class DeviceSettingsControl
  {
    private bool _internalChange;

    /// <summary>
    /// Обрабатывает изменение выбранной модели шасси.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события выбора.</param>
    private void ChassisModelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
    }

    /// <summary>
    /// Обрабатывает изменение выбранного номера стойки.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события выбора.</param>
    private void RacksNumberBorder_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
    }

    /// <summary>
    /// Обрабатывает изменение выбранного номера стойки.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события выбора.</param>
    private void BusTypeSelectionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
    }

    /// <summary>
    /// Обрабатывает изменение выбранной модели устройства и обновляет интерфейс
    /// в зависимости от типа подключения (IP или COM).
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события выбора.</param>
    private void DeviceModelSelectionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (DeviceModelSelectionBox.SelectedItem is not string selectedModel ||
          !DeviceModelMap.TryGetValue(selectedModel, out Type selectedType))
      {
        return;
      }

      try
      {
        Type baseClass = GetBaseDeviceType(selectedType);

        ConnectionTypeIPItem.Visibility = baseClass == typeof(DeviceWithIP) ? Visibility.Visible : Visibility.Collapsed;
        ConnectionTypeCOMItem.Visibility = baseClass == typeof(DeviceWithCOM) ? Visibility.Visible : Visibility.Collapsed;

        DeviceNumberContainer.Visibility = Visibility.Visible;

        if (typeof(IRelaySwitchModule).IsAssignableFrom(selectedType))
        {
          BusTypeContainer.Visibility = Visibility.Visible;
          ResistanceContainer.Visibility = Visibility.Visible;
        }
        else
        {
          BusTypeContainer.Visibility = Visibility.Collapsed;
          ResistanceContainer.Visibility = Visibility.Collapsed;
        }

        if (baseClass == typeof(DeviceWithCOM))
        {
          object deviceModel = Activator.CreateInstance(selectedType);
          ApplyCOMSettingsFromModel(deviceModel);
        }
      }
      catch (InvalidOperationException ex)
      {
        MessageBoxCustom.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    /// <summary>
    /// Обрабатывает изменение типа подключения, настраивая доступные параметры.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события выбора.</param>
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

    /// <summary>
    /// Ограничивает ввод только числовыми значениями для номера устройства.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события ввода текста.</param>
    private void NumberDevice_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
    }

    /// <summary>
    /// Обрабатывает изменение номера устройства и отображает контейнер типа подключения.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события изменения текста.</param>
    private void NumberDevice_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (sender is TextBox textBox)
      {
        if (int.TryParse(textBox.Text, out int number) && number >= 1 && number <= 250)
        {
          ConnectionTypeContainer.Visibility = Visibility.Visible;
        }
      }
    }


    /// <summary>
    /// Ограничивает ввод только числовыми значениями для номера устройства.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события ввода текста.</param>
    private void ResistanceDevice_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      e.Handled = !char.IsDigit(e.Text, 0) && e.Text != "." && e.Text != ",";
    }

    private void ResistanceDevice_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (_internalChange)
        return;

      if (sender is not TextBox textBox)
        return;

      if (string.IsNullOrWhiteSpace(textBox.Text))
      {
        ConnectionTypeContainer.Visibility = Visibility.Collapsed;
        return;
      }

      _internalChange = true;

      string text = textBox.Text.Replace(',', '.');

      if (text.StartsWith("."))
      {
        textBox.Text = string.Empty;
        _internalChange = false;
        return;
      }

      if (text.Count(c => c == '.') > 1)
      {
        textBox.Text = text.Remove(text.LastIndexOf('.'), 1);
      }

      textBox.Text = text;
      textBox.CaretIndex = textBox.Text.Length;

      _internalChange = false;

      if (double.TryParse(
            text,
            NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out _))
      {
        ConnectionTypeContainer.Visibility = Visibility.Visible;
      }
      else
      {
        ConnectionTypeContainer.Visibility = Visibility.Collapsed;
      }
    }

    /// <summary>
    /// Обработчик изменений свойства <see cref="AdditionalSettings"/>.
    /// Обновляет содержимое контейнера дополнительных настроек.
    /// </summary>
    /// <param name="d">Объект зависимости.</param>
    /// <param name="e">Аргументы изменения свойства.</param>
    private static void OnAdditionalSettingsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is DeviceSettingsControl control)
      {
        control.AdditionalSettingsContainer.Content = e.NewValue;
      }
    }

    /// <summary>
    /// Обрабатывает нажатие кнопки сохранения.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события нажатия кнопки мыши.</param>
    private void SaveButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      SaveEvent?.Invoke(this, e);
    }

    /// <summary>
    /// Обрабатывает нажатие кнопки отмены.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события нажатия кнопки мыши.</param>
    private void CancelButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      RequestClose?.Invoke(this, e);
    }

    /// <summary>
    /// Обрабатывает изменение выбранного COM-порта.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события выбора.</param>
    private void COMPortSelectionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (COMPortSelectionBox.SelectedItem is string selectedPort)
      {
        GetVidPidForPort(selectedPort);
      }
    }
  }
}
