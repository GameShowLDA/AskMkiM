using Ask.Core.Services.FileFormats.Apk;
using Ask.UI.Features.Archive.Services;

namespace MainWindowProgram.Services.Conversion
{
  internal sealed class ApkwArchiveWriter : IApkwArchiveWriter
  {
    private readonly ArchiveManager _archiveManager = new ArchiveManager();

    public string CreateArchive(string archiveName)
      => _archiveManager.CreateArchive(archiveName);

    public void OpenArchive(string archivePath)
      => _archiveManager.OpenArchive(archivePath);

    public void AddFileToOpenedArchive(string filePath)
      => _archiveManager.AddFileToOpenedArchive(filePath);

    public void Dispose()
      => _archiveManager.Dispose();
  }
}
