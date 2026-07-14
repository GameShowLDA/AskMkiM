using Ask.Core.Services.Config.LegacyMki;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UI.Controls.Settings.AskMkiConfig;

public enum AskMkiSettingEditorKind
{
  Text,
  Toggle,
  Choice,
  Info,
  UsbDevice,
  IpAddress
}

public sealed class AskMkiSettingOption
{
  public AskMkiSettingOption()
  {
  }

  public AskMkiSettingOption(byte value, string label)
  {
    Value = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    Label = label;
  }

  public string Value { get; set; } = string.Empty;

  public string Label { get; set; } = string.Empty;
}

public sealed class AskMkiFieldDefinition
{
  public string Label { get; set; } = string.Empty;

  public string LabelFormat { get; set; } = string.Empty;

  public string Description { get; set; } = string.Empty;

  public AskMkiSettingEditorKind EditorKind { get; set; }

  public string Path { get; set; } = string.Empty;

  public string PathFormat { get; set; } = string.Empty;

  public int Count { get; set; }

  public int StartIndex { get; set; }

  public string NamesKey { get; set; } = string.Empty;

  public string OptionsKey { get; set; } = string.Empty;
}

public sealed class AskMkiGroupDefinition
{
  public string Code { get; set; } = string.Empty;

  public string Title { get; set; } = string.Empty;

  public string Description { get; set; } = string.Empty;

  public string Summary { get; set; } = string.Empty;

  public string FieldsKey { get; set; } = string.Empty;

  public ObservableCollection<AskMkiGroupDefinition> Children { get; } = new();
}

public sealed class AskMkiSwitchRangeRow : INotifyPropertyChanged
{
  private bool _isPresent;
  private string _firstBk = string.Empty;
  private string _lastBk = string.Empty;

  public string Name { get; set; } = string.Empty;

  public bool IsFixedPresent { get; set; }

  public string FixedDescription { get; set; } = "Всегда";

  public bool IsPresent
  {
    get => _isPresent;
    set
    {
      if (_isPresent == value)
      {
        return;
      }

      _isPresent = value;
      OnPropertyChanged();
    }
  }

  public string FirstBk
  {
    get => _firstBk;
    set
    {
      if (_firstBk == value)
      {
        return;
      }

      _firstBk = value;
      OnPropertyChanged();
    }
  }

  public string LastBk
  {
    get => _lastBk;
    set
    {
      if (_lastBk == value)
      {
        return;
      }

      _lastBk = value;
      OnPropertyChanged();
    }
  }

  public Action<LegacyMkiHardwareProfile, AskMkiSwitchRangeRow> ApplyToProfile { get; set; } = (_, _) => { };

  public event PropertyChangedEventHandler? PropertyChanged;

  private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
}

public sealed class AskMkiSettingGroup : INotifyPropertyChanged
{
  private bool _isExpanded;

  public string Code { get; set; } = string.Empty;

  public string Title { get; set; } = string.Empty;

  public string Description { get; set; } = string.Empty;

  public string Summary { get; set; } = string.Empty;

  public ObservableCollection<AskMkiSettingItem> Items { get; } = new();

  public ObservableCollection<AskMkiSettingGroup> Children { get; } = new();

  public ObservableCollection<AskMkiSwitchRangeRow> SwitchRows { get; } = new();

  public bool IsSwitchMatrixGroup => SwitchRows.Count > 0;

  public bool IsExpanded
  {
    get => _isExpanded;
    set
    {
      if (_isExpanded == value)
      {
        return;
      }

      _isExpanded = value;
      OnPropertyChanged();
    }
  }

  public string ItemCountLabel
  {
    get
    {
      var count = IsSwitchMatrixGroup
        ? SwitchRows.Count
        : Items.Count + Children.Count;

      return count == 0 ? string.Empty : $"{count}";
    }
  }

  public event PropertyChangedEventHandler? PropertyChanged;

  private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
}

public sealed class AskMkiSettingItem : INotifyPropertyChanged
{
  private string? _textValue;
  private bool _toggleValue;
  private bool _isVisible = true;
  private string _usbStatus = string.Empty;
  private string _usbPort = string.Empty;
  private string _usbId = string.Empty;
  private string _usbVid = "N/A";
  private string _usbPid = "N/A";
  private string _ip1 = string.Empty;
  private string _ip2 = string.Empty;
  private string _ip3 = string.Empty;
  private string _ip4 = string.Empty;
  private bool _isSyncingIp;
  private AskMkiSettingOption? _selectedOption;

  public string Label { get; set; } = string.Empty;

  public string Description { get; set; } = string.Empty;

  public string Path { get; set; } = string.Empty;

  public AskMkiSettingEditorKind EditorKind { get; set; }

  public bool IsVisible
  {
    get => _isVisible;
    set
    {
      if (_isVisible == value)
      {
        return;
      }

      _isVisible = value;
      OnPropertyChanged();
    }
  }

  public string? TextValue
  {
    get => _textValue;
    set
    {
      if (_textValue == value)
      {
        return;
      }

      _textValue = value;
      OnPropertyChanged();

      if (!_isSyncingIp && EditorKind == AskMkiSettingEditorKind.IpAddress)
      {
        UpdateIpPartsFromText();
      }
    }
  }

  public string UsbStatus
  {
    get => _usbStatus;
    set => SetField(ref _usbStatus, value);
  }

  public string UsbPort
  {
    get => _usbPort;
    set => SetField(ref _usbPort, value);
  }

  public string UsbId
  {
    get => _usbId;
    set => SetField(ref _usbId, value);
  }

  public string UsbVid
  {
    get => _usbVid;
    set => SetField(ref _usbVid, value);
  }

  public string UsbPid
  {
    get => _usbPid;
    set => SetField(ref _usbPid, value);
  }

  public string Ip1
  {
    get => _ip1;
    set
    {
      if (SetField(ref _ip1, value))
      {
        UpdateIpTextValue();
      }
    }
  }

  public string Ip2
  {
    get => _ip2;
    set
    {
      if (SetField(ref _ip2, value))
      {
        UpdateIpTextValue();
      }
    }
  }

  public string Ip3
  {
    get => _ip3;
    set
    {
      if (SetField(ref _ip3, value))
      {
        UpdateIpTextValue();
      }
    }
  }

  public string Ip4
  {
    get => _ip4;
    set
    {
      if (SetField(ref _ip4, value))
      {
        UpdateIpTextValue();
      }
    }
  }

  public bool ToggleValue
  {
    get => _toggleValue;
    set
    {
      if (_toggleValue == value)
      {
        return;
      }

      _toggleValue = value;
      OnPropertyChanged();
    }
  }

  public ObservableCollection<AskMkiSettingOption> Options { get; set; } = new();

  public AskMkiSettingOption? SelectedOption
  {
    get => _selectedOption;
    set
    {
      if (_selectedOption == value)
      {
        return;
      }

      _selectedOption = value;
      OnPropertyChanged();
    }
  }

  public Action<LegacyMkiHardwareProfile, AskMkiSettingItem> ApplyToProfile { get; set; } = (_, _) => { };

  public event PropertyChangedEventHandler? PropertyChanged;

  public void SetIpTextValue(string? value)
  {
    _textValue = value ?? string.Empty;

    var parts = (_textValue ?? string.Empty).Split('.');
    _ip1 = parts.ElementAtOrDefault(0) ?? string.Empty;
    _ip2 = parts.ElementAtOrDefault(1) ?? string.Empty;
    _ip3 = parts.ElementAtOrDefault(2) ?? string.Empty;
    _ip4 = parts.ElementAtOrDefault(3) ?? string.Empty;

    OnPropertyChanged(nameof(TextValue));
    OnPropertyChanged(nameof(Ip1));
    OnPropertyChanged(nameof(Ip2));
    OnPropertyChanged(nameof(Ip3));
    OnPropertyChanged(nameof(Ip4));
  }

  private bool SetField(ref string field, string? value, [CallerMemberName] string? propertyName = null)
  {
    value ??= string.Empty;
    if (field == value)
    {
      return false;
    }

    field = value;
    OnPropertyChanged(propertyName);
    return true;
  }

  private void UpdateIpTextValue()
  {
    if (_isSyncingIp)
    {
      return;
    }

    _isSyncingIp = true;
    _textValue = $"{Ip1}.{Ip2}.{Ip3}.{Ip4}";
    OnPropertyChanged(nameof(TextValue));
    _isSyncingIp = false;
  }

  private void UpdateIpPartsFromText()
  {
    _isSyncingIp = true;

    var parts = (_textValue ?? string.Empty).Split('.');
    _ip1 = parts.ElementAtOrDefault(0) ?? string.Empty;
    _ip2 = parts.ElementAtOrDefault(1) ?? string.Empty;
    _ip3 = parts.ElementAtOrDefault(2) ?? string.Empty;
    _ip4 = parts.ElementAtOrDefault(3) ?? string.Empty;

    OnPropertyChanged(nameof(Ip1));
    OnPropertyChanged(nameof(Ip2));
    OnPropertyChanged(nameof(Ip3));
    OnPropertyChanged(nameof(Ip4));

    _isSyncingIp = false;
  }

  private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
}
