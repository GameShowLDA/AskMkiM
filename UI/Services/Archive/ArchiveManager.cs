using Ask.Core.Shared.Metadata.Static;
using System.IO;
using System.Reflection.Metadata;

namespace UI.Services.Archive
{
  public sealed class ArchiveManager : IDisposable
  {
    private readonly ArchiveCreationService _archiveCreation = new ArchiveCreationService();
    private readonly ArchiveFileManager _archiveFileAdder = new ArchiveFileManager();
    private readonly ArchiveOpeningService _archiveOpening = new ArchiveOpeningService();

    public string OpenedArchivePath => _archiveOpening.OpenedArchivePath;
    private string _archivePath = Path.Combine(AppContext.BaseDirectory, FileLocations.ArchiveDirectory);

    public IReadOnlyList<string> IntegrityNotifications => _archiveOpening.IntegrityNotifications;

    public string CreateArchive(string archiveName)
    {
      return _archiveCreation.Create(archiveName);
    }

    public void OpenArchive(string archivePath)
    {
      _archiveOpening.Open(archivePath);
    }

    public string GetArchivePath()
    {
      return _archivePath;
    }

    public IReadOnlyList<string> GetFileList()
    {
      return _archiveOpening.GetFileList();
    }

    public string GetFileText(string archiveEntryName)
    {
      return _archiveOpening.GetFileText(archiveEntryName);
    }

    public void AddFileToOpenedArchive(string filePath)
    {
      var openedArchivePath = EnsureArchiveIsOpen();
      _archiveOpening.Close();

      try
      {
        _archiveFileAdder.AddFile(openedArchivePath, filePath);
      }
      finally
      {
        if (System.IO.File.Exists(openedArchivePath))
        {
          _archiveOpening.Open(openedArchivePath);
        }
      }
    }

    public void AddFileToArchive(List<List<string>> sourceLines, string archivePath, string fileName)
    {
      var openedArchivePath = EnsureArchiveIsOpen();
      _archiveOpening.Close();

      try
      {
        _archiveFileAdder.AddFile(sourceLines, archivePath, fileName);
      }
      finally
      {
        if (System.IO.File.Exists(openedArchivePath))
        {
          _archiveOpening.Open(openedArchivePath);
        }
      }
    }

    public void DeleteFileFromOpenedArchive(string archiveEntryName)
    {
      var openedArchivePath = EnsureArchiveIsOpen();
      _archiveOpening.Close();

      try
      {
        _archiveFileAdder.DeleteFile(openedArchivePath, archiveEntryName);
      }
      finally
      {
        if (System.IO.File.Exists(openedArchivePath))
        {
          _archiveOpening.Open(openedArchivePath);
        }
      }
    }

    public void DeleteArchive(string archivePath = null)
    {
      var pathToDelete = string.IsNullOrWhiteSpace(archivePath)
        ? EnsureArchiveIsOpen()
        : archivePath;

      if (!string.IsNullOrWhiteSpace(OpenedArchivePath) &&
          string.Equals(
            OpenedArchivePath,
            System.IO.Path.GetFullPath(pathToDelete),
            StringComparison.OrdinalIgnoreCase))
      {
        _archiveOpening.Close();
      }

      _archiveFileAdder.DeleteArchive(pathToDelete);
    }

    public void CloseArchive()
    {
      _archiveOpening.Close();
    }

    public void Dispose()
    {
      _archiveOpening.Dispose();
    }

    private string EnsureArchiveIsOpen()
    {
      if (string.IsNullOrWhiteSpace(OpenedArchivePath))
      {
        throw new InvalidOperationException("Archive is not open.");
      }

      return OpenedArchivePath;
    }
  }
}
