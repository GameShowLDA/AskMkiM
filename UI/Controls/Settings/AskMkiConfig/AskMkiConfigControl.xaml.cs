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
  private const double DrawerWidth = 650d;
  private readonly int? _numberChassis;
  private bool _isLoadingEditor;
  private bool _isUpdatingVoltmeterEditorState;
  private bool _hasUnsavedChanges;
  private LegacyMkiHardwareConfigFile? _loadedConfigFile;
  private ObservableCollection<AskMkiSettingGroup> _editorGroups = new ObservableCollection<AskMkiSettingGroup>();
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
      if (sender is AskMkiSettingItem item)
      {
        HandleEditorItemChanged(item, e.PropertyName);
      }

      SetUnsavedChanges(true);
    }
  }

  /// <summary>
  /// Обрабатывает зависимые поля редактора конфигурации.
  /// </summary>
  private void HandleEditorItemChanged(AskMkiSettingItem item, string? propertyName)
  {
    if (_isUpdatingVoltmeterEditorState)
    {
      return;
    }

    if (propertyName != nameof(AskMkiSettingItem.SelectedOption))
    {
      return;
    }

    if (item.Path is "HardwareAux.VoltmeterDeviceClass" or "HardwareAux.VoltmeterConnectionType")
    {
      UpdateVoltmeterEditorState(
        autoResolveUsb: item.Path == "HardwareAux.VoltmeterConnectionType",
        resetConnectionSelection: item.Path == "HardwareAux.VoltmeterDeviceClass");
    }
  }

  /// <summary>
  /// Обновляет видимость полей подключения цифрового вольтметра АСК.
  /// </summary>
  private void UpdateVoltmeterEditorState(bool autoResolveUsb, bool resetConnectionSelection = false)
  {
    if (_isUpdatingVoltmeterEditorState)
    {
      return;
    }

    _isUpdatingVoltmeterEditorState = true;
    try
    {
      var meterItem = FindEditorItemByPath("HardwareAux.VoltmeterDeviceClass");
      var connectionItem = FindEditorItemByPath("HardwareAux.VoltmeterConnectionType");
      var usbItem = FindEditorItemByPath("HardwareAux.UsbAddrVm");
      var ipItem = FindEditorItemByPath("HardwareAux.VoltmeterIpAddress");

      var selectedDeviceClass = meterItem?.SelectedOption?.Value;
      var supportsUsb = IsUsbVoltmeter(selectedDeviceClass);
      var supportsIp = IsIpVoltmeter(selectedDeviceClass);

      if (connectionItem != null && supportsIp && !supportsUsb)
      {
        SetVoltmeterConnectionOptions(connectionItem, "IP", resetConnectionSelection);
      }

      if (connectionItem != null && supportsUsb && !supportsIp)
      {
        SetVoltmeterConnectionOptions(connectionItem, "USB", resetConnectionSelection);
      }

      if (connectionItem != null && supportsUsb && supportsIp)
      {
        SetVoltmeterConnectionOptions(connectionItem, null, resetConnectionSelection);
      }

      var connectionType = connectionItem?.SelectedOption?.Value ?? string.Empty;
      var isUsb = string.Equals(connectionType, "USB", StringComparison.OrdinalIgnoreCase) && supportsUsb;
      var isIp = string.Equals(connectionType, "IP", StringComparison.OrdinalIgnoreCase) && supportsIp;

      if (usbItem != null)
      {
        usbItem.IsVisible = isUsb && supportsUsb;
      }

      if (ipItem != null)
      {
        ipItem.IsVisible = isIp;
      }

      if (autoResolveUsb && isUsb && supportsUsb)
      {
        ResolveVoltmeterUsbAddress();
      }
    }
    finally
    {
      _isUpdatingVoltmeterEditorState = false;
    }
  }

  private void SetVoltmeterConnectionOptions(AskMkiSettingItem connectionItem, string? onlyValue, bool resetSelection)
  {
    string? selectedValue = resetSelection ? string.Empty : connectionItem.SelectedOption?.Value;
    var placeholder = new AskMkiSettingOption
    {
      Value = string.Empty,
      Label = "Выбор типа подключения:"
    };

    var options = OptionsFromResource("AskMki.Options.VoltmeterConnection")
      .Where(option => onlyValue == null || string.Equals(option.Value, onlyValue, StringComparison.OrdinalIgnoreCase))
      .GroupBy(option => option.Value, StringComparer.OrdinalIgnoreCase)
      .Select(group => group.First())
      .ToArray();

    connectionItem.Options.Clear();
    connectionItem.Options.Add(placeholder);
    foreach (var option in options)
    {
      connectionItem.Options.Add(option);
    }

    connectionItem.SelectedOption = connectionItem.Options.FirstOrDefault(option =>
      string.Equals(option.Value, selectedValue, StringComparison.OrdinalIgnoreCase))
      ?? connectionItem.Options.FirstOrDefault();
  }

  /// <summary>
  /// Автоматически подставляет USB-адрес выбранного цифрового вольтметра, если устройство найдено.
  /// </summary>
  private void ResolveVoltmeterUsbAddress()
  {
    var meterItem = FindEditorItemByPath("HardwareAux.VoltmeterDeviceClass");
    var usbItem = FindEditorItemByPath("HardwareAux.UsbAddrVm");

    if (meterItem?.SelectedOption == null || usbItem == null)
    {
      return;
    }

    ApplyUsbInfo(usbItem, TryResolveUsbInfo(meterItem.SelectedOption.Value));
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
      LegacyMkiHardwareProfileValidator.ThrowIfInvalid(profile);

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
      UpdateVoltmeterEditorState(autoResolveUsb: true);
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
  /// <summary>
  /// Находит поле редактора по пути свойства legacy-профиля.
  /// </summary>
  /// <param name="path">Путь свойства legacy-профиля.</param>
  /// <returns>Поле редактора или <see langword="null"/>, если поле не найдено.</returns>
  private AskMkiSettingItem? FindEditorItemByPath(string path)
  {
    return EnumerateItems(_editorGroups)
      .FirstOrDefault(item => string.Equals(item.Path, path, StringComparison.Ordinal));
  }

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
      panelWidth: DrawerWidth);
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

}
