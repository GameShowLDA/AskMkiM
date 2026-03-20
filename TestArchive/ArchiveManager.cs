using System;
using System.Collections.Generic;

namespace TestArchive
{
  internal sealed class ArchiveManager : IDisposable
  {
    private readonly ArchiveCreation _archiveCreation = new ArchiveCreation();
    private readonly ArchiveFileAdder _archiveFileAdder = new ArchiveFileAdder();
    private readonly ArchiveOpening _archiveOpening = new ArchiveOpening();

    public string OpenedArchivePath => _archiveOpening.OpenedArchivePath;
    public IReadOnlyList<string> IntegrityNotifications => _archiveOpening.IntegrityNotifications;

    public string CreateArchive(string archiveName)
    {
      return _archiveCreation.Create(archiveName);
    }

    public void OpenArchive(string archivePath)
    {
      _archiveOpening.Open(archivePath);
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
