using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.Base;
using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Shared.DTO.Settings;
using Ask.Core.Shared.Metadata.Enums.RoleEnums;
using Ask.Core.Shared.Metadata.Enums.UiEnums;
using Ask.Core.Shared.Metadata.View.EditorHost;
using Ask.UI.Controls.ErrorList;
using Ask.UI.Infrastructure.UI.Overlay.Drawer.Runtime;
using Ask.UI.Shared.Components.Icons;
using ConsoleUI.ConsoleLogic;
using MainWindowProgram.Engine;
using MainWindowProgram.HotkeyBindings;
using MainWindowProgram.Init;
using MainWindowProgram.ViewModels;
using Message;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using UI.Components;
using static Ask.LogLib.LoggerUtility;
using System.Windows.Media.Media3D;

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
    /// <summary>
    /// ViewModel главного окна, содержащая команды, свойства и логику привязки данных.
    /// Связывает интерфейс с бизнес-логикой.
    /// </summary>
    private readonly MainWindowViewModel _viewModel;
    private bool _isThemeToggleInProgress;
    private bool _isUserSwitchInProgress;
    private readonly Dictionary<RoleType, MultiWindowWorkspaceSession> _workspaceSessionsByRole = new();
    private Action<UserInterfaceDto>? _onUserInterfaceSaved;

    /// <summary>
    /// Показывает, активен ли в данный момент текстовый редактор.
    /// Значение true означает, что текстовый редактор находится в фокусе или используется пользователем.
    /// </summary>
    public bool IsTextEditorActive { get; set; }

    /// <summary>
    /// Сервис управления многооконным интерфейсом.
    /// </summary>

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
      InitializeTodayTasks();
      InitializeAutoLock();

      this.Visibility = Visibility.Hidden;
      InputManager.Current.PreProcessInput += InputManager_PreProcessInput;

      var vm = AppServices.Build(this);
      _viewModel = vm;

      StatusBar.DataContext = _statusBarViewModel;
      _statusBarViewModel.GetActiveEditor = () => MultiWindow.GetActiveTextEditor();

      this.DataContext = _viewModel;
      GuiInitializer.Apply(this);
      UpdateCurrentUserBadge();

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
      lifecycle.Initialize(this, _statusBarViewModel);

      new CommandLineParser().ProcessCommandLineArgs();
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
          BindingOperations.SetBinding(
            icon,
            FileDocumentIcon.IsHoveringProperty,
            new Binding("IsMouseOver")
            {
              Source = File,
              Mode = BindingMode.OneWay,
            });
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
          BindingOperations.SetBinding(
            testIcon,
            TestFlaskIcon.IsHoveringProperty,
            new Binding("IsMouseOver")
            {
              Source = TestMenu,
              Mode = BindingMode.OneWay,
            });
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
          BindingOperations.SetBinding(
            executionIcon,
            ExecutionPlayCircleIcon.IsHoveringProperty,
            new Binding("IsMouseOver")
            {
              Source = Translation,
              Mode = BindingMode.OneWay,
            });
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
          BindingOperations.SetBinding(
            settingsIcon,
            SettingsGearIcon.IsHoveringProperty,
            new Binding("IsMouseOver")
            {
              Source = Settings,
              Mode = BindingMode.OneWay,
            });
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
        BindingOperations.SetBinding(
          helpIcon,
          HelpCircleIcon.IsHoveringProperty,
          new Binding("IsMouseOver")
          {
            Source = HelpText,
            Mode = BindingMode.OneWay,
          });
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

    private void TerminalButton_Click(object sender, RoutedEventArgs e)
    {
      if (DrawerHostService.Instance.ShouldBlockGlobalInput)
      {
        return;
      }

      ConsoleVisibilityController.ToggleConsole();
      e.Handled = true;
    }

    private void TopPanel_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      if (e.ChangedButton != MouseButton.Left || e.ClickCount != 1)
      {
        return;
      }

      if (IsInteractiveTitleBarElement(e.OriginalSource as DependencyObject))
      {
        return;
      }

      try
      {
        DragMove();
        e.Handled = true;
      }
      catch (InvalidOperationException)
      {
        // Ignore failed drag initiation and let the click complete normally.
      }
    }

    private bool IsInteractiveTitleBarElement(DependencyObject? source)
    {
      for (var current = source; current != null && current != TopPanel; current = GetParent(current))
      {
        if (current is ButtonBase
            || current is MenuItem
            || current is TextBoxBase
            || current is PasswordBox
            || current is Selector
            || current is ScrollBar
            || current is Slider
            || current is Thumb
            || current is Hyperlink)
        {
          return true;
        }
      }

      return false;
    }

    private static DependencyObject? GetParent(DependencyObject current)
    {
      if (current is Visual || current is Visual3D)
      {
        return VisualTreeHelper.GetParent(current);
      }

      return current switch
      {
        FrameworkContentElement frameworkContentElement => frameworkContentElement.Parent,
        ContentElement contentElement => ContentOperations.GetParent(contentElement),
        _ => null,
      };
    }

    private async void CurrentUserButton_Click(object sender, RoutedEventArgs e)
    {
      await SwitchCurrentUserAsync();
    }

    private async Task SwitchCurrentUserAsync()
    {
      if (DrawerHostService.Instance.ShouldBlockGlobalInput ||
          SystemStateManager.GetIsLocked() ||
          _isUserSwitchInProgress)
      {
        return;
      }

      var loginWindowManager = new RoleLoginWindowManager();
      loginWindowManager.ClosedByUser += () => Application.Current.Shutdown();
      var previousEffect = Effect;
      bool wasEnabled = IsEnabled;
      bool loginWindowShown = false;
      bool authenticated = false;

      try
      {
        _isUserSwitchInProgress = true;
        CurrentUserButton.IsEnabled = false;
        _searchPanel?.Close();


        SaveCurrentWorkspaceSession();
        Effect = new BlurEffect { Radius = 8 };

        await loginWindowManager.ShowAsync(GetRolesWithSavedWorkspaceSessions(), owner: this);
        loginWindowShown = true;
        IsEnabled = false;

        var authenticatedRole = await loginWindowManager.WaitForAuthenticationAsync();
        if (authenticatedRole == null)
        {
          await loginWindowManager.WaitForCloseAsync();
          Application.Current.Shutdown();
          return;
        }

        RoleApplicationConfigurator.Apply(authenticatedRole);

        AdminConfig.SetAdminRights(authenticatedRole.Role == RoleType.Root);
        RestoreWorkspaceSession(authenticatedRole.Role);
        UpdateCurrentUserBadge();
        await loginWindowManager.CloseAsync();
        authenticated = !loginWindowManager.IsClosedByUser;
      }
      catch (Exception exception)
      {
        if (loginWindowShown)
        {
          await loginWindowManager.CloseAsync();
        }

        LogException("Ошибка смены пользователя.", exception);
        Application.Current.Shutdown();
      }
      finally
      {
        Effect = previousEffect;
        IsEnabled = wasEnabled;
        if (authenticated && wasEnabled)
        {
          if (await WindowOpacityTransition.ShowAsync(this))
          {
            Activate();
            ApplicationActivator.FlushPendingFileRequests();
          }
        }
        _isUserSwitchInProgress = false;
        if (authenticated)
        {
          _inactivityLock.Reset(System.Diagnostics.Stopwatch.GetTimestamp());
        }
        if (CurrentUserButton != null)
        {
          CurrentUserButton.IsEnabled = true;
        }
      }
    }

    private void SaveCurrentWorkspaceSession()
    {
      if (RoleAuthorizationConfig.CurrentRole == null)
      {
        return;
      }

      _workspaceSessionsByRole[RoleAuthorizationConfig.CurrentRole.Value] = MultiWindow.CaptureWorkspaceSession();
    }

    private void RestoreWorkspaceSession(RoleType role)
    {
      var session = _workspaceSessionsByRole.TryGetValue(role, out var savedSession)
        ? savedSession
        : MultiWindowWorkspaceSession.CreateEmpty();

      MultiWindow.RestoreWorkspaceSession(session);
    }

    private IReadOnlySet<RoleType> GetRolesWithSavedWorkspaceSessions()
    {
      return _workspaceSessionsByRole
        .Where(session => !session.Value.IsEmpty)
        .Select(session => session.Key)
        .ToHashSet();
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

    private async void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
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
    /// Глобальная обработка ввода и навигации по ошибкам.
    /// Работает через InputManager, поэтому охватывает все окна приложения.
    /// </summary>
    private void InputManager_PreProcessInput(object? sender, PreProcessInputEventArgs e)
    {
      if (_isUserSwitchInProgress)
      {
        e.Cancel();
        return;
      }

      if (TryHandleSwitchUserHotkey(e))
      {
        return;
      }

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

      if (e.StagingItem.Input is KeyEventArgs searchKeyArgs
          && searchKeyArgs.RoutedEvent == Keyboard.KeyDownEvent
          && searchKeyArgs.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
      {
        ShowSearchPanel();
        searchKeyArgs.Handled = true;
        return;
      }

      if (e.StagingItem.Input is not KeyEventArgs globalKeyArgs || globalKeyArgs.RoutedEvent != Keyboard.KeyDownEvent)
      {
        return;
      }

      if (_searchPanel?.IsVisible == true)
      {
        if (globalKeyArgs.Key == Key.Escape && Keyboard.FocusedElement is not (MenuItem or System.Windows.Controls.ContextMenu))
        {
          _searchPanel.CloseAnimated();
          globalKeyArgs.Handled = true;
          return;
        }
        if (globalKeyArgs.Key == Key.F3
            && (Keyboard.Modifiers == ModifierKeys.None || Keyboard.Modifiers == ModifierKeys.Shift))
        {
          _ = _searchPanel.RunSearchAsync(Keyboard.Modifiers == ModifierKeys.Shift
            ? Ask.Core.Shared.Interfaces.UiInterfaces.TextSearchAction.Previous
            : Ask.Core.Shared.Interfaces.UiInterfaces.TextSearchAction.Next);
          globalKeyArgs.Handled = true;
          return;
        }
      }

      var globalModifiers = Keyboard.Modifiers;
      var globalPressedKey = globalKeyArgs.SystemKey == Key.None ? globalKeyArgs.Key : globalKeyArgs.SystemKey;

      if (globalPressedKey == Key.F8
          && (globalModifiers == ModifierKeys.None || globalModifiers == ModifierKeys.Shift)
          && ErrorListControl.TryNavigatePreferredIssue(globalModifiers == ModifierKeys.Shift ? -1 : 1))
      {
        globalKeyArgs.Handled = true;
      }

    }

    private bool TryHandleSwitchUserHotkey(PreProcessInputEventArgs e)
    {
      if (e.StagingItem.Input is not KeyEventArgs keyArgs ||
          keyArgs.RoutedEvent != Keyboard.KeyDownEvent)
      {
        return false;
      }

      var pressedKey = keyArgs.SystemKey == Key.None ? keyArgs.Key : keyArgs.SystemKey;
      if (pressedKey != Key.L || Keyboard.Modifiers != ModifierKeys.Alt)
      {
        return false;
      }

      keyArgs.Handled = true;

      if (DrawerHostService.Instance.ShouldBlockGlobalInput ||
          SystemStateManager.GetIsLocked())
      {
        return true;
      }

      _ = SwitchCurrentUserAsync();
      return true;
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

    private void UpdateCurrentUserBadge()
    {
      if (CurrentUserNameTextBlock == null)
      {
        return;
      }

      CurrentUserNameTextBlock.Text = string.IsNullOrWhiteSpace(RoleAuthorizationConfig.CurrentRoleDisplayName)
        ? "Пользователь"
        : RoleAuthorizationConfig.CurrentRoleDisplayName;
    }
  }
}
