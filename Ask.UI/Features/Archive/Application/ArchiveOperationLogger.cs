using static Ask.LogLib.LoggerUtility;

namespace Ask.UI.Features.Archive.Application
{
  public sealed class ArchiveOperationLogger : IArchiveOperationLogger
  {
    public void Started(ArchiveOperationKind operationKind, string operationName)
    {
      LogInformation($"Archive API: начата операция '{operationName}' ({operationKind}).");
    }

    public void Succeeded(ArchiveOperationKind operationKind, string operationName)
    {
      LogInformation($"Archive API: операция '{operationName}' ({operationKind}) выполнена.");
    }

    public void Failed(ArchiveOperationKind operationKind, string operationName, Exception exception)
    {
      LogError($"Archive API: ошибка операции '{operationName}' ({operationKind}): {exception}");
    }
  }
}
