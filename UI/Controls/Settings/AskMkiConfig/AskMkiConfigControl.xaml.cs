using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.LegacyMki;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static Ask.LogLib.LoggerUtility;

namespace UI.Controls.Settings.AskMkiConfig;

/// <summary>
/// Контрол настроек конфигурации АСК-МКИ.
/// </summary>
public partial class AskMkiConfigControl : UserControl
{
  private bool _isLoadingEditor;
  private bool _hasUnsavedChanges;
  private LegacyMkiHardwareConfigFile? _loadedConfigFile;
  private ObservableCollection<AskMkiSettingGroup> _editorGroups = new();
  private LegacyMkiHardwareProfile? _currentProfile;
  private LegacyMkiProfileKind _selectedProfileKind;

  public AskMkiConfigControl()
  {
    InitializeComponent();

    Loaded += AskMkiConfigControl_Loaded;
    SizeChanged += AskMkiConfigControl_SizeChanged;
    IsVisibleChanged += AskMkiConfigControl_IsVisibleChanged;

    _selectedProfileKind = LegacyMkiConfig.GetSelectedProfile();
    LoadStoredPaths();

    CollapseAskMkiConfigBlock();
  }

  private string UiString(string resourceKey)
  {
    return FindResource(resourceKey) as string
      ?? throw new InvalidOperationException($"String resource '{resourceKey}' was not found.");
  }

  private string UiFormat(string resourceKey, params object[] args)
  {
    return string.Format(CultureInfo.CurrentCulture, UiString(resourceKey), args);
  }

  private void UpdateProfileGroupsScrollHeight()
  {
    if (ProfileGroupsScrollViewer == null)
    {
      return;
    }

    var window = Window.GetWindow(this);
    if (window == null || window.ActualHeight <= 0)
    {
      return;
    }

    try
    {
      var point = ProfileGroupsScrollViewer.TransformToAncestor(window).Transform(new Point(0, 0));
      var availableHeight = window.ActualHeight - point.Y - 35;

      ProfileGroupsScrollViewer.MaxHeight = Math.Max(160, availableHeight);
    }
    catch (Exception ex)
    {
      LogException(ex, customMessage: "AskMkiConfigControl.UpdateProfileGroupsScrollHeight");
      ProfileGroupsScrollViewer.MaxHeight = 600;
    }
  }

  private void AskMkiConfigControl_Loaded(object sender, RoutedEventArgs e)
  {
    CollapseAskMkiConfigBlock();
    UpdateProfileGroupsScrollHeight();
  }

  private void AskMkiConfigControl_SizeChanged(object sender, SizeChangedEventArgs e)
  {
    UpdateProfileGroupsScrollHeight();
  }

  private void AskMkiConfigControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
  {
    if (e.NewValue is true)
    {
      CollapseAskMkiConfigBlock();
      UpdateProfileGroupsScrollHeight();
    }
  }

  private void CollapseAskMkiConfigBlock()
  {
    ToggleAskMkiConfigButton.IsArrowUp = true;
  }
  private void LoadStoredPaths()
  {
    MkiPathTextBox.Text = LegacyMkiConfig.GetMkiPath();
    ConfigPathTextBox.Text = LegacyMkiConfig.GetConfigPath();
    UpdateClearButtonState();
    TryLoadConfigIntoEditor();
  }

  private void SetUnsavedChanges(bool hasChanges)
  {
    _hasUnsavedChanges = hasChanges;

    if (SuccessAskMkiConfig != null)
    {
      SuccessAskMkiConfig.Visibility = hasChanges ? Visibility.Visible : Visibility.Collapsed;
    }

    if (ErrorAskMkiConfig != null)
    {
      ErrorAskMkiConfig.Visibility = hasChanges ? Visibility.Visible : Visibility.Collapsed;
    }
  }

  private void SubscribeEditorChanges(IEnumerable<AskMkiSettingGroup> groups)
  {
    foreach (var item in EnumerateItems(groups))
    {
      item.PropertyChanged += EditorItem_PropertyChanged;
    }

    foreach (var group in groups)
    {
      foreach (var row in group.SwitchRows)
      {
        row.PropertyChanged += SwitchRow_PropertyChanged;
      }
    }
  }

  private void SwitchRow_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
  {
    if (_isLoadingEditor)
    {
      return;
    }

    if (e.PropertyName is nameof(AskMkiSwitchRangeRow.IsPresent)
        or nameof(AskMkiSwitchRangeRow.FirstBk)
        or nameof(AskMkiSwitchRangeRow.LastBk))
    {
      SetUnsavedChanges(true);
    }
  }

  private void EditorItem_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
  {
    if (_isLoadingEditor)
    {
      return;
    }

    if (e.PropertyName is nameof(AskMkiSettingItem.TextValue)
        or nameof(AskMkiSettingItem.ToggleValue)
        or nameof(AskMkiSettingItem.SelectedOption))
    {
      SetUnsavedChanges(true);
    }
  }

  private void SelectMkiPathButton_Click(object sender, RoutedEventArgs e)
  {
    var dialog = new OpenFileDialog
    {
      Title = UiString("AskMki.Dialog.SelectMki.Title"),
      Filter = UiString("AskMki.Dialog.SelectMki.Filter"),
      CheckFileExists = true,
      Multiselect = false
    };

    var currentPath = LegacyMkiConfig.GetMkiPath();
    var currentDirectory = string.IsNullOrWhiteSpace(currentPath)
      ? null
      : Path.GetDirectoryName(currentPath);

    if (!string.IsNullOrWhiteSpace(currentDirectory) && Directory.Exists(currentDirectory))
    {
      dialog.InitialDirectory = currentDirectory;
    }

    if (dialog.ShowDialog() != true)
    {
      return;
    }

    LegacyMkiConfig.SetMkiPath(dialog.FileName);
    MkiPathTextBox.Text = dialog.FileName;

    if (string.IsNullOrWhiteSpace(ConfigPathTextBox.Text))
    {
      ConfigPathTextBox.Text = LegacyMkiConfig.GetConfigPath();
    }

    UpdateClearButtonState();
    TryLoadConfigIntoEditor();
  }

  private void ClearMkiPathButton_Click(object sender, RoutedEventArgs e)
  {
    LegacyMkiConfig.ClearMkiPath();
    MkiPathTextBox.Text = string.Empty;
    UpdateClearButtonState();
  }

  private void SelectConfigPathButton_Click(object sender, RoutedEventArgs e)
  {
    var dialog = new OpenFileDialog
    {
      Title = UiString("AskMki.Dialog.SelectCfg.Title"),
      Filter = UiString("AskMki.Dialog.SelectCfg.Filter"),
      CheckFileExists = true,
      Multiselect = false
    };

    var currentPath = ConfigPathTextBox.Text;
    var currentDirectory = string.IsNullOrWhiteSpace(currentPath)
      ? null
      : Path.GetDirectoryName(currentPath);

    if (!string.IsNullOrWhiteSpace(currentDirectory) && Directory.Exists(currentDirectory))
    {
      dialog.InitialDirectory = currentDirectory;
    }

    if (dialog.ShowDialog() != true)
    {
      return;
    }

    ConfigPathTextBox.Text = dialog.FileName;
    LegacyMkiConfig.SetConfigPath(dialog.FileName);
    TryLoadConfigIntoEditor();
  }

  private void LoadConfigButton_Click(object sender, RoutedEventArgs e)
  {
    TryLoadConfigIntoEditor(showSuccessMessage: true);
  }

  private void ReloadEditorButton_Click(object sender, RoutedEventArgs e)
  {
    LoadSelectedProfileIntoEditor();
  }

  private void ResetEditorButton_Click(object sender, RoutedEventArgs e)
  {
    LoadSelectedProfileIntoEditor();
  }

  private void SaveConfigIcon_PreviewMouseDown(object sender, MouseButtonEventArgs e)
  {
    SaveConfigButton_Click(sender, e);
    e.Handled = true;
  }

  private void CancelConfigIcon_PreviewMouseDown(object sender, MouseButtonEventArgs e)
  {
    CancelConfigButton_Click(sender, e);
    e.Handled = true;
  }

  private void SaveConfigButton_Click(object sender, RoutedEventArgs e)
  {
    try
    {
      var configPath = ResolveAndPersistPaths();
      if (string.IsNullOrWhiteSpace(configPath))
      {
        MessageBox.Show(UiString("AskMki.Message.Warning.ConfigPathRequired"), UiString("AskMki.Message.Title"), MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }

      if (_loadedConfigFile == null)
      {
        if (!File.Exists(configPath))
        {
          MessageBox.Show(UiString("AskMki.Message.Warning.ConfigFileMissing"), UiString("AskMki.Message.Title"), MessageBoxButton.OK, MessageBoxImage.Warning);
          return;
        }

        _loadedConfigFile = LegacyMkiHardwareConfigFileService.Load(configPath);
        _selectedProfileKind = ResolveProfileKind(_loadedConfigFile);
      }

      var profile = _loadedConfigFile.GetProfile(_selectedProfileKind);
      ApplyEditorGroupsToProfile(profile);

      profile.HardwareConfig.Nas = (byte)_selectedProfileKind;
      _loadedConfigFile.SetProfile(_selectedProfileKind, profile);
      _loadedConfigFile.ActiveProfileIndex = (byte)_selectedProfileKind;

      LegacyMkiConfig.SetSelectedProfile(_selectedProfileKind);
      LegacyMkiHardwareConfigFileService.Save(configPath, _loadedConfigFile);

      LoadSelectedProfileIntoEditor();

      Message.MessageBoxCustom.Show(
        "конфигурация оборудования сохранена, перезапустите АСК-МКИ, если была запущена",
        UiString("AskMki.Message.Title"),
        MessageBoxButton.OK,
        MessageBoxImage.Information);
    }
    catch (Exception ex)
    {
      LogException(ex, customMessage: "AskMkiConfigControl.SaveConfigButton_Click");
      MessageBox.Show(ex.Message, UiString("AskMki.Message.SaveErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
    }
  }

  private void CancelConfigButton_Click(object sender, RoutedEventArgs e)
  {
    LoadSelectedProfileIntoEditor();
  }

  private void TryLoadConfigIntoEditor(bool showSuccessMessage = false)
  {
    try
    {
      var configPath = ResolveAndPersistPaths();
      if (string.IsNullOrWhiteSpace(configPath) || !File.Exists(configPath))
      {
        _loadedConfigFile = null;
        _currentProfile = null;
        _editorGroups = new ObservableCollection<AskMkiSettingGroup>();
        ProfileGroupsItemsControl.ItemsSource = _editorGroups;
        SetUnsavedChanges(false);
        return;
      }

      _loadedConfigFile = LegacyMkiHardwareConfigFileService.Load(configPath);
      _selectedProfileKind = ResolveProfileKind(_loadedConfigFile);
      LegacyMkiConfig.SetSelectedProfile(_selectedProfileKind);

      LoadSelectedProfileIntoEditor();

      if (showSuccessMessage)
      {
        MessageBox.Show(UiFormat("AskMki.Message.LoadSuccess", Environment.NewLine, configPath), UiString("AskMki.Message.Title"), MessageBoxButton.OK, MessageBoxImage.Information);
      }
    }
    catch (Exception ex)
    {
      _loadedConfigFile = null;
      _currentProfile = null;
      _editorGroups = new ObservableCollection<AskMkiSettingGroup>();
      ProfileGroupsItemsControl.ItemsSource = _editorGroups;
      SetUnsavedChanges(false);
      LogException(ex, customMessage: "AskMkiConfigControl.TryLoadConfigIntoEditor");
      MessageBox.Show(ex.Message, UiString("AskMki.Message.LoadErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
    }
  }

  private LegacyMkiProfileKind ResolveProfileKind(LegacyMkiHardwareConfigFile configFile)
  {
    var activeProfileIndex = Convert.ToByte(configFile.ActiveProfileIndex, CultureInfo.InvariantCulture);

    foreach (LegacyMkiProfileKind profileKind in Enum.GetValues(typeof(LegacyMkiProfileKind)))
    {
      if (Convert.ToByte(profileKind, CultureInfo.InvariantCulture) == activeProfileIndex)
      {
        return profileKind;
      }
    }

    return LegacyMkiConfig.GetSelectedProfile();
  }

  private void LoadSelectedProfileIntoEditor()
  {
    _isLoadingEditor = true;

    try
    {
      if (_loadedConfigFile == null)
      {
        _currentProfile = null;
        _editorGroups = new ObservableCollection<AskMkiSettingGroup>();
        ProfileGroupsItemsControl.ItemsSource = _editorGroups;
        SetUnsavedChanges(false);
        return;
      }

      var profile = _loadedConfigFile.GetProfile(_selectedProfileKind);

      _currentProfile = profile;
      _editorGroups = BuildEditorGroups(profile);
      SubscribeEditorChanges(_editorGroups);
      ProfileGroupsItemsControl.ItemsSource = _editorGroups;
      SetUnsavedChanges(false);
    }
    finally
    {
      _isLoadingEditor = false;
    }
  }

  private void ApplyEditorGroupsToProfile(LegacyMkiHardwareProfile profile)
  {
    foreach (var group in _editorGroups)
    {
      foreach (var row in group.SwitchRows)
      {
        row.ApplyToProfile(profile, row);
      }
    }

    foreach (var item in EnumerateItems(_editorGroups))
    {
      item.ApplyToProfile(profile, item);
    }
  }

  private AskMkiSettingGroup CreateSwitchMatrixGroup(AskMkiGroupDefinition definition, LegacyMkiHardwareProfile profile)
  {
    var group = new AskMkiSettingGroup
    {
      Code = definition.Code,
      Title = definition.Title,
      Description = definition.Description,
      Summary = definition.Summary,
      IsExpanded = false
    };

    var names = StringsFromResource("AskMki.SwitchNames");

    for (var index = 0; index < names.Count; index++)
    {
      var rowIndex = index;
      var rowName = names[rowIndex];

      group.SwitchRows.Add(new AskMkiSwitchRangeRow
      {
        Name = rowName,
        IsFixedPresent = rowIndex == 0,
        FixedDescription = "Всегда",
        IsPresent = rowIndex == 0 || IsTruthy(GetValueByPath(profile, $"HardwareConfig.SkIs[{rowIndex}]")),
        FirstBk = Convert.ToString(GetValueByPath(profile, $"HardwareConfig.SkBkBeg[{rowIndex}]"), CultureInfo.InvariantCulture) ?? string.Empty,
        LastBk = Convert.ToString(GetValueByPath(profile, $"HardwareConfig.SkBkEnd[{rowIndex}]"), CultureInfo.InvariantCulture) ?? string.Empty,
        ApplyToProfile = (targetProfile, row) =>
        {
          if (rowIndex == 0)
          {
            SetValueByPath(
              targetProfile,
              $"HardwareConfig.SkIs[{rowIndex}]",
              ConvertToggleToType(true, GetValueTypeByPath(targetProfile, $"HardwareConfig.SkIs[{rowIndex}]")));
          }
          else
          {
            SetValueByPath(
              targetProfile,
              $"HardwareConfig.SkIs[{rowIndex}]",
              ConvertToggleToType(row.IsPresent, GetValueTypeByPath(targetProfile, $"HardwareConfig.SkIs[{rowIndex}]")));
          }

          SetValueByPath(
            targetProfile,
            $"HardwareConfig.SkBkBeg[{rowIndex}]",
            ConvertTextToType(row.FirstBk, GetValueTypeByPath(targetProfile, $"HardwareConfig.SkBkBeg[{rowIndex}]")));

          SetValueByPath(
            targetProfile,
            $"HardwareConfig.SkBkEnd[{rowIndex}]",
            ConvertTextToType(row.LastBk, GetValueTypeByPath(targetProfile, $"HardwareConfig.SkBkEnd[{rowIndex}]")));
        }
      });
    }

    return group;
  }

  private void SmallNumberTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
  {
    if (sender is not TextBox textBox)
    {
      return;
    }

    var newText = GetProposedText(textBox, e.Text);

    e.Handled = newText.Length > 2 || newText.Any(ch => !char.IsDigit(ch));
  }

  private void SmallNumberTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
  {
    if (!e.DataObject.GetDataPresent(DataFormats.Text))
    {
      e.CancelCommand();
      return;
    }

    if (sender is not TextBox textBox)
    {
      e.CancelCommand();
      return;
    }

    var pastedText = e.DataObject.GetData(DataFormats.Text) as string ?? string.Empty;
    var newText = GetProposedText(textBox, pastedText);

    if (newText.Length > 2 || newText.Any(ch => !char.IsDigit(ch)))
    {
      e.CancelCommand();
    }
  }

  private static string GetProposedText(TextBox textBox, string newTextPart)
  {
    var currentText = textBox.Text ?? string.Empty;

    var selectionStart = textBox.SelectionStart;
    var selectionLength = textBox.SelectionLength;

    if (selectionLength > 0)
    {
      currentText = currentText.Remove(selectionStart, selectionLength);
    }

    return currentText.Insert(selectionStart, newTextPart);
  }

  private static IEnumerable<AskMkiSettingItem> EnumerateItems(IEnumerable<AskMkiSettingGroup> groups)
  {
    foreach (var group in groups)
    {
      foreach (var item in group.Items)
      {
        yield return item;
      }

      foreach (var childItem in EnumerateItems(group.Children))
      {
        yield return childItem;
      }
    }
  }

  private string ResolveAndPersistPaths()
  {
    var mkiPath = MkiPathTextBox.Text?.Trim() ?? string.Empty;
    var configPath = ConfigPathTextBox.Text?.Trim() ?? string.Empty;

    LegacyMkiConfig.SetMkiPath(mkiPath);

    if (string.IsNullOrWhiteSpace(configPath) && !string.IsNullOrWhiteSpace(mkiPath))
    {
      configPath = LegacyMkiConfig.GetConfigPath();
      ConfigPathTextBox.Text = configPath;
    }

    LegacyMkiConfig.SetConfigPath(configPath);
    return configPath;
  }

  private void UpdateClearButtonState()
  {
    ClearMkiPathButton.IsEnabled = !string.IsNullOrWhiteSpace(MkiPathTextBox.Text);
  }

  private void ProfileGroupsScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
  {
    if (e.OriginalSource is DependencyObject source)
    {
      var comboBox = FindVisualParent<ComboBox>(source);
      if (comboBox?.IsDropDownOpen == true)
      {
        return;
      }
    }

    UpdateProfileGroupsScrollHeight();

    var scrollViewer = ProfileGroupsScrollViewer;
    if (scrollViewer == null)
    {
      return;
    }

    if (scrollViewer.ExtentHeight <= scrollViewer.ViewportHeight)
    {
      e.Handled = false;
      return;
    }

    var newOffset = scrollViewer.VerticalOffset - e.Delta;
    newOffset = Math.Max(0, Math.Min(scrollViewer.ScrollableHeight, newOffset));

    scrollViewer.ScrollToVerticalOffset(newOffset);
    e.Handled = true;
  }

  private static T? FindVisualParent<T>(DependencyObject? source)
    where T : DependencyObject
  {
    while (source != null)
    {
      if (source is T target)
      {
        return target;
      }

      source = VisualTreeHelper.GetParent(source);
    }

    return null;
  }

  private ObservableCollection<AskMkiSettingGroup> BuildEditorGroups(LegacyMkiHardwareProfile profile)
  {
    _currentProfile = profile;

    var groups = new ObservableCollection<AskMkiSettingGroup>();

    foreach (var definition in GroupDefinitions("AskMki.Groups"))
    {
      if (string.Equals(definition.Code, "A", StringComparison.OrdinalIgnoreCase))
      {
        groups.Add(CreateSwitchMatrixGroup(definition, profile));
      }
      else
      {
        groups.Add(CreateFlatGroupFromDefinition(definition));
      }
    }

    return groups;
  }

  private IReadOnlyList<AskMkiGroupDefinition> GroupDefinitions(string key)
  {
    return ((IEnumerable)FindResource(key)).Cast<AskMkiGroupDefinition>().ToArray();
  }

  private IReadOnlyList<AskMkiFieldDefinition> FieldDefinitions(string key)
  {
    return ((IEnumerable)FindResource(key)).Cast<AskMkiFieldDefinition>().ToArray();
  }

  private IReadOnlyList<AskMkiSettingOption> OptionsFromResource(string key)
  {
    return ((IEnumerable)FindResource(key)).Cast<AskMkiSettingOption>().ToArray();
  }

  private IReadOnlyList<string> StringsFromResource(string key)
  {
    return ((IEnumerable)FindResource(key)).Cast<string>().ToArray();
  }

  private AskMkiSettingGroup CreateFlatGroupFromDefinition(AskMkiGroupDefinition definition)
  {
    var group = new AskMkiSettingGroup
    {
      Code = definition.Code,
      Title = definition.Title,
      Description = definition.Description,
      Summary = definition.Summary,
      IsExpanded = false
    };

    AddFieldsRecursive(group, definition);

    return group;
  }

  private void AddFieldsRecursive(AskMkiSettingGroup targetGroup, AskMkiGroupDefinition definition)
  {
    if (!string.IsNullOrWhiteSpace(definition.FieldsKey))
    {
      AddDefinitionItems(targetGroup, definition.FieldsKey);
    }

    foreach (var child in definition.Children)
    {
      AddFieldsRecursive(targetGroup, child);
    }
  }

  private void AddDefinitionItems(AskMkiSettingGroup group, string resourceKey)
  {
    foreach (var definition in FieldDefinitions(resourceKey))
    {
      foreach (var expandedDefinition in ExpandDefinition(definition))
      {
        group.Items.Add(CreateItemFromDefinition(expandedDefinition));
      }
    }
  }

  private IEnumerable<AskMkiFieldDefinition> ExpandDefinition(AskMkiFieldDefinition definition)
  {
    if (definition.Count <= 0)
    {
      yield return definition;
      yield break;
    }

    IReadOnlyList<string>? names = null;
    if (!string.IsNullOrWhiteSpace(definition.NamesKey))
    {
      names = StringsFromResource(definition.NamesKey);
    }

    for (var offset = 0; offset < definition.Count; offset++)
    {
      var index = definition.StartIndex + offset;

      var labelArgument = names != null && index < names.Count
        ? names[index]
        : (index + 1).ToString(CultureInfo.CurrentCulture);

      yield return new AskMkiFieldDefinition
      {
        Label = string.Format(CultureInfo.CurrentCulture, definition.LabelFormat, labelArgument),
        Description = definition.Description,
        EditorKind = definition.EditorKind,
        Path = string.Format(CultureInfo.InvariantCulture, definition.PathFormat, index),
        OptionsKey = definition.OptionsKey
      };
    }
  }

  private AskMkiSettingItem CreateItemFromDefinition(AskMkiFieldDefinition definition)
  {
    if (definition.EditorKind == AskMkiSettingEditorKind.Info)
    {
      return new AskMkiSettingItem
      {
        Label = definition.Label,
        Description = definition.Description,
        EditorKind = AskMkiSettingEditorKind.Info,
        ApplyToProfile = (_, _) => { }
      };
    }

    var value = GetValueByPath(CurrentProfile(), definition.Path);

    return definition.EditorKind switch
    {
      AskMkiSettingEditorKind.Text => new AskMkiSettingItem
      {
        Label = definition.Label,
        Description = definition.Description,
        EditorKind = AskMkiSettingEditorKind.Text,
        TextValue = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
        ApplyToProfile = (profile, item) =>
        {
          var targetType = GetValueTypeByPath(profile, definition.Path);
          SetValueByPath(profile, definition.Path, ConvertTextToType(item.TextValue, targetType));
        }
      },

      AskMkiSettingEditorKind.Toggle => new AskMkiSettingItem
      {
        Label = definition.Label,
        Description = definition.Description,
        EditorKind = AskMkiSettingEditorKind.Toggle,
        ToggleValue = IsTruthy(value),
        ApplyToProfile = (profile, item) =>
        {
          var targetType = GetValueTypeByPath(profile, definition.Path);
          SetValueByPath(profile, definition.Path, ConvertToggleToType(item.ToggleValue, targetType));
        }
      },

      AskMkiSettingEditorKind.Choice => CreateChoiceItemFromDefinition(definition, value),

      _ => throw new InvalidOperationException(UiFormat("AskMki.Error.UnknownEditorType", definition.EditorKind))
    };
  }

  private AskMkiSettingItem CreateChoiceItemFromDefinition(AskMkiFieldDefinition definition, object? value)
  {
    if (string.IsNullOrWhiteSpace(definition.OptionsKey))
    {
      throw new InvalidOperationException(UiFormat("AskMki.Error.OptionsKeyMissing", definition.Label));
    }

    var options = OptionsFromResource(definition.OptionsKey);

    var item = new AskMkiSettingItem
    {
      Label = definition.Label,
      Description = definition.Description,
      EditorKind = AskMkiSettingEditorKind.Choice,
      Options = new ObservableCollection<AskMkiSettingOption>(options),
      ApplyToProfile = (profile, editorItem) =>
      {
        if (editorItem.SelectedOption == null)
        {
          throw new InvalidOperationException(UiFormat("AskMki.Error.OptionNotSelected", editorItem.Label));
        }

        var targetType = GetValueTypeByPath(profile, definition.Path);
        SetValueByPath(profile, definition.Path, ConvertTextToType(editorItem.SelectedOption.Value.ToString(CultureInfo.InvariantCulture), targetType));
      }
    };

    item.SelectedOption = item.Options.FirstOrDefault(option =>
      string.Equals(
        option.Value.ToString(CultureInfo.InvariantCulture),
        Convert.ToString(value, CultureInfo.InvariantCulture),
        StringComparison.OrdinalIgnoreCase));

    return item;
  }

  private LegacyMkiHardwareProfile CurrentProfile()
  {
    if (_currentProfile == null)
    {
      throw new InvalidOperationException(UiString("AskMki.Error.ProfileNotLoaded"));
    }

    return _currentProfile;
  }

  private static object? GetValueByPath(object source, string path)
  {
    object? current = source;

    foreach (var segment in path.Split('.'))
    {
      if (current == null)
      {
        return null;
      }

      current = GetSegmentValue(current, segment);
    }

    return current;
  }

  private static Type GetValueTypeByPath(object source, string path)
  {
    object? current = source;
    var currentType = source.GetType();

    foreach (var segment in path.Split('.'))
    {
      var parsed = ParseSegment(segment);
      var property = currentType.GetProperty(parsed.PropertyName, BindingFlags.Instance | BindingFlags.Public)
        ?? throw new InvalidOperationException($"Свойство \"{parsed.PropertyName}\" не найдено в {currentType.Name}.");

      if (parsed.Index == null)
      {
        currentType = property.PropertyType;
        current = property.GetValue(current);
        continue;
      }

      var array = property.GetValue(current) as Array
        ?? throw new InvalidOperationException($"Свойство \"{parsed.PropertyName}\" не является массивом.");

      currentType = property.PropertyType.GetElementType()
        ?? throw new InvalidOperationException($"Не удалось определить тип элемента массива \"{parsed.PropertyName}\".");

      current = array.GetValue(parsed.Index.Value);
    }

    return currentType;
  }

  private static void SetValueByPath(object source, string path, object? value)
  {
    var parts = path.Split('.');
    object? current = source;

    for (var index = 0; index < parts.Length - 1; index++)
    {
      current = GetSegmentValue(current!, parts[index]);
    }

    if (current == null)
    {
      throw new InvalidOperationException($"Не удалось записать значение по пути \"{path}\".");
    }

    SetSegmentValue(current, parts[^1], value);
  }

  private static object? GetSegmentValue(object source, string segment)
  {
    var parsed = ParseSegment(segment);
    var property = source.GetType().GetProperty(parsed.PropertyName, BindingFlags.Instance | BindingFlags.Public)
      ?? throw new InvalidOperationException($"Свойство \"{parsed.PropertyName}\" не найдено в {source.GetType().Name}.");

    var value = property.GetValue(source);

    if (parsed.Index == null)
    {
      return value;
    }

    if (value is not Array array)
    {
      throw new InvalidOperationException($"Свойство \"{parsed.PropertyName}\" не является массивом.");
    }

    return array.GetValue(parsed.Index.Value);
  }

  private static void SetSegmentValue(object source, string segment, object? value)
  {
    var parsed = ParseSegment(segment);
    var property = source.GetType().GetProperty(parsed.PropertyName, BindingFlags.Instance | BindingFlags.Public)
      ?? throw new InvalidOperationException($"Свойство \"{parsed.PropertyName}\" не найдено в {source.GetType().Name}.");

    if (parsed.Index == null)
    {
      property.SetValue(source, ConvertObjectToType(value, property.PropertyType));
      return;
    }

    if (property.GetValue(source) is not Array array)
    {
      throw new InvalidOperationException($"Свойство \"{parsed.PropertyName}\" не является массивом.");
    }

    var elementType = property.PropertyType.GetElementType()
      ?? throw new InvalidOperationException($"Не удалось определить тип элемента массива \"{parsed.PropertyName}\".");

    array.SetValue(ConvertObjectToType(value, elementType), parsed.Index.Value);
  }

  private static (string PropertyName, int? Index) ParseSegment(string segment)
  {
    var match = Regex.Match(segment, @"^(?<name>[A-Za-z0-9_]+)(\[(?<index>\d+)\])?$");

    if (!match.Success)
    {
      throw new InvalidOperationException($"Некорректный сегмент пути: \"{segment}\".");
    }

    var propertyName = match.Groups["name"].Value;
    var indexGroup = match.Groups["index"];

    return indexGroup.Success
      ? (propertyName, int.Parse(indexGroup.Value, CultureInfo.InvariantCulture))
      : (propertyName, null);
  }

  private static bool IsTruthy(object? value)
  {
    return value switch
    {
      null => false,
      bool boolValue => boolValue,
      byte byteValue => byteValue != 0,
      ushort ushortValue => ushortValue != 0,
      short shortValue => shortValue != 0,
      int intValue => intValue != 0,
      double doubleValue => Math.Abs(doubleValue) > double.Epsilon,
      _ => !string.IsNullOrWhiteSpace(value.ToString())
    };
  }

  private static object ConvertToggleToType(bool value, Type targetType)
  {
    return ConvertTextToType(value ? "1" : "0", targetType);
  }

  private static object ConvertTextToType(string? text, Type targetType)
  {
    text ??= string.Empty;

    if (targetType == typeof(string))
    {
      return text;
    }

    if (targetType == typeof(byte))
    {
      return byte.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }

    if (targetType == typeof(ushort))
    {
      return ushort.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }

    if (targetType == typeof(short))
    {
      return short.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }

    if (targetType == typeof(int))
    {
      return int.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }

    if (targetType == typeof(double))
    {
      return double.Parse(text.Replace(',', '.'), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture);
    }

    if (targetType == typeof(bool))
    {
      return text == "1" || bool.Parse(text);
    }

    return Convert.ChangeType(text, targetType, CultureInfo.InvariantCulture);
  }

  private static object ConvertObjectToType(object? value, Type targetType)
  {
    if (value != null && targetType.IsInstanceOfType(value))
    {
      return value;
    }

    return ConvertTextToType(value?.ToString(), targetType);
  }
}
