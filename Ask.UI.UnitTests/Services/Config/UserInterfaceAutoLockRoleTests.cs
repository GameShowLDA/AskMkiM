using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.Base;
using Ask.Core.Shared.DTO.Settings;
using Ask.Core.Shared.Metadata.Enums.RoleEnums;

namespace Ask.UI.UnitTests.Services.Config;

public sealed class UserInterfaceAutoLockRoleTests : IAsyncLifetime
{
  private UserInterfaceDto _original = null!;
  private readonly RoleType? _role = RoleAuthorizationConfig.CurrentRole;
  private readonly string _displayName = RoleAuthorizationConfig.CurrentRoleDisplayName;
  private readonly Func<UserInterfaceDto, Task>? _save = UserInterfaceConfig.SaveUserInterfaceAsyncEvent;

  public UserInterfaceAutoLockRoleTests() => UserInterfaceConfig.SaveUserInterfaceAsyncEvent = null;

  public async Task InitializeAsync() => _original = await UserInterfaceConfig.GetParameterModel();

  [Theory]
  [InlineData(RoleType.Administrator, 5)]
  [InlineData(RoleType.Developer, 5)]
  [InlineData(RoleType.Adjuster, 0)]
  [InlineData(RoleType.Root, 0)]
  public async Task MissingRoleSettingUsesDefaultInsteadOfLegacyCommonValue(RoleType role, int expected)
  {
    await UserInterfaceConfig.SetUserInterfaceModel(new UserInterfaceDto { AutoLockMinutes = 10 });
    RoleAuthorizationConfig.SetCurrentRole(role, role.ToString());
    Assert.Equal(expected, UserInterfaceConfig.GetAutoLockMinutes());
    Assert.Equal(expected, (await UserInterfaceConfig.GetParameterModel()).AutoLockMinutes);
  }

  [Fact]
  public async Task SavingCurrentRolePreservesOtherRolesAndSurvivesReload()
  {
    await UserInterfaceConfig.SetUserInterfaceModel(new UserInterfaceDto());
    UserInterfaceDto? persisted = null;
    UserInterfaceConfig.SaveUserInterfaceAsyncEvent = model =>
    {
      persisted = model;
      return Task.CompletedTask;
    };
    RoleAuthorizationConfig.SetCurrentRole(RoleType.Administrator, "Администратор");
    await UserInterfaceConfig.SaveProtocolModel(new UserInterfaceDto { AutoLockMinutes = 0 });
    RoleAuthorizationConfig.SetCurrentRole(RoleType.Developer, "Разработчик");
    Assert.Equal(5, UserInterfaceConfig.GetAutoLockMinutes());
    await UserInterfaceConfig.SaveProtocolModel(new UserInterfaceDto { AutoLockMinutes = 2 });
    RoleAuthorizationConfig.SetCurrentRole(RoleType.Adjuster, "Регулировщик");
    Assert.Equal(0, UserInterfaceConfig.GetAutoLockMinutes());
    await UserInterfaceConfig.SaveProtocolModel(new UserInterfaceDto { AutoLockMinutes = 10 });
    Assert.NotNull(persisted);
    Assert.Equal(0, persisted.AdministratorAutoLockMinutes);
    Assert.Equal(2, persisted.DeveloperAutoLockMinutes);
    Assert.Equal(10, persisted.AdjusterAutoLockMinutes);
    await UserInterfaceConfig.SetUserInterfaceModel(new UserInterfaceDto());
    await UserInterfaceConfig.SetUserInterfaceModel(persisted);
    foreach (var (role, minutes) in new[] { (RoleType.Administrator, 0), (RoleType.Developer, 2), (RoleType.Adjuster, 10) })
    {
      RoleAuthorizationConfig.SetCurrentRole(role, role.ToString());
      Assert.Equal(minutes, UserInterfaceConfig.GetAutoLockMinutes());
    }
  }

  [Fact]
  public async Task InvalidSavedIntervalIsNormalizedAndNoRoleDisablesLock()
  {
    await UserInterfaceConfig.SetUserInterfaceModel(new UserInterfaceDto { AdministratorAutoLockMinutes = -1 });
    RoleAuthorizationConfig.SetCurrentRole(RoleType.Administrator, "Администратор");
    Assert.Equal(0, UserInterfaceConfig.GetAutoLockMinutes());
    RoleAuthorizationConfig.Clear();
    Assert.Equal(0, UserInterfaceConfig.GetAutoLockMinutes());
  }

  public async Task DisposeAsync()
  {
    UserInterfaceConfig.SaveUserInterfaceAsyncEvent = _save;
    await UserInterfaceConfig.SetUserInterfaceModel(_original);
    if (_role is RoleType role) RoleAuthorizationConfig.SetCurrentRole(role, _displayName);
    else RoleAuthorizationConfig.Clear();
  }
}
