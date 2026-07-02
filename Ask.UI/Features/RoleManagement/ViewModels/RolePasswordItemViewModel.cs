using Ask.Core.Shared.Entity.Settings;
using Ask.Core.Shared.Metadata.Enums.RoleEnums;
using Ask.UI.Shared.ViewModels;

namespace Ask.UI.Features.RoleManagement.ViewModels
{
  public sealed class RolePasswordItemViewModel : ObservableObject
  {
    public RolePasswordItemViewModel(RoleCredentialModel credential)
    {
      Role = credential.Role;
      DisplayName = credential.DisplayName;
    }

    public RoleType Role { get; }

    public string DisplayName { get; }
  }
}
