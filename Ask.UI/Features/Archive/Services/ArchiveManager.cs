using Ask.Core.Shared.Metadata.Static;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using System.IO;
using System.Reflection.Metadata;
using static Ask.LogLib.LoggerUtility;

namespace Ask.UI.Features.Archive.Services
{
  /// <summary>
  /// Управляет открытием, изменением и сохранением архивов.
  /// </summary>
  public sealed class ArchiveManager : IDisposable
  {
    /// <summary>
    /// Сервис создания архивов.
    /// </summary>
    private readonly ArchiveCreationService _archiveCreation = new ArchiveCreationService();

    /// <summary>
    /// Менеджер операций с файлами внутри архивов.
    /// </summary>
    private readonly ArchiveFileManager _archiveFileAdder = new ArchiveFileManager();

    /// <summary>
    /// Сервис открытия архивов.
    /// </summary>
    private readonly ArchiveOpeningService _archiveOpening = new ArchiveOpeningService();

    /// <summary>
    /// Путь к текущему открытому архиву.
    /// </summary>
    public string OpenedArchivePath => _archiveOpening.OpenedArchivePath;

    /// <summary>
    /// Базовый путь к директории архивов.
    /// </summary>
    private string _archivePath = Path.Combine(AppContext.BaseDirectory, FileLocations.ArchiveDirectory);

    /// <summary>
    /// Список уведомлений о нарушениях целостности архива.
    /// </summary>
    public IReadOnlyList<string> IntegrityNotifications => _archiveOpening.IntegrityNotifications;

    /// <summary>
    /// Создаёт новый архив и публикует событие изменения.
    /// </summary>
    /// <param name="archiveName">Имя создаваемого архива.</param>
    /// <returns>Путь к созданному архиву.</returns>
    public string CreateArchive(string archiveName)
    {
      var createdArchivePath = _archiveCreation.Create(archiveName);
      EventAggregator.Publish(new ArchiveEvents.Changed(ArchiveEvents.ArchiveChangeKind.ArchiveCreated, createdArchivePath));
      return createdArchivePath;
    }

    /// <summary>
    /// Открывает архив для дальнейшей работы.
    /// </summary>
    /// <param name="archivePath">Путь к архиву.</param>
    public void OpenArchive(string archivePath)
    {
      _archiveOpening.Open(archivePath);
    }

    /// <summary>
    /// Возвращает путь к директории архивов.
    /// </summary>
    /// <returns>Путь к директории архивов.</returns>
    public string GetArchivePath()
    {
      return _archivePath;
    }

    /// <summary>
    /// Возвращает список файлов текущего открытого архива.
    /// </summary>
    /// <returns>Список имён файлов архива.</returns>
    public IReadOnlyList<string> GetFileList()
    {
      return _archiveOpening.GetFileList();
    }

    /// <summary>
    /// Возвращает текстовое содержимое файла из открытого архива.
    /// </summary>
    /// <param name="archiveEntryName">Имя записи архива.</param>
    /// <returns>Текст файла.</returns>
    public string GetFileText(string archiveEntryName)
    {
      return _archiveOpening.GetFileText(archiveEntryName);
    }

    /// <summary>
    /// Добавляет файл в текущий открытый архив и публикует событие обновления.
    /// </summary>
    /// <param name="filePath">Путь к добавляемому файлу.</param>
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

    /// <summary>
    /// Добавляет сформированные данные в текущий открытый архив и публикует событие обновления.
    /// </summary>
    /// <param name="sourceLines">Строки данных для записи.</param>
    /// <param name="archivePath">Путь к архиву.</param>
    /// <param name="fileName">Имя создаваемого файла внутри архива.</param>
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

    /// <summary>
    /// Удаляет файл из текущего открытого архива и публикует событие обновления.
    /// </summary>
    /// <param name="archiveEntryName">Имя удаляемой записи архива.</param>
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

    /// <summary>
    /// Копирует файл из одного архива в другой.
    /// </summary>
    /// <param name="sourceArchivePath">Путь к исходному архиву.</param>
    /// <param name="archiveEntryName">Имя файла внутри архива.</param>
    /// <param name="targetArchivePath">Путь к целевому архиву.</param>
    /// <returns>Имя скопированной записи архива.</returns>
    public string CopyFileBetweenArchives(string sourceArchivePath, string archiveEntryName, string targetArchivePath)
    {
      return TransferFileBetweenArchives(sourceArchivePath, archiveEntryName, targetArchivePath, removeSource: false);
    }

    /// <summary>
    /// Перемещает файл из одного архива в другой.
    /// </summary>
    /// <param name="sourceArchivePath">Путь к исходному архиву.</param>
    /// <param name="archiveEntryName">Имя файла внутри архива.</param>
    /// <param name="targetArchivePath">Путь к целевому архиву.</param>
    /// <returns>Имя перемещённой записи архива.</returns>
    public string MoveFileBetweenArchives(string sourceArchivePath, string archiveEntryName, string targetArchivePath)
    {
      return TransferFileBetweenArchives(sourceArchivePath, archiveEntryName, targetArchivePath, removeSource: true);
    }

    /// <summary>
    /// Удаляет архив и публикует событие удаления.
    /// </summary>
    /// <param name="archivePath">
    /// Путь к архиву.
    /// Если не указан — удаляется текущий открытый архив.
    /// </param>
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

    /// <summary>
    /// Закрывает текущий открытый архив.
    /// </summary>
    public void CloseArchive()
    {
      _archiveOpening.Close();
    }

    /// <summary>
    /// Освобождает ресурсы менеджера архивов.
    /// </summary>
    public void Dispose()
    {
      _archiveOpening.Dispose();
    }

    /// <summary>
    /// Проверяет, что архив открыт, и возвращает путь к нему.
    /// </summary>
    /// <returns>Путь к открытому архиву.</returns>
    private string EnsureArchiveIsOpen()
    {
      if (string.IsNullOrWhiteSpace(OpenedArchivePath))
      {
        throw new InvalidOperationException("Archive is not open.");
      }

      return OpenedArchivePath;
    }

    /// <summary>
    /// Переносит или копирует файл между архивами и публикует события обновления.
    /// </summary>
    /// <param name="sourceArchivePath">Путь к исходному архиву.</param>
    /// <param name="archiveEntryName">Имя файла внутри архива.</param>
    /// <param name="targetArchivePath">Путь к целевому архиву.</param>
    /// <param name="removeSource">
    /// Признак удаления исходного файла после переноса.
    /// true — перемещение, false — копирование.
    /// </param>
    /// <returns>Имя перенесённой или скопированной записи архива.</returns>
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
