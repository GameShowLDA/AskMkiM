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
  Info
}

public sealed class AskMkiSettingOption
{
  public AskMkiSettingOption()
  {
  }

  public AskMkiSettingOption(byte value, string label)
  {
    Value = value;
    Label = label;
  }

  public byte Value { get; set; }

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
  private AskMkiSettingOption? _selectedOption;

  public string Label { get; set; } = string.Empty;

  public string Description { get; set; } = string.Empty;

  public AskMkiSettingEditorKind EditorKind { get; set; }

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

  private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
}