namespace Ask.UI.Features.Archive.Application
{
  public sealed class ArchiveOperationService : IArchiveOperationService
  {
    private readonly IArchivePermissionService _permissionService;
    private readonly IArchiveIntegrityService _integrityService;
    private readonly IArchiveOperationLogger _logger;

    public ArchiveOperationService(
      IArchivePermissionService permissionService,
      IArchiveIntegrityService integrityService,
      IArchiveOperationLogger logger)
    {
      _permissionService = permissionService;
      _integrityService = integrityService;
      _logger = logger;
    }

    public bool CanEditArchives => _permissionService.CanEditArchives;

    public bool CanImportReadyArchives => _permissionService.CanImportReadyArchives;

    public void EnsureCanEditArchives(ArchiveOperationKind operationKind)
    {
      _permissionService.EnsureCanEditArchives(operationKind);
    }

    public T ExecuteMutation<T>(
      ArchiveOperationKind operationKind,
      string operationName,
      Func<T> operation,
      Func<T, IEnumerable<string>>? archivePathsToValidate = null)
    {
      ArgumentNullException.ThrowIfNull(operation);

      _permissionService.EnsureCanEditArchives(operationKind);
      return Execute(operationKind, operationName, operation, archivePathsToValidate);
    }

    public void ExecuteMutation(
      ArchiveOperationKind operationKind,
      string operationName,
      Action operation,
      IEnumerable<string>? archivePathsToValidate = null)
    {
      ArgumentNullException.ThrowIfNull(operation);

      ExecuteMutation(
        operationKind,
        operationName,
        () =>
        {
          operation();
          return true;
        },
        _ => archivePathsToValidate ?? Enumerable.Empty<string>());
    }

    public T ExecuteImport<T>(
      string operationName,
      Func<T> operation,
      Func<T, IEnumerable<string>>? archivePathsToValidate = null)
    {
      ArgumentNullException.ThrowIfNull(operation);

      _permissionService.EnsureCanImportReadyArchives();
      return Execute(ArchiveOperationKind.ImportReadyArchive, operationName, operation, archivePathsToValidate);
    }

    public void ValidateArchivesAfterChange(IEnumerable<string> archivePaths)
    {
      _integrityService.ValidateArchives(archivePaths);
    }

    private T Execute<T>(
      ArchiveOperationKind operationKind,
      string operationName,
      Func<T> operation,
      Func<T, IEnumerable<string>>? archivePathsToValidate)
    {
      var displayName = string.IsNullOrWhiteSpace(operationName)
        ? operationKind.ToString()
        : operationName;

      try
      {
        _logger.Started(operationKind, displayName);
        var result = operation();
        if (archivePathsToValidate != null)
        {
          _integrityService.ValidateArchives(archivePathsToValidate(result));
        }

        _logger.Succeeded(operationKind, displayName);
        return result;
      }
      catch (Exception ex)
      {
        _logger.Failed(operationKind, displayName, ex);
        throw;
      }
    }
  }
}
