using Ask.Core.Services.App;

namespace Ask.UI.Features.Archive.Application
{
  public static class ArchiveOperationServices
  {
    private static readonly Lazy<IArchiveOperationService> Fallback = new Lazy<IArchiveOperationService>(
      () => new ArchiveOperationService(
        new RoleArchivePermissionService(),
        new ArchiveIntegrityService(),
        new ArchiveOperationLogger()));

    public static IArchiveOperationService Current
    {
      get
      {
        try
        {
          return ServiceLocator.TryGet<IArchiveOperationService>() ?? Fallback.Value;
        }
        catch (InvalidOperationException)
        {
          return Fallback.Value;
        }
      }
    }
  }
}
