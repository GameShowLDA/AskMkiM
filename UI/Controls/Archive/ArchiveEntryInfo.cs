using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.UI.Shared.Formatting;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace UI.Controls.Archive
{
  internal sealed class ArchiveEntryInfo : INotifyPropertyChanged
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
    private int _errorCount;

    public int ErrorCount
    {
      get => _errorCount;
      private set
      {
        if (_errorCount == value)
        {
          return;
        }

        _errorCount = value;
        OnPropertyChanged();
        OnPropertyChanged(nameof(ErrorCountDisplay));
      }
    }

    public string ErrorCountDisplay => CountDisplayFormatter.FormatNonZero(ErrorCount);
    public FileType FileType { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

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
      _errorCount = errorCount;
      FileType = fileType;
    }

    public void UpdateReviewState(int errorCount)
    {
      ErrorCount = errorCount;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
