using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Entity.Settings;
using Ask.Core.Shared.Metadata.Enums.RoleEnums;
using Message;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace MainWindowProgram
{
  /// <summary>
  /// Окно входа по роли и паролю.
  /// </summary>
  public partial class RoleLoginWindow : Window
  {
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProcDelegate lpfn, IntPtr hmod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    private const int SW_SHOW = 5;
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int VK_R = 0x52;
    private const int VK_MENU = 0x12;

    private readonly RoleCredentialFileService _roleCredentialService = new();
    private readonly IReadOnlySet<RoleType> _rolesWithSavedSessions;
    private readonly TaskCompletionSource<RoleCredentialModel?> _authenticationCompletionSource =
      new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly DispatcherTimer _keyboardLayoutTimer;
    private readonly LowLevelKeyboardProcDelegate _keyboardHookProc;

    private IntPtr _keyboardHookHandle;
    private bool _isStartupLoading;
    private bool _allowClose;
    private bool _isPasswordVisible;
    private bool _isSyncingPasswordText;
    private bool _isRootLoginSelected;

    /// <summary>
    /// Успешно авторизованная роль.
    /// </summary>
    public RoleCredentialModel? AuthenticatedRole { get; private set; }

    public RoleLoginWindow(IReadOnlySet<RoleType>? rolesWithSavedSessions = null)
    {
      InitializeComponent();
      _rolesWithSavedSessions = rolesWithSavedSessions ?? new HashSet<RoleType>();
      _keyboardHookProc = LowLevelKeyboardProc;

      _keyboardLayoutTimer = new DispatcherTimer
      {
        Interval = TimeSpan.FromMilliseconds(250),
      };

      _keyboardLayoutTimer.Tick += KeyboardLayoutTimer_Tick;

      SourceInitialized += RoleLoginWindow_SourceInitialized;
      Loaded += RoleLoginWindow_Loaded;
      ContentRendered += RoleLoginWindow_ContentRendered;
      Closing += RoleLoginWindow_Closing;
      PreviewKeyDown += RoleLoginWindow_PreviewKeyDown;
      InputLanguageManager.Current.InputLanguageChanged += Current_InputLanguageChanged;
    }

    public Task<RoleCredentialModel?> WaitForAuthenticationAsync()
    {
      return _authenticationCompletionSource.Task;
    }

    public void BeginStartupLoading(string message)
    {
      _isStartupLoading = true;
      SetLoadingState(true, message);
      BringToFront();
    }

    public void UpdateLoadingStatus(string message)
    {
      LoadingStatusTextBlock.Text = message;
    }

    public void CompleteStartupLoading()
    {
      _allowClose = true;
      Close();
    }

    public void FailStartupLoading(string message)
    {
      _isStartupLoading = false;
      SetLoadingState(false, message);
      SelectAllPassword();
      BringToFront();
    }

    private void RoleLoginWindow_SourceInitialized(object? sender, EventArgs e)
    {
      InstallKeyboardHook();
      BringToFront();
    }

    private async void RoleLoginWindow_Loaded(object sender, RoutedEventArgs e)
    {
      BringToFront();
      UpdateKeyboardLayoutIndicator();
      UpdatePasswordVisibility();
      UpdatePasswordPlaceholderVisibility();
      UpdateCapsLockWarning();
      _keyboardLayoutTimer.Start();
      await LoadRolesAsync();
      BringToFront();
    }

    private void RoleLoginWindow_ContentRendered(object? sender, EventArgs e)
    {
      BringToFront();
    }

    private void RoleLoginWindow_Closing(object? sender, CancelEventArgs e)
    {
      if (_isStartupLoading && !_allowClose)
      {
        e.Cancel = true;
        return;
      }

      _keyboardLayoutTimer.Stop();
      InputLanguageManager.Current.InputLanguageChanged -= Current_InputLanguageChanged;
      UninstallKeyboardHook();
      _authenticationCompletionSource.TrySetResult(null);
    }

    private void RoleLoginWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      var pressedKey = e.SystemKey == Key.None ? e.Key : e.SystemKey;
      if (pressedKey == Key.R && Keyboard.Modifiers == ModifierKeys.Alt && !_isStartupLoading)
      {
        HandleRootLoginHotkey();
        e.Handled = true;
        return;
      }

      if (e.Key != Key.Tab || _isStartupLoading || RolesListBox.Items.Count == 0)
      {
        return;
      }

      int direction = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? -1 : 1;
      MoveRoleSelection(direction);
      e.Handled = true;
    }

    private void InstallKeyboardHook()
    {
      if (_keyboardHookHandle != IntPtr.Zero)
      {
        return;
      }

      _keyboardHookHandle = SetWindowsHookEx(
        WH_KEYBOARD_LL,
        _keyboardHookProc,
        GetModuleHandle(null),
        0);
    }

    private void UninstallKeyboardHook()
    {
      if (_keyboardHookHandle == IntPtr.Zero)
      {
        return;
      }

      UnhookWindowsHookEx(_keyboardHookHandle);
      _keyboardHookHandle = IntPtr.Zero;
    }

    private IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
      if (nCode >= 0 && IsRootLoginHotkey(wParam, lParam))
      {
        Dispatcher.BeginInvoke(HandleRootLoginHotkey);
        return new IntPtr(1);
      }

      return CallNextHookEx(_keyboardHookHandle, nCode, wParam, lParam);
    }

    private bool IsRootLoginHotkey(IntPtr wParam, IntPtr lParam)
    {
      if (_isStartupLoading ||
          wParam != new IntPtr(WM_KEYDOWN) && wParam != new IntPtr(WM_SYSKEYDOWN))
      {
        return false;
      }

      var hookData = Marshal.PtrToStructure<Kbdllhookstruct>(lParam);
      bool altPressed = (GetAsyncKeyState(VK_MENU) & unchecked((short)0x8000)) != 0;

      return hookData.VkCode == VK_R && altPressed;
    }

    private void HandleRootLoginHotkey()
    {
      if (_isStartupLoading)
      {
        return;
      }

      SelectRootLogin();
    }

    private async Task LoadRolesAsync()
    {
      try
      {
        SetStatus(string.Empty);
        LoginButton.IsEnabled = false;

        var roles = await _roleCredentialService.GetRolesAsync();
        var lastSelectedRole = await _roleCredentialService.GetLastSelectedRoleAsync();

        var roleItems = roles
          .Select(role => new RoleLoginItem(role, _rolesWithSavedSessions.Contains(role.Role)))
          .ToList();

        RolesListBox.ItemsSource = roleItems;
        RolesListBox.SelectedItem = roleItems.FirstOrDefault(x => x.Role == lastSelectedRole);

        if (RolesListBox.SelectedItem == null)
        {
          RolesListBox.SelectedIndex = roles.Count > 0 ? 0 : -1;
        }

        if (roles.Count == 0)
        {
          SetSelectedRoleName(null);
          SetStatus("Роли для входа не найдены.");
          return;
        }

        SetSelectedRoleName((RolesListBox.SelectedItem as RoleLoginItem)?.Credential);
        UpdateLoginButtonState();
        FocusPasswordInput();
      }
      catch (Exception ex)
      {
        SetSelectedRoleName(null);
        SetStatus("Не удалось загрузить роли для входа.");
        MessageBoxCustom.Show($"Ошибка загрузки ролей: {ex.Message}", image: MessageBoxImage.Error);
      }
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
      await AuthorizeAsync();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
      _allowClose = true;
      Close();
    }

    private async Task AuthorizeAsync()
    {
      if (_isRootLoginSelected)
      {
        await AuthorizeRootAsync();
        return;
      }

      if (RolesListBox.SelectedItem is not RoleLoginItem selectedRoleItem)
      {
        SetStatus("Выберите роль.");
        return;
      }

      var selectedRole = selectedRoleItem.Credential;
      var enteredPassword = GetCurrentPassword();
      if (string.IsNullOrWhiteSpace(enteredPassword))
      {
        SetStatus("Введите пароль.");
        return;
      }

      try
      {
        LoginButton.IsEnabled = false;
        SetStatus("Проверка пароля...");

        var authorizedRole = await _roleCredentialService.AuthorizeAsync(selectedRole.Role, enteredPassword);
        if (authorizedRole == null)
        {
          SetStatus("Неверный пароль.");
          SelectAllPassword();
          FocusPasswordInput();
          UpdateLoginButtonState();
          return;
        }

        AuthenticatedRole = authorizedRole;
        BeginStartupLoading("Подготовка приложения...");
        _authenticationCompletionSource.TrySetResult(authorizedRole);
      }
      catch (Exception ex)
      {
        SetStatus("Ошибка проверки пароля.");
        MessageBoxCustom.Show($"Ошибка авторизации: {ex.Message}", image: MessageBoxImage.Error);
        UpdateLoginButtonState();
      }
    }

    private void RolesListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
      if (_isStartupLoading)
      {
        return;
      }

      _isRootLoginSelected = false;
      SetSelectedRoleName((RolesListBox.SelectedItem as RoleLoginItem)?.Credential);
      SetStatus(string.Empty);
      UpdateLoginButtonState();
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
      if (_isSyncingPasswordText)
      {
        return;
      }

      _isSyncingPasswordText = true;
      VisiblePasswordTextBox.Text = PasswordBox.Password;
      _isSyncingPasswordText = false;

      HandlePasswordEdited();
    }

    private void VisiblePasswordTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
      if (_isSyncingPasswordText)
      {
        return;
      }

      _isSyncingPasswordText = true;
      PasswordBox.Password = VisiblePasswordTextBox.Text;
      _isSyncingPasswordText = false;

      HandlePasswordEdited();
    }

    private void PasswordBox_GotFocus(object sender, RoutedEventArgs e)
    {
      UpdatePasswordPlaceholderVisibility();
      UpdateKeyboardLayoutIndicator();
      UpdateCapsLockWarning();
    }

    private void PasswordBox_LostFocus(object sender, RoutedEventArgs e)
    {
      UpdatePasswordPlaceholderVisibility();
      UpdateCapsLockWarning();
    }

    private void VisiblePasswordTextBox_GotFocus(object sender, RoutedEventArgs e)
    {
      UpdatePasswordPlaceholderVisibility();
      UpdateKeyboardLayoutIndicator();
      UpdateCapsLockWarning();
    }

    private void VisiblePasswordTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
      UpdatePasswordPlaceholderVisibility();
      UpdateCapsLockWarning();
    }

    private void TogglePasswordVisibilityButton_Click(object sender, RoutedEventArgs e)
    {
      _isPasswordVisible = !_isPasswordVisible;
      UpdatePasswordVisibility();
      FocusPasswordInput();
    }

    private void KeyboardLayoutButton_Click(object sender, RoutedEventArgs e)
    {
      ToggleKeyboardLayout();
      UpdateKeyboardLayoutIndicator();
      FocusPasswordInput();
    }

    private void KeyboardLayoutTimer_Tick(object? sender, EventArgs e)
    {
      UpdateKeyboardLayoutIndicator();
      UpdateCapsLockWarning();
    }

    private void Current_InputLanguageChanged(object sender, InputLanguageEventArgs e)
    {
      UpdateKeyboardLayoutIndicator();
    }

    private void HandlePasswordEdited()
    {
      if (_isStartupLoading)
      {
        return;
      }

      UpdatePasswordPlaceholderVisibility();
      SetStatus(string.Empty);
      UpdateLoginButtonState();
      UpdateKeyboardLayoutIndicator();
      UpdateCapsLockWarning();
    }

    private void UpdateLoginButtonState()
    {
      if (_isStartupLoading)
      {
        LoginButton.IsEnabled = false;
        return;
      }

      LoginButton.IsEnabled =
        (RolesListBox.SelectedItem != null || _isRootLoginSelected) &&
        !string.IsNullOrWhiteSpace(GetCurrentPassword());
    }

    private void SelectRootLogin()
    {
      RolesListBox.SelectedItem = null;
      _isRootLoginSelected = true;
      SelectedRoleNameTextBlock.Text = "root";
      SetStatus(string.Empty);
      UpdateLoginButtonState();
      FocusPasswordInput();
    }

    private async Task AuthorizeRootAsync()
    {
      var enteredPassword = GetCurrentPassword();
      if (string.IsNullOrWhiteSpace(enteredPassword))
      {
        SetStatus("Введите пароль.");
        return;
      }

      try
      {
        LoginButton.IsEnabled = false;
        SetStatus("Проверка пароля...");

        var authorizedRole = await _roleCredentialService.AuthorizeAsync(RoleType.Root, enteredPassword);
        if (authorizedRole == null)
        {
          SetStatus("Неверный пароль.");
          SelectAllPassword();
          FocusPasswordInput();
          UpdateLoginButtonState();
          return;
        }

        AuthenticatedRole = authorizedRole;
        BeginStartupLoading("Подготовка приложения...");
        _authenticationCompletionSource.TrySetResult(authorizedRole);

      }
      catch (Exception ex)
      {
        SetStatus("Ошибка проверки пароля.");
        MessageBoxCustom.Show($"Ошибка авторизации: {ex.Message}", image: MessageBoxImage.Error);
        UpdateLoginButtonState();
      }
    }

    private void SetStatus(string message)
    {
      StatusTextBlock.Text = message;
    }

    private void SetSelectedRoleName(RoleCredentialModel? role)
    {
      SelectedRoleNameTextBlock.Text = role?.DisplayName ?? "Выберите роль";
    }

    private void UpdatePasswordPlaceholderVisibility()
    {
      bool hasFocus = PasswordBox.IsKeyboardFocused || VisiblePasswordTextBox.IsKeyboardFocused;
      PasswordPlaceholderTextBlock.Visibility =
        string.IsNullOrEmpty(GetCurrentPassword()) && !hasFocus
          ? Visibility.Visible
          : Visibility.Collapsed;
    }

    private void UpdatePasswordVisibility()
    {
      if (_isPasswordVisible)
      {
        VisiblePasswordTextBox.Visibility = Visibility.Visible;
        PasswordBox.Visibility = Visibility.Collapsed;
        TogglePasswordVisibilityButton.Content = "\uE8F5";
        TogglePasswordVisibilityButton.ToolTip = "Скрыть пароль";
      }
      else
      {
        VisiblePasswordTextBox.Visibility = Visibility.Collapsed;
        PasswordBox.Visibility = Visibility.Visible;
        TogglePasswordVisibilityButton.Content = "\uE890";
        TogglePasswordVisibilityButton.ToolTip = "Показать пароль";
      }

      UpdatePasswordPlaceholderVisibility();
      UpdateCapsLockWarning();
    }

    private void UpdateCapsLockWarning()
    {
      bool hasPasswordFocus = PasswordBox.IsKeyboardFocused || VisiblePasswordTextBox.IsKeyboardFocused;
      CapsLockWarningTextBlock.Visibility =
        hasPasswordFocus && Keyboard.IsKeyToggled(Key.CapsLock)
          ? Visibility.Visible
          : Visibility.Collapsed;
    }

    private void UpdateKeyboardLayoutIndicator()
    {
      KeyboardLayoutTextBlock.Text = GetKeyboardLayoutCode();
      KeyboardLayoutButton.ToolTip = KeyboardLayoutTextBlock.Text == "RU"
        ? "Переключить на EN"
        : "Переключить на RU";
    }

    private static string GetKeyboardLayoutCode()
    {
      try
      {
        var culture = InputLanguageManager.Current.CurrentInputLanguage;
        if (culture == null)
        {
          return "??";
        }

        return culture.TwoLetterISOLanguageName.ToUpper(CultureInfo.InvariantCulture);
      }
      catch
      {
        return "??";
      }
    }

    private static void ToggleKeyboardLayout()
    {
      try
      {
        var current = GetKeyboardLayoutCode();
        var targetCulture = current == "RU"
          ? CultureInfo.GetCultureInfo("en-US")
          : CultureInfo.GetCultureInfo("ru-RU");

        InputLanguageManager.Current.CurrentInputLanguage = targetCulture;
      }
      catch
      {
        // Игнорируем невозможность переключить раскладку и оставляем текущую.
      }
    }

    private void MoveRoleSelection(int direction)
    {
      if (RolesListBox.Items.Count == 0)
      {
        return;
      }

      int currentIndex = RolesListBox.SelectedIndex;
      if (currentIndex < 0)
      {
        currentIndex = 0;
      }

      int nextIndex = (currentIndex + direction + RolesListBox.Items.Count) % RolesListBox.Items.Count;
      RolesListBox.SelectedIndex = nextIndex;
      RolesListBox.ScrollIntoView(RolesListBox.SelectedItem);
      FocusPasswordInput();
    }

    private string GetCurrentPassword()
    {
      return _isPasswordVisible ? VisiblePasswordTextBox.Text : PasswordBox.Password;
    }

    private void SelectAllPassword()
    {
      if (_isPasswordVisible)
      {
        VisiblePasswordTextBox.SelectAll();
      }
      else
      {
        PasswordBox.SelectAll();
      }
    }

    private void FocusPasswordInput()
    {
      if (_isPasswordVisible)
      {
        VisiblePasswordTextBox.Focus();
        VisiblePasswordTextBox.CaretIndex = VisiblePasswordTextBox.Text.Length;
      }
      else
      {
        PasswordBox.Focus();
      }
    }

    private void SetLoadingState(bool isLoading, string message)
    {
      LoginPanel.Visibility = isLoading ? Visibility.Collapsed : Visibility.Visible;
      LoadingPanel.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
      LoadingStatusTextBlock.Text = message;

      RolesListBox.IsEnabled = !isLoading;
      PasswordBox.IsEnabled = !isLoading;
      VisiblePasswordTextBox.IsEnabled = !isLoading;
      TogglePasswordVisibilityButton.IsEnabled = !isLoading;
      CancelButton.IsEnabled = !isLoading;

      if (!isLoading)
      {
        UpdatePasswordPlaceholderVisibility();
        SetStatus(message);
        FocusPasswordInput();
        UpdateLoginButtonState();
      }
    }

    private void BringToFront()
    {
      if (!IsVisible)
      {
        Show();
      }

      Activate();
      Topmost = true;

      var handle = new WindowInteropHelper(this).Handle;
      if (handle != IntPtr.Zero)
      {
        ShowWindow(handle, SW_SHOW);
        BringWindowToTop(handle);
        SetForegroundWindow(handle);
      }

      Dispatcher.BeginInvoke(() =>
      {
        Topmost = false;
        Activate();

        if (!_isStartupLoading)
        {
          UpdateKeyboardLayoutIndicator();
          UpdatePasswordPlaceholderVisibility();
          FocusPasswordInput();
        }
      }, DispatcherPriority.ApplicationIdle);
    }

    private sealed class RoleLoginItem
    {
      public RoleCredentialModel Credential { get; }

      public RoleType Role => Credential.Role;

      public string DisplayText { get; }

      public RoleLoginItem(RoleCredentialModel credential, bool hasSavedSession)
      {
        Credential = credential;
        DisplayText = hasSavedSession
          ? $"{credential.DisplayName} (выполнен вход)"
          : credential.DisplayName;
      }
    }

    private delegate IntPtr LowLevelKeyboardProcDelegate(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct Kbdllhookstruct
    {
      public int VkCode;
      public int ScanCode;
      public int Flags;
      public int Time;
      public IntPtr ExtraInfo;
    }
  }
}
