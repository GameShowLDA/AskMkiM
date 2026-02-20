using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using NewCore.Base.Device;
using NewCore.Device;
using NewCore.Base;
using System.IO.Ports;
using System.Management;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace UI.Controls.Settings.DeviceConfig.Base.BaseSettingsConfig
{
  /// <summary>
  /// Логика взаимодействия для DeviceSettingsControl.xaml.
  /// </summary>
  public partial class DeviceSettingsControl : UserControl
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DeviceSettingsControl"/>.
    /// </summary>
    public DeviceSettingsControl()
    {
      InitializeComponent();
      VisibilityElements();
    }

    /// <summary>
    /// Устанавливает головное устройство.
    /// </summary>
    /// <typeparam name="T">Тип головного устройства.</typeparam>
    /// <param name="headUnit">Экземпляр головного устройства.</param>
    public void SetHeadUnit<T>(T headUnit) where T : class, IHeadUnit
    {
      _headUnit = headUnit;
      BusTypeLoaded();
    }

    /// <summary>
    /// Загружает доступные модели устройств.
    /// </summary>
    /// <typeparam name="T">Тип модели устройства.</typeparam>
    public void LoadDeviceModels<T>() where T : class
    {
      var models = ReflectionHelper.GetAllImplementations<T>();

      var deviceModelMap = models
          .Select(t => Activator.CreateInstance(t) as T)
          .Where(instance => instance != null)
          .ToDictionary(
              instance => instance.GetType().GetProperty("Name")?.GetValue(instance)?.ToString(),
              instance => instance.GetType());

      DeviceModelMap = deviceModelMap;
      DeviceModelSelectionBox.ItemsSource = deviceModelMap.Keys;
    }
    private void BusTypeLoaded()
    {
      var values = Enum.GetValues(typeof(SwitchingBusNew)).Cast<SwitchingBusNew>().ToList();

      switch (_headUnit.BusType)
      {
        case BusStructureEnum.Type.Bus2:
          values.Remove(SwitchingBusNew.AB2);
          values.Remove(SwitchingBusNew.AB3);
          values.Remove(SwitchingBusNew.AB4);
          break;

        case BusStructureEnum.Type.Bus4:
          values.Remove(SwitchingBusNew.AB3);
          values.Remove(SwitchingBusNew.AB4);
          break;

        case BusStructureEnum.Type.Bus6:
          values.Remove(SwitchingBusNew.AB4);
          break;
      }

      BusTypeSelectionBox.ItemsSource = values;
      BusTypeSelectionBox.SelectedIndex = 0;
    }

    /// <summary>
    /// Скрывает элементы интерфейса.
    /// </summary>
    private void VisibilityElements()
    {
      DeviceNumberContainer.Visibility = Visibility.Visible;
      BusTypeContainer.Visibility = Visibility.Collapsed;
      ResistanceContainer.Visibility = Visibility.Collapsed;
      ConnectionTypeContainer.Visibility = Visibility.Visible;
      IPAddressContainer.Visibility = Visibility.Collapsed;
      COMContainer.Visibility = Visibility.Collapsed;
      AdditionalSettingsContainer.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Определяет базовый класс для указанного типа устройства.
    /// </summary>
    /// <param name="selectedType">Тип устройства.</param>
    /// <returns>Тип базового класса устройства.</returns>
    /// <exception cref="InvalidOperationException">
    /// Выбрасывается, если класс наследует оба базовых класса или ни один из них.
    /// </exception>
    private Type GetBaseDeviceType(Type selectedType)
    {
      bool inheritsIP = typeof(DeviceWithIP).IsAssignableFrom(selectedType);
      bool inheritsCOM = typeof(DeviceWithCOM).IsAssignableFrom(selectedType);

      return (inheritsIP, inheritsCOM) switch
      {
        (true, true) => throw new InvalidOperationException($"Ошибка: Класс {selectedType.Name} наследует оба базовых класса."),
        (true, false) => typeof(DeviceWithIP),
        (false, true) => typeof(DeviceWithCOM),
        _ => throw new InvalidOperationException($"Ошибка: Класс {selectedType.Name} не наследует ни один из поддерживаемых классов."),
      };
    }

    /// <summary>
    /// Определяет базовый тип устройства из выпадающего списка.
    /// </summary>
    /// <returns>Тип базового класса устройства.</returns>
    public Type GetBaseDeviceType()
    {
      if (DeviceModelSelectionBox.SelectedItem is not string selectedModel ||
          !DeviceModelMap.TryGetValue(selectedModel, out Type selectedType))
      {
        return null;
      }

      return GetBaseDeviceType(selectedType);
    }

    /// <summary>
    /// Настраивает отображение IP-адреса.
    /// </summary>
    private void ShowIP()
    {
      IPAddressContainer.Visibility = Visibility.Visible;
      IpPart1.Text = "192";
      IpPart2.Text = "168";

      IpPart3.Text = _headUnit == null ? DeviceNumberTextBox.Text : _headUnit.Number.ToString();
      IpPart4.Text = DeviceNumberTextBox.Text;
    }

    /// <summary>
    /// Создает экземпляр выбранного пользователем устройства.
    /// </summary>
    /// <returns>Экземпляр выбранного устройства.</returns>
    /// <exception cref="InvalidOperationException">Если модель устройства не выбрана.</exception>
    public object CreateSelectedDeviceInstance()
    {
      if (DeviceModelSelectionBox.SelectedItem == null)
      {
        throw new InvalidOperationException("Не выбрана модель устройства!");
      }

      Type selectedType = DeviceModelMap[DeviceModelSelectionBox.SelectedItem.ToString()];
      return Activator.CreateInstance(selectedType);
    }

    /// <summary>
    /// Заполняет список доступных COM-портов.
    /// </summary>
    private void PopulateCOMPorts()
    {
      string[] portNames = SerialPort.GetPortNames();
      COMPortSelectionBox.ItemsSource = portNames;

      if (portNames.Any())
      {
        COMPortSelectionBox.SelectedIndex = 0;
      }
    }

    /// <summary>
    /// Получает значения VID и PID для указанного COM-порта.
    /// </summary>
    /// <param name="comPort">Имя COM-порта.</param>
    private void GetVidPidForPort(string comPort)
    {
      string query = $"SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%({comPort})%'";

      using var searcher = new ManagementObjectSearcher(query);
      foreach (ManagementObject device in searcher.Get())
      {
        string deviceId = device["DeviceID"] as string;
        if (string.IsNullOrEmpty(deviceId))
        {
          continue;
        }

        Regex regex = new(@"VID_([0-9A-F]{4})&PID_([0-9A-F]{4})", RegexOptions.IgnoreCase);
        Match match = regex.Match(deviceId);
        if (match.Success)
        {
          VIDData.Text = match.Groups[1].Value;
          PIDData.Text = match.Groups[2].Value;
          return;
        }
      }

      VIDData.Text = "N/A";
      PIDData.Text = "N/A";
    }

    /// <summary>
    /// Применяет настройки COM-порта из модели устройства.
    /// </summary>
    /// <param name="deviceModel">Экземпляр модели устройства.</param>
    private void ApplyCOMSettingsFromModel(object deviceModel)
    {
      Type modelType = deviceModel.GetType();

      SetComboBoxValueFromProperty(modelType, deviceModel, "BaudRate", BaudRateSelectionBox);
      SetComboBoxValueFromProperty(modelType, deviceModel, "StopBits", StopBitsSelectionBox);
      SetComboBoxValueFromProperty(modelType, deviceModel, "DataBits", DataBitsSelectionBox);
      SetComboBoxValueFromProperty(modelType, deviceModel, "Parity", ParitySelectionBox);
      SetComboBoxValueFromProperty(modelType, deviceModel, "FlowControl", FlowControlSelectionBox);
    }

    /// <summary>
    /// Устанавливает значение ComboBox из свойства модели устройства.
    /// </summary>
    /// <param name="modelType">Тип модели устройства.</param>
    /// <param name="deviceModel">Экземпляр модели устройства.</param>
    /// <param name="propertyName">Имя свойства.</param>
    /// <param name="comboBox">ComboBox для установки значения.</param>
    private void SetComboBoxValueFromProperty(Type modelType, object deviceModel, string propertyName, ComboBox comboBox)
    {
      var property = modelType.GetProperty(propertyName);
      if (property == null)
      {
        return;
      }

      var valueObj = property.GetValue(deviceModel);
      if (valueObj == null)
      {
        return;
      }

      string value = valueObj.ToString();
      foreach (var item in comboBox.Items)
      {
        string itemContent = item is ComboBoxItem cbItem ? cbItem.Content.ToString() : item.ToString();
        if (string.Equals(itemContent, value, StringComparison.OrdinalIgnoreCase))
        {
          comboBox.SelectedItem = item;
          return;
        }
      }
    }

    /// <summary>
    /// Блокирует выбор с помощью колесика мышки.
    /// </summary>
    private void ConnectionTypeSelectionBox_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
      e.Handled = true;
    }

    public void LoadFromDevice(IDevice device)
    {
      if (device == null)
      {
        return;
      }

      DeviceNumberTextBox.Text = device.Number.ToString();

      var model = DeviceModelMap
        .FirstOrDefault(x => string.Equals(x.Value.FullName, device.DeviceClass, StringComparison.Ordinal));

      if (!string.IsNullOrWhiteSpace(model.Key))
      {
        DeviceModelSelectionBox.SelectedItem = model.Key;
      }

      if (device is Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule.IRelaySwitchModule relayDevice)
      {
        ResistanceTextBox.Text = relayDevice.SwitchResistance.ToString(System.Globalization.CultureInfo.InvariantCulture);
        BusTypeSelectionBox.SelectedItem = relayDevice.BusType;
      }

      ApplyConnectionDetails(device.ConnectionDetails);
    }

    private void ApplyConnectionDetails(string connectionDetails)
    {
      if (string.IsNullOrWhiteSpace(connectionDetails))
      {
        return;
      }

      if (IPAddress.TryParse(connectionDetails, out var ip))
      {
        ConnectionTypeSelectionBox.SelectedIndex = 1; // IP
        string[] parts = ip.ToString().Split('.');
        if (parts.Length == 4)
        {
          IpPart1.Text = parts[0];
          IpPart2.Text = parts[1];
          IpPart3.Text = parts[2];
          IpPart4.Text = parts[3];
        }

        return;
      }

      var serial = SerialPortCustom.ToObject(connectionDetails);
      if (serial == null)
      {
        return;
      }

      ConnectionTypeSelectionBox.SelectedIndex = 2; // COM
      PopulateCOMPorts();

      if (COMPortSelectionBox.ItemsSource is IEnumerable<string> ports && !ports.Contains(serial.PortName))
      {
        var allPorts = ports.ToList();
        allPorts.Add(serial.PortName);
        COMPortSelectionBox.ItemsSource = allPorts;
      }

      COMPortSelectionBox.SelectedItem = serial.PortName;
      SetComboBoxByText(BaudRateSelectionBox, serial.BaudRate.ToString());
      SetComboBoxByText(DataBitsSelectionBox, serial.DataBits.ToString());
      string stopBitsText = serial.StopBits switch
      {
        StopBits.One => "1",
        StopBits.OnePointFive => "1.5",
        StopBits.Two => "2",
        _ => "1",
      };
      SetComboBoxByText(StopBitsSelectionBox, stopBitsText);

      string parityText = serial.Parity switch
      {
        Parity.Even => "Чет",
        Parity.Odd => "Нечет",
        Parity.Mark => "Маркер",
        Parity.Space => "Пробел",
        _ => "Нет",
      };
      SetComboBoxByText(ParitySelectionBox, parityText);
    }

    private static void SetComboBoxByText(ComboBox comboBox, string text)
    {
      foreach (var item in comboBox.Items)
      {
        string itemContent = item is ComboBoxItem cbItem ? cbItem.Content?.ToString() ?? string.Empty : item?.ToString() ?? string.Empty;
        if (string.Equals(itemContent, text, StringComparison.OrdinalIgnoreCase))
        {
          comboBox.SelectedItem = item;
          return;
        }
      }
    }
  }
}
