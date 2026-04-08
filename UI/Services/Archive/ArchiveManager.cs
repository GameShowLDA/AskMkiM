using Ask.Core.Shared.Metadata.Static;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using System.IO;
using System.Reflection.Metadata;
using static Ask.LogLib.LoggerUtility;

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
      var createdArchivePath = _archiveCreation.Create(archiveName);
      EventAggregator.Publish(new ArchiveEvents.Changed(ArchiveEvents.ArchiveChangeKind.ArchiveCreated, createdArchivePath));
      return createdArchivePath;
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

      try
      {
        _archiveOpening.Close();
        _archiveFileAdder.AddFile(openedArchivePath, filePath);
        EventAggregator.Publish(new ArchiveEvents.Changed(ArchiveEvents.ArchiveChangeKind.ArchiveEntriesChanged, openedArchivePath));
      }
      finally
      {
        _archiveOpening.Close();
      }
    }

    public void AddFileToArchive(List<List<string>> sourceLines, string archivePath, string fileName)
    {
      var openedArchivePath = EnsureArchiveIsOpen();

      try
      {
        _archiveOpening.Close();
        _archiveFileAdder.AddFile(sourceLines, openedArchivePath, fileName);
        EventAggregator.Publish(new ArchiveEvents.Changed(ArchiveEvents.ArchiveChangeKind.ArchiveEntriesChanged, openedArchivePath));
      }
      finally
      {
        _archiveOpening.Close();
      }
    }

    public void DeleteFileFromOpenedArchive(string archiveEntryName)
    {
      var openedArchivePath = EnsureArchiveIsOpen();

      try
      {
        _archiveOpening.Close();
        _archiveFileAdder.DeleteFile(openedArchivePath, archiveEntryName);
        EventAggregator.Publish(new ArchiveEvents.Changed(ArchiveEvents.ArchiveChangeKind.ArchiveEntriesChanged, openedArchivePath));
      }
      finally
      {
        _archiveOpening.Close();
      }
    }

    public string CopyFileBetweenArchives(string sourceArchivePath, string archiveEntryName, string targetArchivePath)
    {
      return TransferFileBetweenArchives(sourceArchivePath, archiveEntryName, targetArchivePath, removeSource: false);
    }

    public string MoveFileBetweenArchives(string sourceArchivePath, string archiveEntryName, string targetArchivePath)
    {
      return TransferFileBetweenArchives(sourceArchivePath, archiveEntryName, targetArchivePath, removeSource: true);
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
      EventAggregator.Publish(new ArchiveEvents.Changed(ArchiveEvents.ArchiveChangeKind.ArchiveDeleted, pathToDelete));
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

    private string TransferFileBetweenArchives(string sourceArchivePath, string archiveEntryName, string targetArchivePath, bool removeSource)
    {
      var operationName = removeSource ? "перемещён" : "скопирован";

      try
      {
        _archiveOpening.Close();
        var transferredEntryName = _archiveFileAdder.TransferFile(sourceArchivePath, archiveEntryName, targetArchivePath, removeSource);

        LogInformation(
          $"Файл '{transferredEntryName}' {operationName} из архива '{Path.GetFileNameWithoutExtension(sourceArchivePath)}' в архив '{Path.GetFileNameWithoutExtension(targetArchivePath)}'.");

        EventAggregator.Publish(new ArchiveEvents.Changed(ArchiveEvents.ArchiveChangeKind.ArchiveEntriesChanged, targetArchivePath));
        if (removeSource)
        {
          EventAggregator.Publish(new ArchiveEvents.Changed(ArchiveEvents.ArchiveChangeKind.ArchiveEntriesChanged, sourceArchivePath));
        }

        return transferredEntryName;
      }
      finally
      {
        _archiveOpening.Close();
      }
    }
  }
}
