using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Communication.Com;
using Ask.Device.Communication.Ethernet;
using Ask.Device.Communication.Usb;
using Ask.Device.Runtime.Base.Device;
using Ask.UI.Infrastructure.Localization;
using Message;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using UI.Controls.Settings.DeviceConfig.Base;

namespace UI.Controls.Settings.DeviceConfig.Base.BaseSettingsConfig
{
  /// <summary>
  /// Частичный класс управления настройками устройства.
  /// </summary>
  public partial class DeviceSettingsControl
  {
    private bool _internalChange;

    /// <summary>
    /// Признак синхронизации номера устройства и последнего октета IP-адреса.
    /// </summary>
    private bool _synchronizingDeviceNumberAndIp;

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
        ResetSettingsForDeviceModelChange();

        Type baseClass = GetBaseDeviceType(selectedType);

        ConnectionTypeIPItem.Visibility = baseClass == typeof(DeviceWithIP) ? Visibility.Visible : Visibility.Collapsed;
        ConnectionTypeCOMItem.Visibility = baseClass == typeof(DeviceWithCOM) ? Visibility.Visible : Visibility.Collapsed;
        ConnectionTypeUSBItem.Visibility = baseClass == typeof(DeviceWithUSB) ? Visibility.Visible : Visibility.Collapsed;

        DeviceNumberContainer.Visibility = Visibility.Visible;
        AdditionalSettingsContainer.Visibility = Visibility.Visible;
        if (typeof(IRelaySwitchModule).IsAssignableFrom(selectedType))
        {
          BusTypeContainer.Visibility = Visibility.Visible;
          ResistanceContainer.Visibility = Visibility.Visible;
          CapacitanceContainer.Visibility = Visibility.Visible;
          var relayModule = (IRelaySwitchModule)Activator.CreateInstance(selectedType)!;
          RelayPointCountContainer.Visibility = Visibility.Visible;
          SetRelayPointCount(relayModule.PointCount);
        }
        else
        {
          BusTypeContainer.Visibility = Visibility.Collapsed;
          ResistanceContainer.Visibility = Visibility.Collapsed;
          CapacitanceContainer.Visibility = Visibility.Collapsed;
          RelayPointCountContainer.Visibility = Visibility.Collapsed;
        }

        if (typeof(IMultimeter).IsAssignableFrom(selectedType))
        {
          ShowFastMeterAdditionalSettings(sender as IMultimeter);
        }
        else if (typeof(IBreakdownTester).IsAssignableFrom(selectedType))
        {
          ShowBreakdownTesterAdditionalSettings();
        }
        if (baseClass == typeof(DeviceWithCOM))
        {
          object deviceModel = Activator.CreateInstance(selectedType);
          COMContainer.ApplyModelDefaults(deviceModel);
        }

        if (baseClass == typeof(DeviceWithUSB))
        {
          ResolveUsbDevice();
        }
      }
      catch (InvalidOperationException ex)
      {
        MessageBoxCustom.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void ResetSettingsForDeviceModelChange()
    {
      AdditionalSettingsContainer.Content = null;
      _acwPpuDividerCoefficientPercentTextBox = null;
      _dcwPpuDividerCoefficientPercentTextBox = null;
      _systemInsulationResistanceGOhmTextBox = null;

      ConnectionTypeSelectionBox.SelectedIndex = 0;
      IPAddressContainer.Visibility = Visibility.Collapsed;
      COMContainer.Visibility = Visibility.Collapsed;
      USBContainer.Visibility = Visibility.Collapsed;

      IpPart1.Text = string.Empty;
      IpPart2.Text = string.Empty;
      IpPart3.Text = string.Empty;
      IpPart4.Text = string.Empty;

      COMContainer.Reset();

      ResistanceTextBox.Text = string.Empty;
      CapacitanceTextBox.Text = string.Empty;
      RelayPointCountTextBox.Text = string.Empty;

      _usbConnectionDetails = string.Empty;
      USBStatusData.Text = "Ожидание поиска...";
      ClearUsbFields();
    }

    private void ShowFastMeterAdditionalSettings(IMultimeter multimeter)
    {
      _acwPpuDividerCoefficientPercentTextBox = CreatePpuDividerTextBox();
      _dcwPpuDividerCoefficientPercentTextBox = CreatePpuDividerTextBox();

      var container = new Border
      {
        Style = (Style)FindResource("DeviceInputSectionCardStyle")
      };

      var grid = new Grid();
      grid.RowDefinitions.Add(new RowDefinition());
      grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
      grid.RowDefinitions.Add(new RowDefinition());
      grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

      var titleBar = new Border
      {
        Style = (Style)FindResource("DeviceInputSectionTitleBarStyle")
      };

      titleBar.Child = CreateLocalizedAdditionalSettingsTitle("settings.device.multimeter.additionalDividerCoefficient.acw");

      var inputBorder = new Border
      {
        Style = (Style)FindResource("DeviceSettingsUnifiedInputBorderStyle")
      };

      inputBorder.Child = _acwPpuDividerCoefficientPercentTextBox;
      Grid.SetRow(inputBorder, 1);

      var dcwTitleBar = new Border
      {
        Style = (Style)FindResource("DeviceInputSectionTitleBarStyle")
      };

      dcwTitleBar.Child = CreateLocalizedAdditionalSettingsTitle("settings.device.multimeter.additionalDividerCoefficient.dcw");

      var dcwInputBorder = new Border
      {
        Style = (Style)FindResource("DeviceSettingsUnifiedInputBorderStyle")
      };

      dcwInputBorder.Child = _dcwPpuDividerCoefficientPercentTextBox;
      Grid.SetRow(dcwTitleBar, 2);
      Grid.SetRow(dcwInputBorder, 3);

      grid.Children.Add(titleBar);
      grid.Children.Add(inputBorder);
      grid.Children.Add(dcwTitleBar);
      grid.Children.Add(dcwInputBorder);
      container.Child = grid;
      AdditionalSettingsContainer.Content = container;
    }

    private void ShowBreakdownTesterAdditionalSettings()
    {
      _systemInsulationResistanceGOhmTextBox = new TextBox
      {
        Style = (Style)FindResource("DeviceSettingsUnifiedTextBoxStyle"),
        Text = "60",
        MaxLength = 2
      };
      _systemInsulationResistanceGOhmTextBox.PreviewTextInput += IntegerDevice_PreviewTextInput;

      var container = new Border
      {
        Style = (Style)FindResource("DeviceInputSectionCardStyle")
      };
      var grid = new Grid();
      grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
      grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

      var titleBar = new Border
      {
        Style = (Style)FindResource("DeviceInputSectionTitleBarStyle"),
        Height = double.NaN,
        MinHeight = 42,
        Child = CreateLocalizedAdditionalSettingsTitle("settings.device.breakdownTester.systemInsulationResistance")
      };
      var inputBorder = new Border
      {
        Style = (Style)FindResource("DeviceSettingsUnifiedInputBorderStyle"),
        Child = _systemInsulationResistanceGOhmTextBox
      };
      Grid.SetRow(inputBorder, 1);
      grid.Children.Add(titleBar);
      grid.Children.Add(inputBorder);
      container.Child = grid;
      AdditionalSettingsContainer.Content = container;
    }

    private static void IntegerDevice_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      e.Handled = !e.Text.All(char.IsDigit);
    }

    private TextBlock CreateLocalizedAdditionalSettingsTitle(string localizationKey)
    {
      var title = new TextBlock
      {
        Foreground = (System.Windows.Media.Brush)FindResource("ForegrounfBrushes"),
        FontSize = 20,
        FontWeight = FontWeights.Bold,
        Margin = new Thickness(7, 0, 7, 0),
        TextWrapping = TextWrapping.Wrap,
        VerticalAlignment = VerticalAlignment.Center
      };

      title.SetBinding(
        TextBlock.TextProperty,
        new Binding(nameof(LocalizedString.Value))
        {
          Source = new LocalizedString(localizationKey)
        });

      return title;
    }

    private TextBox CreatePpuDividerTextBox()
    {
      var textBox = new TextBox
      {
        Style = (Style)FindResource("DeviceSettingsUnifiedTextBoxStyle"),
        Text = "100"
      };

      textBox.PreviewTextInput += ResistanceDevice_PreviewTextInput;
      textBox.TextChanged += ResistanceDevice_TextChanged;

      return textBox;
    }

    /// <summary>
    /// Обрабатывает изменение типа подключения, настраивая доступные параметры.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события выбора.</param>
    private void ConnectionTypeSelectionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (IPAddressContainer == null || COMContainer == null)
      {
        return;
      }

      if (ConnectionTypeSelectionBox.SelectedItem is ComboBoxItem selectedItem)
      {
        IPAddressContainer.Visibility = Visibility.Collapsed;
        COMContainer.Visibility = Visibility.Collapsed;
        USBContainer.Visibility = Visibility.Collapsed;

        string? selectedType = selectedItem.Content?.ToString()?.ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(selectedType))
        {
          return;
        }

        if (selectedType.Contains("ip"))
        {
          ShowIP();
        }
        else if (selectedType.Contains("com"))
        {
          COMContainer.Visibility = Visibility.Visible;
          COMContainer.LoadAvailablePorts();
        }
        else if (selectedType.Contains("usb"))
        {
          ShowUSB();
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
      SynchronizeDeviceNumberAndIp(DeviceNumberTextBox, IpPart4);
    }

    /// <summary>
    /// Обрабатывает изменение последнего октета IP-адреса.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события изменения текста.</param>
    private void IpPart4_TextChanged(object sender, TextChangedEventArgs e)
    {
      SynchronizeDeviceNumberAndIp(IpPart4, DeviceNumberTextBox);
    }

    /// <summary>
    /// Синхронизирует номер устройства и последний октет IP-адреса.
    /// </summary>
    /// <param name="source">Поле с исходным значением.</param>
    /// <param name="target">Поле для синхронизируемого значения.</param>
    private void SynchronizeDeviceNumberAndIp(TextBox source, TextBox target)
    {
      if (_synchronizingDeviceNumberAndIp ||
          GetBaseDeviceType() != typeof(DeviceWithIP) ||
          IsSelectedChassisManager() ||
          string.Equals(source.Text, target.Text, StringComparison.Ordinal))
      {
        return;
      }

      _synchronizingDeviceNumberAndIp = true;
      try
      {
        target.Text = source.Text;
        target.CaretIndex = target.Text.Length;
      }
      finally
      {
        _synchronizingDeviceNumberAndIp = false;
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
        return;
      }
      else
      {
        return;
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
      if (!ValidateRequiredParameters())
      {
        e.Handled = true;
        return;
      }

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
    /// Проверяет обязательные параметры перед сохранением устройства.
    /// </summary>
    public bool ValidateRequiredParameters()
    {
      ClearValidationHighlights();

      var issues = new List<RequiredParameterIssue>();

      AddIssueIf(
        issues,
        DeviceModelSelectionBox.SelectedItem is not string,
        "Модель устройства",
        DeviceModelContainer,
        DeviceModelSelectionBox);

      AddIssueIf(
        issues,
        DeviceNumberContainer.Visibility == Visibility.Visible && NumberDevice < 0,
        "Номер устройства",
        DeviceNumberContainer,
        DeviceNumberTextBox);

      AddIssueIf(
        issues,
        BusTypeContainer.Visibility == Visibility.Visible && BusTypeSelectionBox.SelectedItem is not SwitchingBusNew,
        "Тип структурной шины",
        BusTypeContainer,
        BusTypeSelectionBox);

      AddIssueIf(
        issues,
        RelayPointCountContainer.Visibility == Visibility.Visible && RelayPointCount <= 0,
        "Количество точек (каналов)",
        RelayPointCountContainer,
        RelayPointCountTextBox);

      AddIssueIf(
        issues,
        ResistanceContainer.Visibility == Visibility.Visible &&
        !DeviceRequiredParameterValidator.IsNonNegativeNumber(ResistanceTextBox.Text),
        "Сопротивление коммутатора",
        ResistanceContainer,
        ResistanceTextBox);

      AddIssueIf(
        issues,
        CapacitanceContainer.Visibility == Visibility.Visible &&
        !DeviceRequiredParameterValidator.IsNonNegativeNumber(CapacitanceTextBox.Text),
        "Емкость коммутатора",
        CapacitanceContainer,
        CapacitanceTextBox);

      string connectionType = GetSelectedConnectionType();
      AddIssueIf(
        issues,
        ConnectionTypeContainer.Visibility == Visibility.Visible && string.IsNullOrWhiteSpace(connectionType),
        "Тип подключения устройства",
        ConnectionTypeContainer,
        ConnectionTypeSelectionBox);

      AddConnectionDetailsIssues(issues, connectionType);
      AddAdditionalSettingsIssues(issues);

      if (issues.Count == 0)
      {
        return true;
      }

      foreach (var issue in issues)
      {
        HighlightInvalidSection(issue.Section);
      }

      ScrollToIssue(issues[0]);
      DeviceConfigNotifications.ShowRequiredParametersMissing(issues.Select(issue => issue.Name));

      return false;
    }

    private void AddConnectionDetailsIssues(List<RequiredParameterIssue> issues, string connectionType)
    {
      if (string.Equals(connectionType, "IP", StringComparison.OrdinalIgnoreCase))
      {
        AddIssueIf(
          issues,
          IPAddressContainer.Visibility == Visibility.Visible && !IsValidIpAddress(),
          "IP Address",
          IPAddressContainer,
          IpPart1);
        return;
      }

      if (string.Equals(connectionType, "COM", StringComparison.OrdinalIgnoreCase))
      {
        bool isInvalid = COMContainer.Visibility == Visibility.Visible &&
          (string.IsNullOrWhiteSpace(COMContainer.PortName) ||
           COMContainer.BaudRate < 0 ||
           COMContainer.DataBits < 0);

        AddIssueIf(
          issues,
          isInvalid,
          "Параметры COM-порта",
          COMContainer,
          COMContainer);
        return;
      }

      if (string.Equals(connectionType, "USB", StringComparison.OrdinalIgnoreCase))
      {
        AddIssueIf(
          issues,
          USBContainer.Visibility == Visibility.Visible && string.IsNullOrWhiteSpace(UsbConnectionDetails),
          "USB устройство",
          USBContainer,
          USBContainer);
      }
    }

    private void AddAdditionalSettingsIssues(List<RequiredParameterIssue> issues)
    {
      AddIssueIf(
        issues,
        _acwPpuDividerCoefficientPercentTextBox != null &&
        !DeviceRequiredParameterValidator.IsPositiveNumber(_acwPpuDividerCoefficientPercentTextBox.Text),
        "Коэффициент делителя ППУ ACW",
        AdditionalSettingsContainer.Content as FrameworkElement ?? AdditionalSettingsContainer,
        _acwPpuDividerCoefficientPercentTextBox);

      AddIssueIf(
        issues,
        _dcwPpuDividerCoefficientPercentTextBox != null &&
        !DeviceRequiredParameterValidator.IsPositiveNumber(_dcwPpuDividerCoefficientPercentTextBox.Text),
        "Коэффициент делителя ППУ DCW",
        AdditionalSettingsContainer.Content as FrameworkElement ?? AdditionalSettingsContainer,
        _dcwPpuDividerCoefficientPercentTextBox);

      AddIssueIf(
        issues,
        _systemInsulationResistanceGOhmTextBox != null &&
        GetSystemInsulationResistanceGOhm() is < 1 or > 60,
        "Сопротивление изоляции системы",
        AdditionalSettingsContainer.Content as FrameworkElement ?? AdditionalSettingsContainer,
        _systemInsulationResistanceGOhmTextBox);
    }

    private string GetSelectedConnectionType()
    {
      if (ConnectionTypeSelectionBox.SelectedItem is not ComboBoxItem item ||
          item.IsEnabled == false)
      {
        return string.Empty;
      }

      return DeviceRequiredParameterValidator.NormalizeConnectionType(
        item.Content?.ToString(),
        item.IsEnabled);
    }

    private bool IsValidIpAddress()
    {
      return DeviceRequiredParameterValidator.IsValidIpAddress(
        IpPart1Value,
        IpPart2Value,
        IpPart3Value,
        IpPart4Value);
    }

    private static void AddIssueIf(
      List<RequiredParameterIssue> issues,
      bool condition,
      string name,
      FrameworkElement section,
      FrameworkElement focusTarget)
    {
      if (condition)
      {
        issues.Add(new RequiredParameterIssue(name, section, focusTarget));
      }
    }

    private void ClearValidationHighlights()
    {
      foreach (Border border in EnumerateVisualChildren<Border>(this))
      {
        if (Equals(border.Tag, "DeviceSettingsValidation"))
        {
          border.ClearValue(Border.BorderBrushProperty);
          border.ClearValue(Border.BorderThicknessProperty);
          border.Tag = null;
        }
      }

      COMContainer.SetValidationHighlight(false);
    }

    private void HighlightInvalidSection(FrameworkElement section)
    {
      if (section == COMContainer)
      {
        COMContainer.SetValidationHighlight(true);
        return;
      }

      if (section is not Border border)
      {
        return;
      }

      border.BorderBrush = GetValidationBrush();
      border.BorderThickness = new Thickness(2);
      border.Tag = "DeviceSettingsValidation";
    }

    private Brush GetValidationBrush()
    {
      return TryFindResource("RedColorSolidColorBrush") as Brush ?? Brushes.IndianRed;
    }

    private void ScrollToIssue(RequiredParameterIssue issue)
    {
      issue.Section.BringIntoView();
      SettingsScrollViewer.UpdateLayout();
      issue.Section.BringIntoView();
      issue.FocusTarget.Focus();
    }

    private static IEnumerable<T> EnumerateVisualChildren<T>(DependencyObject root)
      where T : DependencyObject
    {
      int count = VisualTreeHelper.GetChildrenCount(root);
      for (int index = 0; index < count; index++)
      {
        DependencyObject child = VisualTreeHelper.GetChild(root, index);
        if (child is T typedChild)
        {
          yield return typedChild;
        }

        foreach (T descendant in EnumerateVisualChildren<T>(child))
        {
          yield return descendant;
        }
      }
    }

    private sealed record RequiredParameterIssue(
      string Name,
      FrameworkElement Section,
      FrameworkElement FocusTarget);

  }
}
