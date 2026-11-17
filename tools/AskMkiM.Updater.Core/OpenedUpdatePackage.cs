using AskMkiM.Updater.Core.Models;

namespace AskMkiM.Updater.Core;

/// <summary>
/// Представляет проверенный временный архив автономного обновления.
/// </summary>
public sealed class OpenedUpdatePackage : IDisposable
{
  internal OpenedUpdatePackage(string archivePath, UpdateManifest manifest)
  {
    ArchivePath = archivePath;
    Manifest = manifest;
  }

  /// <summary>Путь к временному ZIP-архиву.</summary>
  public string ArchivePath { get; }

  /// <summary>Манифест пакета.</summary>
  public UpdateManifest Manifest { get; }

  /// <inheritdoc />
  public void Dispose()
  {
    try
    {
      if (File.Exists(ArchivePath)) File.Delete(ArchivePath);
    }
    catch
    {
      // Очистка временного каталога не влияет на результат установки.
    }
  }
}
