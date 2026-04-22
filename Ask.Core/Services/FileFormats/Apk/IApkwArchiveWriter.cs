namespace Ask.Core.Services.FileFormats.Apk
{
  /// <summary>
  /// Provides the APK converter with archive writing operations without tying core conversion to UI archive services.
  /// </summary>
  public interface IApkwArchiveWriter : IDisposable
  {
    string CreateArchive(string archiveName);

    void OpenArchive(string archivePath);

    void AddFileToOpenedArchive(string filePath);
  }
}
