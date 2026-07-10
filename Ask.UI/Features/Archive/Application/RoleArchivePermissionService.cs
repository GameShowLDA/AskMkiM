using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Metadata.Enums.RoleEnums;

namespace Ask.UI.Features.Archive.Application
{
  public sealed class RoleArchivePermissionService : IArchivePermissionService
  {
    public bool CanEditArchives =>
      RoleAuthorizationConfig.CurrentRole is RoleType.Administrator or RoleType.Root or RoleType.Developer;

    public bool CanImportReadyArchives => true;

    public void EnsureCanEditArchives(ArchiveOperationKind operationKind)
    {
      if (CanEditArchives)
      {
        return;
      }

      throw new UnauthorizedAccessException(
        "Недостаточно прав для изменения архивов. Редактирование, создание и удаление доступны только Администратору, Root и Разработчику ПК.");
    }

    public void EnsureCanImportReadyArchives()
    {
      if (CanImportReadyArchives)
      {
        return;
      }

      throw new UnauthorizedAccessException("Недостаточно прав для загрузки готового архива.");
    }
  }
}
