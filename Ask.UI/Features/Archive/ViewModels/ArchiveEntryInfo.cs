using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.UI.Shared.Formatting;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace Ask.UI.Features.Archive.ViewModels
{
  /// <summary>
  /// Представляет информацию о файле внутри архива.
  /// </summary>
  public sealed class ArchiveEntryInfo : INotifyPropertyChanged
  {
    /// <summary>
    /// Путь к архиву, содержащему файл.
    /// </summary>
    public string ArchivePath { get; }

    /// <summary>
    /// Имя файла внутри архива.
    /// </summary>
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
    public string KDDisplay => KD == null ? string.Empty : string.Join(Environment.NewLine, KD);

    /// <summary>
    /// Цех.
    /// </summary>
    public string? Department { get; }
    /// <summary>
    /// Примечание.
    /// </summary>
    public string? Comment { get; }

    /// <summary>
    /// Дата создания файла.
    /// </summary>
    public DateTime CreationDate { get; }

    /// <summary>
    /// Исходный путь к файлу на диске.
    /// </summary>
    public string? SourceFilePath { get; }

    /// <summary>
    /// Признак того, что файл относится к архиву на проверке.
    /// </summary>
    public bool IsReviewEntry { get; }

    /// <summary>
    /// Количество ошибок, найденных в файле.
    /// </summary>
    private int _errorCount;

    /// <summary>
    /// Количество ошибок, найденных в файле.
    /// </summary>
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

    /// <summary>
    /// Отформатированное отображение количества ошибок.
    /// </summary>
    public string ErrorCountDisplay => CountDisplayFormatter.FormatNonZero(ErrorCount);

    /// <summary>
    /// Тип файла для отображения и обработки.
    /// </summary>
    public FileType FileType { get; }

    /// <summary>
    /// Событие изменения свойств объекта.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Инициализирует информацию о файле архива.
    /// </summary>
    /// <param name="archivePath">Путь к архиву.</param>
    /// <param name="entryName">Имя файла внутри архива.</param>
    /// <param name="name">Наименование файла.</param>
    /// <param name="nameOK">Наименование ОК.</param>
    /// <param name="order">Номер приказа или заказа.</param>
    /// <param name="opkFileName">Имя файла ОПК.</param>
    /// <param name="kd">Список KD-значений.</param>
    /// <param name="department">Подразделение.</param>
    /// <param name="comment">Комментарий.</param>
    /// <param name="opk">Значение ОПК.</param>
    /// <param name="ik">Значение ИК.</param>
    /// <param name="creationDate">Дата создания файла.</param>
    /// <param name="sourceFilePath">Исходный путь к файлу.</param>
    /// <param name="isReviewEntry">
    /// Признак того, что запись относится к архиву на проверке.
    /// </param>
    /// <param name="errorCount">Количество найденных ошибок.</param>
    /// <param name="fileType">Тип файла.</param>
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

    /// <summary>
    /// Обновляет состояние проверки файла.
    /// </summary>
    /// <param name="errorCount">Новое количество ошибок.</param>
    public void UpdateReviewState(int errorCount)
    {
      ErrorCount = errorCount;
    }

    /// <summary>
    /// Вызывает событие изменения свойства.
    /// </summary>
    /// <param name="propertyName">Имя изменённого свойства.</param>
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
