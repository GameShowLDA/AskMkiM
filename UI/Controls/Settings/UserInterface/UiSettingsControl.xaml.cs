using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.Base;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Settings;
using Ask.Core.Shared.Metadata.Enums.UiEnums;
using Ask.UI.Infrastructure.Localization;
using Message;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static Ask.LogLib.LoggerUtility;

namespace UI.Controls.Settings.UserInterface
{
  /// <summary>
  /// Логика взаимодействия для UiSettingsControl.xaml
  /// </summary>
  public partial class UiSettingsControl : UserControl
  {
    private UserInterfaceDto _baseParameterModel { get; set; }
    private SettingsProtocolDto _baseProtocolModel { get; set; }
    private bool _isInitialized;
    private record LangOption(string Key, string Title);
    private record ThemeOption(string Key, string Title);

    /// <summary>
    /// Глобальный флаг наличия несохранённых изменений в разделе.
    /// <para>True — есть отличия от сохранённой модели; False — всё совпадает.</para>
    /// </summary>
    public bool HasUnsavedChanges { get; private set; }

    public UiSettingsControl()
    {
      InitializeComponent();
      Loaded += UiSettingsControl_Loaded;
      Unloaded += UiSettingsControl_Unloaded;
    }

    /// <summary>
    /// Клик по галочке «сохранить»: сохраняет текущую модель,
    /// перечитывает базу и скрывает индикаторы изменений.
    /// </summary>
    private async void Success_PreviewMouseDown(object sender, MouseButtonEventArgs e) => await SaveData();

    /// <summary>
    /// Клик по кресту «отменить»: откатывает значения к сохранённой модели
    /// и скрывает индикаторы изменений.
    /// </summary>
    private void Error_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      DefalultData();
      Error.Visibility = Visibility.Collapsed;
      Success.Visibility = Visibility.Collapsed;
      HasUnsavedChanges = false;
    }

    public async Task SaveData()
    {
      try
      {
        await UserInterfaceConfig.SaveProtocolModel(GetModel());
        await ProtocolConfig.SaveProtocolModel(GetProtocolModel());
        _baseParameterModel = await UserInterfaceConfig.GetParameterModel();
        _baseProtocolModel = ProtocolConfig.GetProtocolModel();

        Error.Visibility = Visibility.Collapsed;
        Success.Visibility = Visibility.Collapsed;
        HasUnsavedChanges = false;
      }
      catch (Exception ex)
      {
        LogException("Ошибка сохранения настроек интерфейса", ex);
        MessageBoxCustom.Show($"Ошибка сохранения настроек интерфейса: {ex.Message}", image: MessageBoxImage.Error);
      }
    }

    private void ValueChanged(object? sender, object? e)
    {
      if (!UserInterfaceEquals(_baseParameterModel, GetModel())
        || !PrintSettingsEquals(_baseProtocolModel, GetProtocolModel()))
      {
        Error.Visibility = Visibility.Visible;
        Success.Visibility = Visibility.Visible;
        HasUnsavedChanges = true;
      }
      else
      {
        Error.Visibility = Visibility.Collapsed;
        Success.Visibility = Visibility.Collapsed;
        HasUnsavedChanges = false;
      }
    }

    private async void UiSettingsControl_Loaded(object sender, RoutedEventArgs e)
    {
      _baseParameterModel = await UserInterfaceConfig.GetParameterModel();
      _baseProtocolModel = ProtocolConfig.GetProtocolModel();
      DefalultData();
      UpdateCommandAutoCollapseVisibility();

      if (!_isInitialized)
      {
        LanguageSelect.ValueChanged += ValueChanged;
        ThemeSelect.ValueChanged += ValueChanged;
        SyntaxHighlighting.CheckedChanged += SettingsCard_CheckedChanged;
        CommandBodyBackgroundHighlighting.CheckedChanged += SettingsCard_CheckedChanged;
        ChainPointBodyBackgroundHighlighting.CheckedChanged += SettingsCard_CheckedChanged;
        TopMenuIcons.CheckedChanged += SettingsCard_CheckedChanged;
        CommandAutoCollapsing.CheckedChanged += SettingsCard_CheckedChanged;
        PrintFontFamilyComboBox.SelectionChanged += (s, ev) => ValueChanged(s, ev);
        PrintFontSizeComboBox.SelectionChanged += (s, ev) => ValueChanged(s, ev);

        Success.PreviewMouseDown += Success_PreviewMouseDown;
        Error.PreviewMouseDown += Error_PreviewMouseDown;
        _isInitialized = true;
      }

      Error.Visibility = Visibility.Collapsed;
      Success.Visibility = Visibility.Collapsed;
      HasUnsavedChanges = false;

      LoadLanguageOptions();
      LoadThemeOptions(_baseParameterModel.Theme);

      EventAggregator.Subscribe<ThemeEvent.Change>(OnThemeChanged);
      ProtocolConfig.SaveProtocolEvent += ProtocolConfig_SaveProtocolEvent;
    }

    private void UiSettingsControl_Unloaded(object sender, RoutedEventArgs e)
    {
      EventAggregator.Unsubscribe<ThemeEvent.Change>(OnThemeChanged);
      ProtocolConfig.SaveProtocolEvent -= ProtocolConfig_SaveProtocolEvent;
    }

    private void SettingsCard_CheckedChanged(object? sender, bool e)
    {
      ValueChanged(sender, e);
    }

    /// <summary>
    /// Загружает список доступных языков интерфейса и устанавливает текущий.
    /// </summary>
    private void LoadLanguageOptions()
    {
      var cultures = LocalizationService.GetAvailableCultures();

      var options = cultures
        .Select(c => new LangOption(
            Key: c.Name,
            Title: LocalizationService.GetDisplayName(c)))
        .ToList();

      LanguageSelect.ItemsSource = options;

      var current = LanguageSettings.CurrentLanguage;
      LanguageSelect.DefaultValue = current;
      LanguageSelect.SelectedValue = current;
    }

    private void LoadThemeOptions(ThemeMode currentTheme)
    {
      var themes = Enum.GetValues(typeof(ThemeMode))
                       .Cast<ThemeMode>()
                       .Select(t => new ThemeOption(t.ToString(), t.GetDisplayName()))
                       .ToList();

      ThemeSelect.ItemsSource = themes;
      var themeString = currentTheme.ToString();
      ThemeSelect.DefaultValue = themeString;
      ThemeSelect.SelectedValue = themeString;
    }

    /// <summary>
    /// Заполняет элементы UI значениями из базовой (сохранённой) модели.
    /// </summary>
    private void DefalultData()
    {
      var current = _baseParameterModel.Language;
      LanguageSelect.DefaultValue = current;
      LanguageSelect.SelectedValue = current;

      var currentTheme = _baseParameterModel.Theme.ToString();
      ThemeSelect.DefaultValue = currentTheme;
      ThemeSelect.SelectedValue = currentTheme;

      SyntaxHighlighting.IsChecked = _baseParameterModel.UseSyntaxHighlighting;
      CommandAutoCollapsing.IsChecked = _baseParameterModel.UseCommandAutoCollapse;
      CommandBodyBackgroundHighlighting.IsChecked = _baseParameterModel.UseCommandBodyBackgroundHighlighting;
      ChainPointBodyBackgroundHighlighting.IsChecked = _baseParameterModel.UseChainPointBodyBackgroundHighlighting;
      TopMenuIcons.IsChecked = _baseParameterModel.UseTopMenuIcons;
      FillPrintFontControls();
    }

    private void FillPrintFontControls()
    {
      var fontFamilies = Fonts.SystemFontFamilies
        .Select(font => font.Source)
        .Distinct(StringComparer.CurrentCultureIgnoreCase)
        .OrderBy(font => font)
        .ToList();

      if (!fontFamilies.Contains(_baseProtocolModel.PrintFontFamily))
      {
        fontFamilies.Insert(0, _baseProtocolModel.PrintFontFamily);
      }

      PrintFontFamilyComboBox.ItemsSource = fontFamilies;
      PrintFontFamilyComboBox.SelectedItem = _baseProtocolModel.PrintFontFamily;

      double[] sizes = new double[] { 8, 9, 10, 11, 12, 14, 16, 18, 20, 24, 28, 32 };
      if (!sizes.Contains(_baseProtocolModel.PrintFontSize))
      {
        sizes = sizes.Append(_baseProtocolModel.PrintFontSize).OrderBy(size => size).ToArray();
      }

      PrintFontSizeComboBox.ItemsSource = sizes;
      PrintFontSizeComboBox.SelectedItem = _baseProtocolModel.PrintFontSize;
    }

    private void ProtocolConfig_SaveProtocolEvent(SettingsProtocolDto _)
    {
      Dispatcher.Invoke(UpdateCommandAutoCollapseVisibility);
    }

    private void UpdateCommandAutoCollapseVisibility()
    {
      bool isAvailable = ProtocolConfig.GetCommandHeadersInProtocol();
      CommandAutoCollapsing.Visibility = isAvailable ? Visibility.Visible : Visibility.Collapsed;
      CommandAutoCollapsing.IsEnabled = isAvailable;
    }

    /// <summary>
    /// Формирует модель параметров из текущих значений элементов UI.
    /// </summary>
    private UserInterfaceDto GetModel()
    {
      var languageCode =
          LanguageSelect.SelectedValue as string
          ?? LanguageSelect.SelectedItem?.ToString()
          ?? LanguageSettings.CurrentLanguage;

      var themeValue = ThemeSelect.SelectedValue as string ?? "Dark";
      var parsedTheme = Enum.TryParse<ThemeMode>(themeValue, out var theme) ? theme : ThemeMode.Dark;

      return new UserInterfaceDto
      {
        Language = languageCode,
        Theme = parsedTheme,
        UseSyntaxHighlighting = SyntaxHighlighting.IsChecked,
        UseCommandBodyBackgroundHighlighting = CommandBodyBackgroundHighlighting.IsChecked,
        UseChainPointBodyBackgroundHighlighting = ChainPointBodyBackgroundHighlighting.IsChecked,
        UseTopMenuIcons = TopMenuIcons.IsChecked,
        UseCommandAutoCollapse = CommandAutoCollapsing.IsChecked
      };
    }

    private SettingsProtocolDto GetProtocolModel()
    {
      var model = ProtocolConfig.GetProtocolModel();
      model.PrintFontFamily = PrintFontFamilyComboBox.SelectedItem?.ToString() ?? _baseProtocolModel.PrintFontFamily;
      model.PrintFontSize = PrintFontSizeComboBox.SelectedItem is double selectedSize
        ? selectedSize
        : _baseProtocolModel.PrintFontSize;

      return model;
    }

    /// <summary>
    /// Сравнивает две модели параметров.
    /// </summary>
    private static bool UserInterfaceEquals(UserInterfaceDto a, UserInterfaceDto b) =>
      a.Language == b.Language &&
      a.UseSyntaxHighlighting == b.UseSyntaxHighlighting &&
      a.UseCommandBodyBackgroundHighlighting == b.UseCommandBodyBackgroundHighlighting &&
      a.UseChainPointBodyBackgroundHighlighting == b.UseChainPointBodyBackgroundHighlighting &&
      a.UseTopMenuIcons == b.UseTopMenuIcons &&
      a.UseCommandAutoCollapse == b.UseCommandAutoCollapse &&
      b.Theme == a.Theme;

    private static bool PrintSettingsEquals(SettingsProtocolDto a, SettingsProtocolDto b) =>
      a.PrintFontFamily == b.PrintFontFamily &&
      Math.Abs(a.PrintFontSize - b.PrintFontSize) < 0.01;

    /// <summary>
    /// Обработчик события смены темы. Вызывается, когда тема меняется глобально.
    /// </summary>
    private void OnThemeChanged(Ask.Core.Services.EventCore.Events.ThemeEvent.Change e)
    {
      Theme.ThemeManager.ApplyThemeAsync(e.NewTheme);

      ThemeSelect.DefaultValue = e.NewTheme.ToString();
      ThemeSelect.SelectedValue = e.NewTheme.ToString();
    }
  }
}
