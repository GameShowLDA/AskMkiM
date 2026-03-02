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
    public List<string>? OpkFileName { get; }
    public string OpkFileNameDisplay =>
    OpkFileName == null ? string.Empty : string.Join(Environment.NewLine, OpkFileName);
    /// <summary>
    /// Цех.
    /// </summary>
    public string? Department { get; }
    /// <summary>
    /// Примечание.
    /// </summary>
    public string? Comment { get; }
    public DateTime CreationDate { get; }

    public ArchiveEntryInfo(string archivePath, string entryName, string name, string nameOK, string order, List<string> opkfileName,
      string department, string comment, string opk, string ik, DateTime creationDate)
    {
      ArchivePath = archivePath;
      EntryName = entryName;
      Name = name;
      NameOK = nameOK;
      Order = order;
      OpkFileName = opkfileName;
      Department = department;
      Comment = comment;
      OPK = opk;  
      IK = ik;
      CreationDate = creationDate;
    }
  }
}
