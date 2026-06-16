using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.LegacyMki;
using Ask.DataBase.Provider.Services.Devices;
using Ask.UI.Infrastructure.UI.Overlay.Drawer.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
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
  private readonly int? _numberChassis;
  private const double AskMkiConfigDrawerPanelWidth = 470d;
  private bool _isLoadingEditor;
  private bool _hasUnsavedChanges;
  private LegacyMkiHardwareConfigFile? _loadedConfigFile;
  private ObservableCollection<AskMkiSettingGroup> _editorGroups = new();
  private LegacyMkiHardwareProfile? _currentProfile;
  private LegacyMkiProfileKind _selectedProfileKind;

  /// <summary>
  /// Инициализирует контрол без заранее выбранной стойки АСК.
  /// </summary>
  public AskMkiConfigControl()
    : this(null)
  {
  }

  /// <summary>
  /// Инициализирует контрол для указанной стойки АСК.
  /// </summary>
  public AskMkiConfigControl(int? numberChassis)
  {
    _numberChassis = numberChassis;

    InitializeComponent();

    Loaded += AskMkiConfigControl_Loaded;
    SizeChanged += AskMkiConfigControl_SizeChanged;
    IsVisibleChanged += AskMkiConfigControl_IsVisibleChanged;

    _selectedProfileKind = LegacyMkiConfig.GetSelectedProfile();
    LoadProfileFromDatabaseIntoEditor();

    
  }

  /// <summary>
  /// Возвращает строковый ресурс интерфейса по ключу.
  /// </summary>
  private string UiString(string resourceKey)
  {
    return FindResource(resourceKey) as string
      ?? throw new InvalidOperationException($"String resource '{resourceKey}' was not found.");
  }

  /// <summary>
  /// Форматирует строковый ресурс интерфейса с учетом текущей культуры.
  /// </summary>
  private string UiFormat(string resourceKey, params object[] args)
  {
    return string.Format(CultureInfo.CurrentCulture, UiString(resourceKey), args);
  }

  /// <summary>
  /// Пересчитывает доступную высоту списка групп конфигурации.
  /// </summary>
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

    if (!IsVisualAncestor(window, ProfileGroupsScrollViewer))
    {
      return;
    }

    try
    {
      var point = ProfileGroupsScrollViewer.TransformToAncestor(window).Transform(new Point(0, 0));
      var availableHeight = window.ActualHeight - point.Y - 35;

      ProfileGroupsScrollViewer.MaxHeight = Math.Max(160, availableHeight);
    }
    catch (InvalidOperationException)
    {
      ProfileGroupsScrollViewer.MaxHeight = 600;
    }
    catch (Exception ex)
    {
      LogException(ex, customMessage: "AskMkiConfigControl.UpdateProfileGroupsScrollHeight");
      ProfileGroupsScrollViewer.MaxHeight = 600;
    }
  }

  /// <summary>
  /// Проверяет, находится ли визуальный элемент внутри указанного визуального предка.
  /// </summary>
  private static bool IsVisualAncestor(DependencyObject ancestor, DependencyObject child)
  {
    var current = child;

    while (current != null)
    {
      if (ReferenceEquals(current, ancestor))
      {
        return true;
      }

      current = VisualTreeHelper.GetParent(current);
    }

    return false;
  }

  /// <summary>
  /// Обрабатывает загрузку контрола и обновляет начальную компоновку.
  /// </summary>
  private void AskMkiConfigControl_Loaded(object sender, RoutedEventArgs e)
  {

    UpdateProfileGroupsScrollHeight();
  }

  /// <summary>
  /// Обрабатывает изменение размера контрола.
  /// </summary>
  private void AskMkiConfigControl_SizeChanged(object sender, SizeChangedEventArgs e)
  {
    UpdateProfileGroupsScrollHeight();
  }

  /// <summary>
  /// Обрабатывает появление контрола на экране.
  /// </summary>
  private void AskMkiConfigControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
  {
    if (e.NewValue is true)
    {

      UpdateProfileGroupsScrollHeight();
    }
  }



  /// <summary>
  /// Отмечает наличие несохраненных изменений и показывает кнопки подтверждения.
  /// </summary>
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

  /// <summary>
  /// Подписывает элементы редактора на уведомления об изменениях.
  /// </summary>
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

  /// <summary>
  /// Обрабатывает изменение строки матрицы СК/БК.
  /// </summary>
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

  /// <summary>
  /// Обрабатывает изменение обычного поля редактора.
  /// </summary>
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

  /// <summary>
  /// Запускает сохранение профиля при нажатии на иконку подтверждения.
  /// </summary>
  private void SaveConfigIcon_PreviewMouseDown(object sender, MouseButtonEventArgs e)
  {
    SaveConfigButton_Click(sender, e);
    e.Handled = true;
  }

  /// <summary>
  /// Отменяет изменения при нажатии на иконку отмены.
  /// </summary>
  private void CancelConfigIcon_PreviewMouseDown(object sender, MouseButtonEventArgs e)
  {
    CancelConfigButton_Click(sender, e);
    e.Handled = true;
  }
  /// <summary>
  /// Сохраняет текущий профиль legacy-конфигурации в базу данных.
  /// </summary>
  private async void SaveConfigButton_Click(object sender, RoutedEventArgs e)
  {
    try
    {
      if (_loadedConfigFile == null)
      {
        _loadedConfigFile = new LegacyMkiHardwareConfigFile();
        _loadedConfigFile.ActiveProfileIndex = (byte)_selectedProfileKind;
      }

      var profile = _loadedConfigFile.GetProfile(_selectedProfileKind);
      ApplyEditorGroupsToProfile(profile);

      profile.HardwareConfig.Nas = (byte)_selectedProfileKind;
      _loadedConfigFile.SetProfile(_selectedProfileKind, profile);
      _loadedConfigFile.ActiveProfileIndex = (byte)_selectedProfileKind;

      LegacyMkiConfig.SetSelectedProfile(_selectedProfileKind);
      await SaveProfileToDatabaseAsync(profile);

      LoadSelectedProfileIntoEditor();

      Message.MessageBoxCustom.Show(
        "Настройки АСК сохранены",
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

  /// <summary>
  /// Перезагружает текущий профиль из последнего сохраненного состояния.
  /// </summary>
  private void CancelConfigButton_Click(object sender, RoutedEventArgs e)
  {
    LoadSelectedProfileIntoEditor();
  }
  /// <summary>
  /// Создает рабочий файл конфигурации из профиля, сохраненного в базе данных.
  /// </summary>
  private void LoadProfileFromDatabaseIntoEditor()
  {
    _loadedConfigFile = new LegacyMkiHardwareConfigFile();
    _loadedConfigFile.ActiveProfileIndex = (byte)_selectedProfileKind;
    ApplyProfileFromDatabaseIfExists();
    LoadSelectedProfileIntoEditor();
  }

  /// <summary>
  /// Определяет активный legacy-профиль из рабочего файла конфигурации.
  /// </summary>
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

  /// <summary>
  /// Сохраняет профиль для текущей legacy-стойки АСК.
  /// </summary>
  private async Task SaveProfileToDatabaseAsync(LegacyMkiHardwareProfile profile)
  {
    var numberChassis = await TryResolveLegacyAskChassisNumberAsync();
    if (numberChassis == null)
    {
      return;
    }

    var service = new LegacyMkiHardwareProfileDtoService();
    await service.SaveProfileAsync(numberChassis.Value, _selectedProfileKind, profile);
  }

  /// <summary>
  /// Подставляет в рабочий файл профиль, сохраненный в базе данных для текущей стойки.
  /// </summary>
  private void ApplyProfileFromDatabaseIfExists()
  {
    if (_loadedConfigFile == null)
    {
      return;
    }

    var numberChassis = TryResolveLegacyAskChassisNumberAsync()
      .GetAwaiter()
      .GetResult();

    if (numberChassis == null)
    {
      return;
    }

    var service = new LegacyMkiHardwareProfileDtoService();
    service.EnsureDefaultProfilesAsync(numberChassis.Value)
      .GetAwaiter()
      .GetResult();

    var profileDto = service.GetByChassisAsync(numberChassis.Value, _selectedProfileKind)
      .GetAwaiter()
      .GetResult();

    if (profileDto == null)
    {
      return;
    }

    _loadedConfigFile.SetProfile(_selectedProfileKind, profileDto.ToProfile());
  }

  /// <summary>
  /// Возвращает номер выбранной legacy-стойки АСК или находит первую такую стойку в базе.
  /// </summary>
  private async Task<int?> TryResolveLegacyAskChassisNumberAsync()
  {
    if (_numberChassis != null)
    {
      return _numberChassis.Value;
    }

    var service = new ChassisManagerDtoService();
    var chassis = await service.GetAllAsync();
    var legacyAsk = chassis.FirstOrDefault(x =>
      string.Equals(x.Name, "\u0422\u0435\u0441\u0442\u0435\u0440 \u0410\u0421\u041a", StringComparison.OrdinalIgnoreCase) ||
      x.DeviceClass.EndsWith(".ManagerASKMKI", StringComparison.Ordinal));

    return legacyAsk?.Number;
  }
  /// <summary>
  /// Загружает выбранный профиль в визуальные группы редактора.
  /// </summary>
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
        SelectProfileGroup(null);
        SetUnsavedChanges(false);
        return;
      }

      var profile = _loadedConfigFile.GetProfile(_selectedProfileKind);

      _currentProfile = profile;
      _editorGroups = BuildEditorGroups(profile);
      SubscribeEditorChanges(_editorGroups);
      ProfileGroupsItemsControl.ItemsSource = _editorGroups;
      SelectProfileGroup(_editorGroups.FirstOrDefault());
      SetUnsavedChanges(false);
    }
    finally
    {
      _isLoadingEditor = false;
    }
  }

  /// <summary>
  /// Применяет значения всех полей редактора к доменному профилю.
  /// </summary>
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

  /// <summary>
  /// Открывает выбранную группу параметров в правой панели редактора.
  /// </summary>
  private void SelectProfileGroup(AskMkiSettingGroup? group)
  {
    foreach (var editorGroup in _editorGroups)
    {
      editorGroup.IsExpanded = ReferenceEquals(editorGroup, group);
    }
  }

  /// <summary>
  /// Обрабатывает выбор группы параметров в левом списке.
  /// </summary>
  private void GroupButton_Click(object sender, RoutedEventArgs e)
  {
    if (sender is Button { DataContext: AskMkiSettingGroup group })
    {
      SelectProfileGroup(group);
    }
  }

  /// <summary>
  /// Открывает выбранную группу параметров в правой панели редактирования.
  /// </summary>
  private async void EditGroupButton_Click(object sender, RoutedEventArgs e)
  {
    if (sender is not Button { CommandParameter: AskMkiSettingGroup group })
    {
      return;
    }

    SelectProfileGroup(group);

    var editor = new AskMkiGroupEditorDrawerControl
    {
      DataContext = group
    };

    editor.SaveRequested += (_, _) =>
    {
      SaveConfigButton_Click(editor, new RoutedEventArgs());
      DrawerHostService.Instance.Close();
    };

    editor.CancelRequested += (_, _) =>
    {
      CancelConfigButton_Click(editor, new RoutedEventArgs());
      DrawerHostService.Instance.Close();
    };

    await DrawerHostService.Instance.OpenContentAsync(
      editor,
      "Редактирование параметров",
      "F4 - закрыть",
      onClose: null,
      panelWidth: AskMkiConfigDrawerPanelWidth);
  }

  /// <summary>
  /// Создает специальную группу редактирования СК/БК в виде матрицы.
  /// </summary>
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
        FixedDescription = "\\u0412\\u0441\\u0435\\u0433\\u0434\\u0430",
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

  /// <summary>
  /// Ограничивает ввод номера БК двумя цифрами.
  /// </summary>
  private void SmallNumberTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
  {
    if (sender is not TextBox textBox)
    {
      return;
    }

    var newText = GetProposedText(textBox, e.Text);

    e.Handled = newText.Length > 2 || newText.Any(ch => !char.IsDigit(ch));
  }

  /// <summary>
  /// Проверяет вставляемое значение номера БК.
  /// </summary>
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

  /// <summary>
  /// Возвращает текст поля после предполагаемого ввода или вставки.
  /// </summary>
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

  /// <summary>
  /// Перечисляет все поля редактора во всех группах.
  /// </summary>
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

  /// <summary>
  /// Обрабатывает прокрутку списка групп с учетом открытых выпадающих списков.
  /// </summary>
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

  /// <summary>
  /// Находит ближайшего визуального родителя указанного типа.
  /// </summary>
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

  /// <summary>
  /// Строит коллекцию групп редактора по XAML-описанию полей.
  /// </summary>
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

  /// <summary>
  /// Возвращает определения групп из ресурсов XAML.
  /// </summary>
  private IReadOnlyList<AskMkiGroupDefinition> GroupDefinitions(string key)
  {
    return ((IEnumerable)FindResource(key)).Cast<AskMkiGroupDefinition>().ToArray();
  }

  /// <summary>
  /// Возвращает определения полей из ресурсов XAML.
  /// </summary>
  private IReadOnlyList<AskMkiFieldDefinition> FieldDefinitions(string key)
  {
    return ((IEnumerable)FindResource(key)).Cast<AskMkiFieldDefinition>().ToArray();
  }

  /// <summary>
  /// Возвращает варианты выбора из ресурсов XAML.
  /// </summary>
  private IReadOnlyList<AskMkiSettingOption> OptionsFromResource(string key)
  {
    return ((IEnumerable)FindResource(key)).Cast<AskMkiSettingOption>().ToArray();
  }

  /// <summary>
  /// Возвращает строковый список из ресурсов XAML.
  /// </summary>
  private IReadOnlyList<string> StringsFromResource(string key)
  {
    return ((IEnumerable)FindResource(key)).Cast<string>().ToArray();
  }

  /// <summary>
  /// Создает обычную плоскую группу полей редактора.
  /// </summary>
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

  /// <summary>
  /// Рекурсивно добавляет поля дочерних определений в целевую группу.
  /// </summary>
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

  /// <summary>
  /// Добавляет в группу поля из указанного XAML-ресурса.
  /// </summary>
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

  /// <summary>
  /// Разворачивает повторяющееся определение поля в отдельные элементы.
  /// </summary>
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

  /// <summary>
  /// Создает элемент редактора по определению поля.
  /// </summary>
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

  /// <summary>
  /// Создает элемент редактора с выпадающим списком.
  /// </summary>
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

  /// <summary>
  /// Возвращает текущий редактируемый профиль.
  /// </summary>
  private LegacyMkiHardwareProfile CurrentProfile()
  {
    if (_currentProfile == null)
    {
      throw new InvalidOperationException(UiString("AskMki.Error.ProfileNotLoaded"));
    }

    return _currentProfile;
  }

  /// <summary>
  /// Читает значение объекта по точечному пути с поддержкой индексов массивов.
  /// </summary>
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

  /// <summary>
  /// Определяет тип значения по точечному пути с поддержкой индексов массивов.
  /// </summary>
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

  /// <summary>
  /// Записывает значение объекта по точечному пути с поддержкой индексов массивов.
  /// </summary>
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

  /// <summary>
  /// Читает значение одного сегмента пути.
  /// </summary>
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

  /// <summary>
  /// Записывает значение одного сегмента пути.
  /// </summary>
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

  /// <summary>
  /// Разбирает сегмент пути на имя свойства и необязательный индекс массива.
  /// </summary>
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

  /// <summary>
  /// Преобразует значение legacy-поля к логическому признаку включения.
  /// </summary>
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

  /// <summary>
  /// Преобразует значение переключателя к целевому типу legacy-поля.
  /// </summary>
  private static object ConvertToggleToType(bool value, Type targetType)
  {
    return ConvertTextToType(value ? "1" : "0", targetType);
  }

  /// <summary>
  /// Преобразует текстовое значение редактора к целевому типу legacy-поля.
  /// </summary>
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

  /// <summary>
  /// Преобразует произвольное значение к целевому типу legacy-поля.
  /// </summary>
  private static object ConvertObjectToType(object? value, Type targetType)
  {
    if (value != null && targetType.IsInstanceOfType(value))
    {
      return value;
    }

    return ConvertTextToType(value?.ToString(), targetType);
  }
}
