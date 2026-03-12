using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.Base;
using Ask.Core.Services.Usb;
using Ask.Core.Shared.Entity.Settings;
using Ask.Core.Shared.Metadata.Enums.UiEnums;
using Ask.Core.Shared.Metadata.View;
using Ask.Core.Shared.Metadata.View.EditorHost;
using Ask.UI.Shared.Components.Icons;
using Ask.UI.Infrastructure.UI.Overlay.Drawer.Runtime;
using ConsoleUI.ConsoleLogic;
using MainWindowProgram.Engine;
using MainWindowProgram.HotkeyBindings;
using MainWindowProgram.Services;
using MainWindowProgram.ViewModels;
using Message;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using UI.Controls.Search;
using static Ask.LogLib.LoggerUtility;


namespace MainWindowProgram
{
  public partial class MainWindow : Window
  {
    #region Поля.

    /// <summary>
    /// Обработчик сообщений, передаёт текст в интерфейс через связанный блок информации.
    /// Используется для отображения логов, статусов, ошибок и другой информации в UI.
    /// </summary>
    internal MessageHandler messageHandler { get; set; }

    /// <summary>
    /// Статический UI-элемент, в который выводятся сообщения от системы.
    /// Используется как главный информационный блок в окне приложения.
    /// </summary>
    internal static TextBlock _infoBlock;

    /// <summary>
    /// Флаг, указывающий, заблокировано ли текущее состояние интерфейса.
    /// Используется для предотвращения повторных операций, если процесс уже запущен.
    /// </summary>
    internal bool IsLocked = false;

    /// <summary>
    /// Сервис управления USB-устройствами.
    /// Обеспечивает обнаружение, мониторинг и реакцию на USB-события.
    /// </summary>
    private readonly IUsbMonitorView _usbServices;

    /// <summary>
    /// ViewModel главного окна, содержащая команды, свойства и логику привязки данных.
    /// Связывает интерфейс с бизнес-логикой.
    /// </summary>
    private readonly MainWindowViewModel _viewModel;
    private bool _isThemeToggleInProgress;
    private Action<UserInterfaceModel>? _onUserInterfaceSaved;

    /// <summary>
    /// Показывает, активен ли в данный момент текстовый редактор.
    /// Значение true означает, что текстовый редактор находится в фокусе или используется пользователем.
    /// </summary>
    public bool IsTextEditorActive { get; set; }

    /// <summary>
    /// Сервис управления многооконным интерфейсом.
    /// </summary>
    public SearchWindow SearchWindow;

    private readonly TextEditorStatusViewModel _statusBarViewModel = new();

    public IRunService RunService => MultiWindow.RunService;
    public IEditorDocumentService EditorDocumentService => MultiWindow.EditorDocumentService;
    public IProtocolViewerService ProtocolViewerService => MultiWindow.ProtocolViewerService;
    public IWorkspaceService WorkspaceService => MultiWindow.WorkspaceService;
    public ITranslationService TranslationService => MultiWindow.TranslationService;


    #endregion

    /// <summary>
    /// Инициализирует новое окно приложения и настраивает события, командную строку, мониторинг USB и конфигурацию.
    /// </summary>
    public MainWindow()
    {
      InitializeComponent();
      AddHandler(Keyboard.PreviewKeyDownEvent, new KeyEventHandler(MainWindow_PreviewKeyDown), true);

      this.Visibility = Visibility.Hidden;
      this.PreviewKeyDown += MainWindow_PreviewKeyDown;
      InputManager.Current.PreProcessInput += InputManager_PreProcessInput;

      (var vm, var usb) = AppServices.Build(this);
      _viewModel = vm;
      _usbServices = usb;

      StatusBar.DataContext = _statusBarViewModel;
      _statusBarViewModel.GetActiveEditor = () => MultiWindow.GetActiveTextEditor();

      this.DataContext = _viewModel;
      GuiInitializer.Apply(this);

      Loaded += MainWindow_Loaded;
      _onUserInterfaceSaved = model =>
      {
        if (!Dispatcher.CheckAccess())
        {
          Dispatcher.BeginInvoke(() => ApplyFileMenuTopIconMode(model.UseTopMenuIcons));
          return;
        }

        ApplyFileMenuTopIconMode(model.UseTopMenuIcons);
      };
      UserInterfaceConfig.SaveUserInterfaceEvent += _onUserInterfaceSaved;

      ThemeSettings.ThemeChanged += OnThemeChanged;
      UpdateThemeToggleButtons(ThemeSettings.CurrentTheme);

      this.Closed += (_, _) =>
      {
        ThemeSettings.ThemeChanged -= OnThemeChanged;
        if (_onUserInterfaceSaved != null)
        {
          UserInterfaceConfig.SaveUserInterfaceEvent -= _onUserInterfaceSaved;
        }
      };
    }

    /// <summary>
    /// Запускает асинхронную инициализацию окна.
    /// </summary>
    public async Task InitializeAsync()
    {
      var lifecycle = new ApplicationLifecycleManager();
      lifecycle.Initialize(this, _usbServices, _statusBarViewModel);

      new CommandLineParser(_usbServices).ProcessCommandLineArgs();
      ApplicationInitializer applicationInitializer = new ApplicationInitializer(messageHandler = new(_infoBlock));
      SystemStateEventAdapter.RaiseControlProgramActiveChanged(false);

      try
      {
        applicationInitializer.SubscribeToMessageEvents();

        await this.Dispatcher.InvokeAsync(() =>
        {
          HotkeyBinderManager.AttachAllHotkeys(this, this.DataContext);
        }, DispatcherPriority.Loaded);

      }
      catch (InvalidOperationException exception)
      {
        LogException($"Ошибка загрузки темы программы", exception);
        MessageBoxCustom.Show($"Ошибка загрузки темы: {exception.Message}", image: MessageBoxImage.Error);
      }
      catch (Exception ex)
      {
        LogException($"Ошибка выполнения программы", ex);
        MessageBoxCustom.Show($"Ошибка: {ex.Message}", image: MessageBoxImage.Error);
      }
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
      var uiConfig = await UserInterfaceConfig.GetParameterModel();
      ApplyFileMenuTopIconMode(uiConfig.UseTopMenuIcons);
    }

    private void ApplyFileMenuTopIconMode(bool useTopMenuIcons)
    {
      if (File != null)
      {
        var menuFileResource = TryFindResource("LS_Menu_File");
        if (useTopMenuIcons)
        {
          BindingOperations.ClearBinding(File, HeaderedItemsControl.HeaderProperty);
          File.Header = string.Empty;
          BindingOperations.ClearBinding(File, ToolTipProperty);
          File.Margin = new Thickness(6, 0, 0, 0);

          var icon = new FileDocumentIcon
          {
            Size = 22,
          };
          File.Icon = icon;

          if (menuFileResource != null)
          {
            BindingOperations.SetBinding(
              File,
              ToolTipProperty,
              new Binding("Value")
              {
                Source = menuFileResource,
                Mode = BindingMode.OneWay,
              });
          }
          else
          {
            File.ToolTip = "File";
          }
        }
        else
        {
          File.Icon = null;
          BindingOperations.ClearBinding(File, ToolTipProperty);
          File.ToolTip = null;
          File.Margin = new Thickness(0);

          if (menuFileResource != null)
          {
            BindingOperations.SetBinding(
              File,
              HeaderedItemsControl.HeaderProperty,
              new Binding("Value")
              {
                Source = menuFileResource,
                Mode = BindingMode.OneWay,
              });
          }
          else
          {
            File.Header = "File";
          }
        }
      }

      if (TestMenu != null)
      {
        var menuTestResource = TryFindResource("LS_Menu_Test");
        if (useTopMenuIcons)
        {
          BindingOperations.ClearBinding(TestMenu, HeaderedItemsControl.HeaderProperty);
          TestMenu.Header = string.Empty;
          BindingOperations.ClearBinding(TestMenu, ToolTipProperty);
          TestMenu.Margin = new Thickness(0);

          var testIcon = new TestFlaskIcon
          {
            Size = 22,
          };
          TestMenu.Icon = testIcon;

          if (menuTestResource != null)
          {
            BindingOperations.SetBinding(
              TestMenu,
              ToolTipProperty,
              new Binding("Value")
              {
                Source = menuTestResource,
                Mode = BindingMode.OneWay,
              });
          }
          else
          {
            TestMenu.ToolTip = "Tests";
          }
        }
        else
        {
          TestMenu.Icon = null;
          BindingOperations.ClearBinding(TestMenu, ToolTipProperty);
          TestMenu.ToolTip = null;
          TestMenu.Margin = new Thickness(0);

          if (menuTestResource != null)
          {
            BindingOperations.SetBinding(
              TestMenu,
              HeaderedItemsControl.HeaderProperty,
              new Binding("Value")
              {
                Source = menuTestResource,
                Mode = BindingMode.OneWay,
              });
          }
          else
          {
            TestMenu.Header = "Tests";
          }
        }
      }

      if (Translation != null)
      {
        var menuExecutionResource = TryFindResource("LS_Menu_Execution");
        if (useTopMenuIcons)
        {
          BindingOperations.ClearBinding(Translation, HeaderedItemsControl.HeaderProperty);
          Translation.Header = string.Empty;
          BindingOperations.ClearBinding(Translation, ToolTipProperty);
          Translation.Margin = new Thickness(0);

          var executionIcon = new ExecutionPlayCircleIcon
          {
            Size = 22,
          };
          Translation.Icon = executionIcon;

          if (menuExecutionResource != null)
          {
            BindingOperations.SetBinding(
              Translation,
              ToolTipProperty,
              new Binding("Value")
              {
                Source = menuExecutionResource,
                Mode = BindingMode.OneWay,
              });
          }
          else
          {
            Translation.ToolTip = "Execution";
          }
        }
        else
        {
          Translation.Icon = null;
          BindingOperations.ClearBinding(Translation, ToolTipProperty);
          Translation.ToolTip = null;
          Translation.Margin = new Thickness(0);

          if (menuExecutionResource != null)
          {
            BindingOperations.SetBinding(
              Translation,
              HeaderedItemsControl.HeaderProperty,
              new Binding("Value")
              {
                Source = menuExecutionResource,
                Mode = BindingMode.OneWay,
              });
          }
          else
          {
            Translation.Header = "Execution";
          }
        }
      }

      if (Settings != null)
      {
        var menuSettingsResource = TryFindResource("LS_Menu_Settings");
        if (useTopMenuIcons)
        {
          BindingOperations.ClearBinding(Settings, HeaderedItemsControl.HeaderProperty);
          Settings.Header = string.Empty;
          BindingOperations.ClearBinding(Settings, ToolTipProperty);
          Settings.Margin = new Thickness(0);

          var settingsIcon = new SettingsGearIcon
          {
            Size = 22,
          };
          Settings.Icon = settingsIcon;

          if (menuSettingsResource != null)
          {
            BindingOperations.SetBinding(
              Settings,
              ToolTipProperty,
              new Binding("Value")
              {
                Source = menuSettingsResource,
                Mode = BindingMode.OneWay,
              });
          }
          else
          {
            Settings.ToolTip = "Settings";
          }
        }
        else
        {
          Settings.Icon = null;
          BindingOperations.ClearBinding(Settings, ToolTipProperty);
          Settings.ToolTip = null;
          Settings.Margin = new Thickness(0);

          if (menuSettingsResource != null)
          {
            BindingOperations.SetBinding(
              Settings,
              HeaderedItemsControl.HeaderProperty,
              new Binding("Value")
              {
                Source = menuSettingsResource,
                Mode = BindingMode.OneWay,
              });
          }
          else
          {
            Settings.Header = "Settings";
          }
        }
      }

      if (HelpText == null)
      {
        return;
      }

      var menuHelpResource = TryFindResource("LS_Menu_Help");
      if (useTopMenuIcons)
      {
        BindingOperations.ClearBinding(HelpText, HeaderedItemsControl.HeaderProperty);
        HelpText.Header = string.Empty;
        BindingOperations.ClearBinding(HelpText, ToolTipProperty);
        HelpText.Margin = new Thickness(0);

        var helpIcon = new HelpCircleIcon
        {
          Size = 22,
        };
        HelpText.Icon = helpIcon;

        if (menuHelpResource != null)
        {
          BindingOperations.SetBinding(
            HelpText,
            ToolTipProperty,
            new Binding("Value")
            {
              Source = menuHelpResource,
              Mode = BindingMode.OneWay,
            });
        }
        else
        {
          HelpText.ToolTip = "Help";
        }

        return;
      }

      HelpText.Icon = null;
      BindingOperations.ClearBinding(HelpText, ToolTipProperty);
      HelpText.ToolTip = null;
      HelpText.Margin = new Thickness(0);

      if (menuHelpResource != null)
      {
        BindingOperations.SetBinding(
          HelpText,
          HeaderedItemsControl.HeaderProperty,
          new Binding("Value")
          {
            Source = menuHelpResource,
            Mode = BindingMode.OneWay,
          });
      }
      else
      {
        HelpText.Header = "Help";
      }
    }

    public WindowState WindowStateStatus
    {
      get => this.WindowState;
    }

    public void OpenFileFromExternalRequest(string filePath)
    {
      if (string.IsNullOrWhiteSpace(filePath))
      {
        return;
      }

      FileInteractionEventAdapter.RaiseOpenFileInEditorAgain(filePath);
    }

    private void ErrorMenuItem_Click(object sender, RoutedEventArgs e)
    {

    }

    private void TerminalButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (DrawerHostService.Instance.ShouldBlockGlobalInput)
      {
        return;
      }

      ConsoleVisibilityController.ToggleConsole();
      e.Handled = true;
    }

    private void OnThemeChanged(ThemeMode theme)
    {
      if (!Dispatcher.CheckAccess())
      {
        Dispatcher.BeginInvoke(() => UpdateThemeToggleButtons(theme));
        return;
      }

      UpdateThemeToggleButtons(theme);
    }

    private void UpdateThemeToggleButtons(ThemeMode theme)
    {
      if (DarkThemeButton == null || LightThemeButton == null)
      {
        return;
      }

      DarkThemeButton.Visibility = IsDarkTheme(theme)
        ? Visibility.Visible
        : Visibility.Collapsed;

      LightThemeButton.Visibility = IsLightTheme(theme)
        ? Visibility.Visible
        : Visibility.Collapsed;
    }

    private async void ThemeToggleButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      e.Handled = true;

      if (DrawerHostService.Instance.ShouldBlockGlobalInput || _isThemeToggleInProgress)
      {
        return;
      }

      var nextTheme = GetOppositeTheme(ThemeSettings.CurrentTheme);

      try
      {
        _isThemeToggleInProgress = true;

        var uiConfig = await UserInterfaceConfig.GetParameterModel();
        uiConfig.Theme = nextTheme;
        await UserInterfaceConfig.SaveProtocolModel(uiConfig);
      }
      catch (Exception exception)
      {
        LogException("Ошибка переключения темы интерфейса", exception);
        MessageBoxCustom.Show($"Ошибка переключения темы: {exception.Message}", image: MessageBoxImage.Error);
      }
      finally
      {
        _isThemeToggleInProgress = false;
      }
    }

    private static bool IsDarkTheme(ThemeMode theme) =>
      theme == ThemeMode.Dark || theme == ThemeMode.DarkCustom;

    private static bool IsLightTheme(ThemeMode theme) =>
      theme == ThemeMode.Light || theme == ThemeMode.LightCustom;

    private static ThemeMode GetOppositeTheme(ThemeMode theme) =>
      theme switch
      {
        ThemeMode.Dark => ThemeMode.Light,
        ThemeMode.Light => ThemeMode.Dark,
        ThemeMode.DarkCustom => ThemeMode.LightCustom,
        ThemeMode.LightCustom => ThemeMode.DarkCustom,
        _ => ThemeMode.Dark,
      };

    /// <summary>
    /// Глобальные хоткеи для навигации по результатам поиска (F3/Shift+F3), даже когда окно SearchWindow не в фокусе.
    /// </summary>
    private async void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (DrawerHostService.Instance.ShouldBlockGlobalInput)
      {
        return;
      }

      if (e.Key == Key.Escape && SearchWindow != null && SearchWindow.IsVisible)
      {
        SearchWindow.CloseDialog();
        e.Handled = true;
        return;
      }

      if (e.Key != Key.F3 || Keyboard.Modifiers == ModifierKeys.Control || Keyboard.Modifiers == ModifierKeys.Alt)
      {
        return;
      }

      if (SearchWindow == null)
      {
        return;
      }

      if (SearchWindow.IsVisible)
      {
        return;
      }

      var direction = Keyboard.Modifiers == ModifierKeys.Shift ? "FindPrevious" : "FindNext";
      var selectedText = MultiWindow?.GetActiveTextEditor()?.TextArea?.Selection?.GetText();

      if (!string.IsNullOrEmpty(selectedText))
      {
        e.Handled = true;
        await ShowSearchWindowAndRunSearchAsync(selectedText, direction);
        return;
      }

      SearchEventAdapter.RaiseSearchButtonPressed(direction);
      e.Handled = true;
    }

    private async Task ShowSearchWindowAndRunSearchAsync(string selectedText, string direction)
    {
      if (SearchWindow == null)
      {
        return;
      }

      if (SearchWindow.Owner == null)
      {
        SearchWindow.Owner = this;
      }

      await SearchWindow.ShowWindow(expandReplaceRow: false, focusReplaceField: false);
      SearchEventAdapter.RaiseSearchTextRequested(selectedText);
      SearchEventAdapter.RaiseSearchButtonPressed(direction);
    }

    /// <summary>
    /// Глобальный перехват Esc/F3 вне зависимости от того, где фокус (включая окно поиска).
    /// Работает через InputManager, поэтому охватывает все окна приложения.
    /// </summary>
    private void InputManager_PreProcessInput(object? sender, PreProcessInputEventArgs e)
    {
      if (DrawerHostService.Instance.ShouldBlockGlobalInput)
      {
        if (e.StagingItem.Input is KeyEventArgs drawerKeyArgs && drawerKeyArgs.RoutedEvent == Keyboard.KeyDownEvent)
        {
          var drawerKey = drawerKeyArgs.SystemKey == Key.None ? drawerKeyArgs.Key : drawerKeyArgs.SystemKey;
          if (drawerKey == Key.F4 && Keyboard.Modifiers == ModifierKeys.None)
          {
            DrawerHostService.Instance.ViewModel.Cancel();
            drawerKeyArgs.Handled = true;
            return;
          }

          if (!IsDrawerNavigationKey(drawerKey))
          {
            drawerKeyArgs.Handled = true;
          }
        }

        return;
      }

      if (SearchWindow == null || !SearchWindow.IsVisible)
      {
        return;
      }

      if (e.StagingItem.Input is not KeyEventArgs ke || ke.RoutedEvent != Keyboard.KeyDownEvent)
      {
        return;
      }

      var mods = Keyboard.Modifiers;
      bool ctrl = (mods & ModifierKeys.Control) != 0;
      bool alt = (mods & ModifierKeys.Alt) != 0;
      var pressedKey = ke.SystemKey == Key.None ? ke.Key : ke.SystemKey;

      if (ctrl && !alt && pressedKey == Key.F)
      {
        SearchWindow.CloseDialog();
        ke.Handled = true;
        return;
      }

      if (ctrl && !alt && pressedKey == Key.H)
      {
        _ = SearchWindow.ToggleReplaceRowAsync();
        ke.Handled = true;
        return;
      }

      // Пропускаем только «чистый» Ctrl (без Alt) — им заведуют другие биндинги.
      if (ctrl && !alt)
      {
        return;
      }

      if (SearchWindow.IsVisible && ke.Key == Key.Escape)
      {
        SearchWindow.CloseDialog();
        ke.Handled = true;
        return;
      }

      if (SearchWindow.IsVisible && ke.Key == Key.F3)
      {
        var selectedText = MultiWindow?.GetActiveTextEditor()?.TextArea?.Selection?.GetText();
        var direction = Keyboard.Modifiers == ModifierKeys.Shift ? "FindPrevious" : "FindNext";

        if (!string.IsNullOrEmpty(selectedText))
        {
          SearchEventAdapter.RaiseSearchTextRequested(selectedText);
        }

        SearchEventAdapter.RaiseSearchButtonPressed(direction);
        ke.Handled = true;
        return;
      }

      // Alt+R / Alt+A — локально в SearchWindow
      if (SearchWindow != null && SearchWindow.IsVisible && alt)
      {
        var key = ke.SystemKey == Key.None ? ke.Key : ke.SystemKey;

        if (key == Key.R)
        {
          _ = SearchWindow.ShowWindow(expandReplaceRow: true, focusReplaceField: true);
          SearchEventAdapter.RaiseReplaceWordButtonPressed();
          ke.Handled = true;
          return;
        }

        if (key == Key.A)
        {
          _ = SearchWindow.ShowWindow(expandReplaceRow: true, focusReplaceField: false);
          SearchEventAdapter.RaiseReplaceAllWordsButtonPressed();
          ke.Handled = true;
          return;
        }
      }
    }

    private static bool IsDrawerNavigationKey(Key key)
    {
      return key == Key.Up
             || key == Key.Down
             || key == Key.Left
             || key == Key.Right
             || key == Key.Enter
             || key == Key.Tab
             || key == Key.PageUp
             || key == Key.PageDown
             || key == Key.Home
             || key == Key.End;
    }
  }
}
