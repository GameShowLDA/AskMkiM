using Ask.Core.Shared.Metadata.Enums.FileEnums;
using System.Globalization;
using System.IO;

namespace UI.Controls.Archive
{
  internal sealed class ArchiveEntryInfo
  {
    public string ArchivePath { get; }
    public string EntryName { get; }

    /// <summary>
    /// Обозначение.
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// Наименование ОК.
    /// </summary>
    public string? NameOK { get; }
    /// <summary>
    /// Бумажный документ таблицы программы испытаний при его наличии. 
    /// </summary>
    public string? OPK { get; }
    /// <summary>
    /// Тип системы контроля.
    /// </summary>
    public string? IK { get; }
    /// <summary>
    /// Заказ.
    /// </summary>
    public string? Order { get; }
    /// <summary>
    /// Файл ОПК.
    /// </summary>
    public string OpkFileName { get; }

    public List<string>? KD { get; }
    public string KDDisplay =>
    KD == null ? string.Empty : string.Join(Environment.NewLine, KD);
    /// <summary>
    /// Цех.
    /// </summary>
    public string? Department { get; }
    /// <summary>
    /// Примечание.
    /// </summary>
    public string? Comment { get; }
    public DateTime CreationDate { get; }
    public string? SourceFilePath { get; }
    public bool IsReviewEntry { get; }
    public int ErrorCount { get; }
    public string ErrorCountDisplay => ErrorCount > 0 ? ErrorCount.ToString(CultureInfo.InvariantCulture) : string.Empty;
    public FileType FileType { get; }

    public ArchiveEntryInfo(
      string archivePath,
      string entryName,
      string name,
      string nameOK,
      string order,
      string opkFileName,
      List<string> kd,
      string department,
      string comment,
      string opk,
      string ik,
      DateTime creationDate,
      string? sourceFilePath = null,
      bool isReviewEntry = false,
      int errorCount = 0,
      FileType fileType = FileType.OPKW)
    {
      ArchivePath = archivePath;
      EntryName = entryName;
      Name = name;
      NameOK = nameOK;
      Order = order;
      OpkFileName = opkFileName;
      KD = kd;
      Department = department;
      Comment = comment;
      OPK = opk;
      IK = ik;
      CreationDate = creationDate;
      SourceFilePath = sourceFilePath;
      IsReviewEntry = isReviewEntry;
      ErrorCount = errorCount;
      FileType = fileType;
    }
  }
}
