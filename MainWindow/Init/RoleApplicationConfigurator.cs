using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Shared.Entity.Settings;
using Ask.Core.Shared.Metadata.Enums.RoleEnums;

namespace MainWindowProgram.Init
{
  internal static class RoleApplicationConfigurator
  {
    public static void Apply(RoleCredentialModel role)
    {
      ArgumentNullException.ThrowIfNull(role);

      RoleAuthorizationConfig.SetCurrentRole(role.Role, role.DisplayName);
      AdminConfig.SetAdminRights(role.Role == RoleType.Root);

      switch (role.Role)
      {
        case RoleType.Administrator:
          SetAdminConsoleEnabled(false);
          SetTestsMenuVisible(true);
          SetDeviceConfigurationEditingEnabled(true);
          break;

        case RoleType.Root:
          SetAdminConsoleEnabled(true);
          SetTestsMenuVisible(true);
          SetDeviceConfigurationEditingEnabled(true);
          break;

        case RoleType.Adjuster:
          SetAdminConsoleEnabled(false);
          SetTestsMenuVisible(true);
          SetDeviceConfigurationEditingEnabled(false);
          break;

        case RoleType.Developer:
          SetAdminConsoleEnabled(false);
          SetTestsMenuVisible(false);
          SetDeviceConfigurationEditingEnabled(false);
          break;

        default:
          SetAdminConsoleEnabled(false);
          SetTestsMenuVisible(true);
          SetDeviceConfigurationEditingEnabled(false);
          break;
      }
    }

    public static void SetAdminConsoleEnabled(bool enabled)
    {
      SystemStateEventAdapter.RaiseConsoleAccessChanged(enabled);
    }

    public static void SetTestsMenuVisible(bool visible)
    {
      SystemStateEventAdapter.RaiseTestsMenuVisibilityChanged(visible);
    }

    public static void SetDeviceConfigurationEditingEnabled(bool enabled)
    {
      SystemStateEventAdapter.RaiseDeviceConfigurationEditingAccessChanged(enabled);
    }
  }
}
