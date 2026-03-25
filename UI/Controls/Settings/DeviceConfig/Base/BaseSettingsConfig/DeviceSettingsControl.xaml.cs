using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Device.Communication.Com;
using Ask.Device.Communication.Com.Configuration;
using Ask.Device.Communication.Ethernet;
using Ask.Device.Communication.Usb;
using Ask.Device.Runtime.Base.Device;
using Ask.Device.Runtime.Device;
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
  /// Interaction logic for DeviceSettingsControl.xaml.
  /// </summary>
  public partial class DeviceSettingsControl : UserControl
  {
    private static readonly Regex VidPidRegex = new(@"VID_([0-9A-F]{4})&PID_([0-9A-F]{4})", RegexOptions.IgnoreCase);
    private string _usbConnectionDetails = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceSettingsControl"/> class.
    /// </summary>
    public DeviceSettingsControl()
    {
      InitializeComponent();
      VisibilityElements();
    }

    /// <summary>
    /// Sets selected chassis manager.
    /// </summary>
    public void SetHeadUnit<T>(T headUnit) where T : class, IHeadUnit
    {
      _headUnit = headUnit;
      BusTypeLoaded();
    }

    /// <summary>
    /// Loads available runtime device models for the interface.
    /// </summary>
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
    /// Resets initial visibility of form blocks.
    /// </summary>
    private void VisibilityElements()
    {
      DeviceNumberContainer.Visibility = Visibility.Visible;
      BusTypeContainer.Visibility = Visibility.Collapsed;
      ResistanceContainer.Visibility = Visibility.Collapsed;
      CapacitanceContainer.Visibility = Visibility.Collapsed;
      ConnectionTypeContainer.Visibility = Visibility.Visible;
      IPAddressContainer.Visibility = Visibility.Collapsed;
      COMContainer.Visibility = Visibility.Collapsed;
      USBContainer.Visibility = Visibility.Collapsed;
      AdditionalSettingsContainer.Visibility = Visibility.Visible;
      USBStatusData.Text = "Ожидание поиска...";
      ClearUsbFields();
    }

    /// <summary>
    /// Returns base transport class used by selected runtime model.
    /// </summary>
    private Type GetBaseDeviceType(Type selectedType)
    {
      bool inheritsIP = typeof(DeviceWithIP).IsAssignableFrom(selectedType);
      bool inheritsCOM = typeof(DeviceWithCOM).IsAssignableFrom(selectedType);
      bool inheritsUSB = typeof(DeviceWithUSB).IsAssignableFrom(selectedType);

      int count = (inheritsIP ? 1 : 0) + (inheritsCOM ? 1 : 0) + (inheritsUSB ? 1 : 0);
      if (count != 1)
      {
        throw new InvalidOperationException($"Ошибка: Класс {selectedType.Name} должен наследовать ровно один базовый тип подключения.");
      }

      if (inheritsIP)
      {
        return typeof(DeviceWithIP);
      }

      if (inheritsCOM)
      {
        return typeof(DeviceWithCOM);
      }

      return typeof(DeviceWithUSB);
    }

    /// <summary>
    /// Returns base transport class for selected model.
    /// </summary>
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
    /// Shows IP input block and fills defaults.
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
    /// Shows USB block and auto-resolves matching device by model name/pattern.
    /// </summary>
    private void ShowUSB(string? preferredPattern = null)
    {
      USBContainer.Visibility = Visibility.Visible;
      ResolveUsbDevice(preferredPattern);
    }

    /// <summary>
    /// Creates selected runtime model instance.
    /// </summary>
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
    /// Loads COM ports into selector.
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
    /// Reads VID/PID for selected COM port.
    /// </summary>
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

        Match match = VidPidRegex.Match(deviceId);
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
    /// Applies COM defaults from runtime model.
    /// </summary>
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
    /// Selects combo item by property value.
    /// </summary>
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
    /// Disables changing connection type with mouse wheel.
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
        CapacitanceTextBox.Text = relayDevice.SwitchCapacitance.ToString(System.Globalization.CultureInfo.InvariantCulture);
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

      var baseType = GetBaseDeviceType();

      if (baseType == typeof(DeviceWithUSB))
      {
        SelectConnectionType("USB");
        ShowUSB(connectionDetails);
        return;
      }

      if (IPAddress.TryParse(connectionDetails, out var ip))
      {
        SelectConnectionType("IP");
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

      SelectConnectionType("COM");
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

    private bool SelectConnectionType(string content)
    {
      foreach (var item in ConnectionTypeSelectionBox.Items.OfType<ComboBoxItem>())
      {
        if (string.Equals(item.Content?.ToString(), content, StringComparison.OrdinalIgnoreCase))
        {
          ConnectionTypeSelectionBox.SelectedItem = item;
          return true;
        }
      }

      return false;
    }

    private void ResolveUsbDevice(string? preferredPattern = null)
    {
      var patterns = ResolveUsbSearchPatterns(preferredPattern);
      if (patterns.Count == 0)
      {
        USBStatusData.Text = "Не задан шаблон поиска USB.";
        ClearUsbFields();
        _usbConnectionDetails = string.Empty;
        return;
      }

      string pattern = patterns[0];

      try
      {
        var match = TryFindUsbDevice(patterns, out string resolvedPattern);
        if (match == null)
        {
          USBStatusData.Text = $"USB не найден: {pattern}";
          ClearUsbFields();
          _usbConnectionDetails = resolvedPattern;
          return;
        }

        USBPortData.Text = match.PortDisplay;
        USBDeviceIdData.Text = match.DeviceId;
        USBVIDData.Text = match.Vid;
        USBPIDData.Text = match.Pid;
        USBStatusData.Text = $"Найдено: USB порт {match.PortDisplay}";
        _usbConnectionDetails = BuildUsbConnectionPattern(match);
      }
      catch (Exception ex)
      {
        USBStatusData.Text = $"Ошибка поиска USB: {ex.Message}";
        ClearUsbFields();
        _usbConnectionDetails = patterns[0];
      }
    }

    private List<string> ResolveUsbSearchPatterns(string? preferredPattern = null)
    {
      var patterns = new List<string>();

      AddUsbSearchPattern(patterns, preferredPattern);

      if (DeviceModelSelectionBox.SelectedItem is not string selectedModel ||
          !DeviceModelMap.TryGetValue(selectedModel, out var selectedType))
      {
        return patterns;
      }

      object? instance = Activator.CreateInstance(selectedType);
      string? details = instance?.GetType().GetProperty("ConnectionDetails")?.GetValue(instance)?.ToString();
      string? name = instance?.GetType().GetProperty("Name")?.GetValue(instance)?.ToString();

      AddUsbSearchPattern(patterns, details);
      AddUsbSearchPattern(patterns, name);
      AddUsbSearchPattern(patterns, selectedModel);

      return patterns;
    }

    private UsbDeviceMatch? TryFindUsbDevice(string pattern)
    {
      const string query = "SELECT Name, DeviceID, PNPDeviceID, Service FROM Win32_PnPEntity WHERE PNPDeviceID LIKE 'USB%' OR PNPDeviceID LIKE 'HID%'";

      using var searcher = new ManagementObjectSearcher(query);
      UsbDeviceMatch? bestMatch = null;
      int bestScore = int.MinValue;
      foreach (ManagementObject item in searcher.Get())
      {
        string name = item["Name"]?.ToString() ?? string.Empty;
        string deviceId = item["DeviceID"]?.ToString() ?? string.Empty;
        string pnpDeviceId = item["PNPDeviceID"]?.ToString() ?? string.Empty;
        string service = item["Service"]?.ToString() ?? string.Empty;

        bool isMatch =
          name.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0 ||
          deviceId.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0 ||
          pnpDeviceId.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0;

        if (!isMatch)
        {
          continue;
        }

        int score = GetUsbMatchScore(deviceId, pnpDeviceId, service);
        if (score <= bestScore)
        {
          continue;
        }

        string vid = "N/A";
        string pid = "N/A";
        var vidPidSource = string.IsNullOrWhiteSpace(pnpDeviceId) ? deviceId : pnpDeviceId;
        Match match = VidPidRegex.Match(vidPidSource);
        if (match.Success)
        {
          vid = match.Groups[1].Value;
          pid = match.Groups[2].Value;
        }

        bestScore = score;
        bestMatch = new UsbDeviceMatch(BuildUsbPortDisplay(deviceId, pnpDeviceId), deviceId, pnpDeviceId, vid, pid);
      }

      return bestMatch;
    }

    private void ClearUsbFields()
    {
      USBPortData.Text = string.Empty;
      USBDeviceIdData.Text = string.Empty;
      USBVIDData.Text = "N/A";
      USBPIDData.Text = "N/A";
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

    private UsbDeviceMatch? TryFindUsbDevice(IEnumerable<string> patterns, out string resolvedPattern)
    {
      foreach (string pattern in patterns)
      {
        var match = TryFindUsbDevice(pattern);
        if (match != null)
        {
          resolvedPattern = pattern;
          return match;
        }
      }

      resolvedPattern = patterns.FirstOrDefault() ?? string.Empty;
      return null;
    }

    private static void AddUsbSearchPattern(List<string> patterns, string? pattern)
    {
      if (string.IsNullOrWhiteSpace(pattern))
      {
        return;
      }

      if (patterns.Any(existing => string.Equals(existing, pattern, StringComparison.OrdinalIgnoreCase)))
      {
        return;
      }

      patterns.Add(pattern);
    }

    private static int GetUsbMatchScore(string deviceId, string pnpDeviceId, string service)
    {
      int score = 0;

      if (pnpDeviceId.StartsWith("USB\\VID_", StringComparison.OrdinalIgnoreCase))
      {
        score += 100;
      }
      else if (deviceId.StartsWith("USB\\VID_", StringComparison.OrdinalIgnoreCase))
      {
        score += 90;
      }
      else if (pnpDeviceId.StartsWith("HID\\VID_", StringComparison.OrdinalIgnoreCase))
      {
        score += 80;
      }

      if (string.Equals(service, "HidUsb", StringComparison.OrdinalIgnoreCase))
      {
        score += 20;
      }

      return score;
    }

    private static string BuildUsbPortDisplay(string deviceId, string pnpDeviceId)
    {
      string source = !string.IsNullOrWhiteSpace(deviceId) ? deviceId : pnpDeviceId;
      if (string.IsNullOrWhiteSpace(source))
      {
        return "-";
      }

      Match portMatch = Regex.Match(source, @"&0&(\d+)$", RegexOptions.IgnoreCase);
      if (portMatch.Success)
      {
        return int.Parse(portMatch.Groups[1].Value).ToString();
      }

      return "-";
    }

    private static string BuildUsbConnectionPattern(UsbDeviceMatch match)
    {
      if (!string.IsNullOrWhiteSpace(match.Vid) &&
          !string.IsNullOrWhiteSpace(match.Pid) &&
          !string.Equals(match.Vid, "N/A", StringComparison.OrdinalIgnoreCase) &&
          !string.Equals(match.Pid, "N/A", StringComparison.OrdinalIgnoreCase))
      {
        return $"VID_{match.Vid.ToUpperInvariant()}&PID_{match.Pid.ToUpperInvariant()}";
      }

      if (!string.IsNullOrWhiteSpace(match.PnpDeviceId))
      {
        return match.PnpDeviceId;
      }

      if (!string.IsNullOrWhiteSpace(match.DeviceId))
      {
        return match.DeviceId;
      }

      return match.PortDisplay;
    }

    private sealed record UsbDeviceMatch(string PortDisplay, string DeviceId, string PnpDeviceId, string Vid, string Pid);
  }
}
