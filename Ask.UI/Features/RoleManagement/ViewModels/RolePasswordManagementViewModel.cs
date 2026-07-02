using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Metadata.Enums.RoleEnums;
using Ask.UI.Features.Notifications.Models;
using Ask.UI.Infrastructure.UI.Overlay.Notifications.Runtime;
using Ask.UI.Shared.Commands;
using Ask.UI.Shared.ViewModels;
using System.Collections.ObjectModel;

namespace Ask.UI.Features.RoleManagement.ViewModels
{
  public sealed class RolePasswordManagementViewModel : ObservableObject
  {
    private readonly RoleCredentialFileService _roleCredentialService;
    private RolePasswordItemViewModel? _selectedRole;
    private string _newPassword = string.Empty;
    private string _confirmPassword = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _isBusy;
    private bool _isLoaded;

    public RolePasswordManagementViewModel()
      : this(new RoleCredentialFileService())
    {
    }

    public RolePasswordManagementViewModel(RoleCredentialFileService roleCredentialService)
    {
      _roleCredentialService = roleCredentialService;
      SavePasswordCommand = new AsyncRelayCommand(SavePasswordAsync, CanSavePassword);
    }

    public event EventHandler? PasswordChangedSuccessfully;

    public ObservableCollection<RolePasswordItemViewModel> Roles { get; } = new();

    public RolePasswordItemViewModel? SelectedRole
    {
      get => _selectedRole;
      set
      {
        if (!SetProperty(ref _selectedRole, value))
        {
          return;
        }

        StatusMessage = string.Empty;
        RaiseCommandStateChanged();
      }
    }

    public string NewPassword
    {
      get => _newPassword;
      set
      {
        if (!SetProperty(ref _newPassword, value))
        {
          return;
        }

        StatusMessage = string.Empty;
        RaiseCommandStateChanged();
      }
    }

    public string ConfirmPassword
    {
      get => _confirmPassword;
      set
      {
        if (!SetProperty(ref _confirmPassword, value))
        {
          return;
        }

        StatusMessage = string.Empty;
        RaiseCommandStateChanged();
      }
    }

    public string StatusMessage
    {
      get => _statusMessage;
      private set => SetProperty(ref _statusMessage, value);
    }

    public bool IsBusy
    {
      get => _isBusy;
      private set
      {
        if (!SetProperty(ref _isBusy, value))
        {
          return;
        }

        RaisePropertyChanged(nameof(IsNotBusy));
        RaiseCommandStateChanged();
      }
    }

    public bool IsNotBusy => !IsBusy;

    public AsyncRelayCommand SavePasswordCommand { get; }

    public async Task LoadAsync()
    {
      if (_isLoaded || IsBusy)
      {
        return;
      }

      try
      {
        IsBusy = true;
        Roles.Clear();

        var currentRole = RoleAuthorizationConfig.CurrentRole;
        if (!CanManagePasswords(currentRole))
        {
          SelectedRole = null;
          StatusMessage = "Недостаточно прав для изменения паролей ролей.";
          _isLoaded = true;
          return;
        }

        var roles = await _roleCredentialService.GetManageableRolesAsync();
        foreach (var role in roles.Where(role => CanChangeRolePassword(currentRole, role.Role)))
        {
          Roles.Add(new RolePasswordItemViewModel(role));
        }

        SelectedRole = Roles.FirstOrDefault();
        StatusMessage = Roles.Count == 0 ? "Нет доступных ролей для изменения пароля." : string.Empty;
        _isLoaded = true;
      }
      catch (Exception ex)
      {
        StatusMessage = $"Не удалось загрузить роли: {ex.Message}";
        NotificationHostService.Instance.Show("Пароли ролей", StatusMessage, NotificationType.Error);
      }
      finally
      {
        IsBusy = false;
      }
    }

    public void ClearPasswordFields()
    {
      NewPassword = string.Empty;
      ConfirmPassword = string.Empty;
    }

    private async Task SavePasswordAsync()
    {
      var validationError = ValidatePasswordInput();
      if (validationError != null)
      {
        StatusMessage = validationError;
        return;
      }

      try
      {
        IsBusy = true;
        await _roleCredentialService.ChangePasswordAsync(SelectedRole!.Role, NewPassword);

        var roleName = SelectedRole.DisplayName;
        ClearPasswordFields();
        StatusMessage = $"Пароль роли \"{roleName}\" изменен.";
        PasswordChangedSuccessfully?.Invoke(this, EventArgs.Empty);

        NotificationHostService.Instance.Show(
          "Пароли ролей",
          $"Пароль роли \"{roleName}\" успешно изменен.",
          NotificationType.Success);
      }
      catch (Exception ex)
      {
        StatusMessage = $"Не удалось изменить пароль: {ex.Message}";
        NotificationHostService.Instance.Show("Ошибка изменения пароля", ex.Message, NotificationType.Error);
      }
      finally
      {
        IsBusy = false;
      }
    }

    private bool CanSavePassword()
    {
      return !IsBusy
        && SelectedRole != null
        && CanChangeRolePassword(RoleAuthorizationConfig.CurrentRole, SelectedRole.Role)
        && !string.IsNullOrWhiteSpace(NewPassword)
        && string.Equals(NewPassword, ConfirmPassword, StringComparison.Ordinal);
    }

    private string? ValidatePasswordInput()
    {
      if (!CanManagePasswords(RoleAuthorizationConfig.CurrentRole))
      {
        return "Недостаточно прав для изменения паролей ролей.";
      }

      if (SelectedRole == null)
      {
        return "Выберите роль.";
      }

      if (!CanChangeRolePassword(RoleAuthorizationConfig.CurrentRole, SelectedRole.Role))
      {
        return "Недостаточно прав для изменения пароля выбранной роли.";
      }

      if (string.IsNullOrWhiteSpace(NewPassword))
      {
        return "Введите новый пароль.";
      }

      if (!string.Equals(NewPassword, ConfirmPassword, StringComparison.Ordinal))
      {
        return "Пароль и подтверждение не совпадают.";
      }

      return null;
    }

    private static bool CanManagePasswords(RoleType? currentRole)
    {
      return currentRole is RoleType.Administrator or RoleType.Root;
    }

    private static bool CanChangeRolePassword(RoleType? currentRole, RoleType targetRole)
    {
      return currentRole switch
      {
        RoleType.Root => true,
        RoleType.Administrator => targetRole != RoleType.Root,
        _ => false,
      };
    }

    private void RaiseCommandStateChanged()
    {
      SavePasswordCommand.RaiseCanExecuteChanged();
    }
  }
}
