using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Entity.Settings;
using Message;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
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

    private const int SW_SHOW = 5;

    private readonly RoleCredentialFileService _roleCredentialService = new();
    private readonly TaskCompletionSource<RoleCredentialModel?> _authenticationCompletionSource =
      new(TaskCreationOptions.RunContinuationsAsynchronously);

    private bool _isStartupLoading;
    private bool _allowClose;

    /// <summary>
    /// Успешно авторизованная роль.
    /// </summary>
    public RoleCredentialModel? AuthenticatedRole { get; private set; }

    public RoleLoginWindow()
    {
      InitializeComponent();
      SourceInitialized += RoleLoginWindow_SourceInitialized;
      Loaded += RoleLoginWindow_Loaded;
      ContentRendered += RoleLoginWindow_ContentRendered;
      Closing += RoleLoginWindow_Closing;
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
      PasswordBox.SelectAll();
      BringToFront();
    }

    private void RoleLoginWindow_SourceInitialized(object? sender, EventArgs e)
    {
      BringToFront();
    }

    private async void RoleLoginWindow_Loaded(object sender, RoutedEventArgs e)
    {
      BringToFront();
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

      _authenticationCompletionSource.TrySetResult(null);
    }

    private async Task LoadRolesAsync()
    {
      try
      {
        SetStatus(string.Empty);
        LoginButton.IsEnabled = false;

        var roles = await _roleCredentialService.GetRolesAsync();
        var lastSelectedRole = await _roleCredentialService.GetLastSelectedRoleAsync();
        RolesComboBox.ItemsSource = roles;
        RolesComboBox.SelectedItem = roles.FirstOrDefault(x => x.Role == lastSelectedRole);

        if (RolesComboBox.SelectedItem == null)
        {
          RolesComboBox.SelectedIndex = roles.Count > 0 ? 0 : -1;
        }

        if (roles.Count == 0)
        {
          SetStatus("Роли для входа не найдены.");
          return;
        }

        PasswordBox.Focus();
        UpdateLoginButtonState();
      }
      catch (Exception ex)
      {
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
      if (RolesComboBox.SelectedItem is not RoleCredentialModel selectedRole)
      {
        SetStatus("Выберите роль.");
        return;
      }

      if (string.IsNullOrWhiteSpace(PasswordBox.Password))
      {
        SetStatus("Введите пароль.");
        return;
      }

      try
      {
        LoginButton.IsEnabled = false;
        SetStatus("Проверка пароля...");

        var authorizedRole = await _roleCredentialService.AuthorizeAsync(selectedRole.Role, PasswordBox.Password);
        if (authorizedRole == null)
        {
          SetStatus("Неверный пароль.");
          PasswordBox.SelectAll();
          PasswordBox.Focus();
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

    private void RolesComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
      if (_isStartupLoading)
      {
        return;
      }

      SetStatus(string.Empty);
      UpdateLoginButtonState();
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
      if (_isStartupLoading)
      {
        return;
      }

      SetStatus(string.Empty);
      UpdateLoginButtonState();
    }

    private void UpdateLoginButtonState()
    {
      if (_isStartupLoading)
      {
        LoginButton.IsEnabled = false;
        return;
      }

      LoginButton.IsEnabled = RolesComboBox.SelectedItem != null && !string.IsNullOrWhiteSpace(PasswordBox.Password);
    }

    private void SetStatus(string message)
    {
      StatusTextBlock.Text = message;
    }

    private void SetLoadingState(bool isLoading, string message)
    {
      LoginPanel.Visibility = isLoading ? Visibility.Collapsed : Visibility.Visible;
      LoadingPanel.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
      LoadingStatusTextBlock.Text = message;

      RolesComboBox.IsEnabled = !isLoading;
      PasswordBox.IsEnabled = !isLoading;
      CancelButton.IsEnabled = !isLoading;

      if (!isLoading)
      {
        SetStatus(message);
        PasswordBox.Focus();
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
          PasswordBox.Focus();
        }
      }, DispatcherPriority.ApplicationIdle);
    }
  }
}
