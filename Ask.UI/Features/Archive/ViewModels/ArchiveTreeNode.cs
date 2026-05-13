using Ask.UI.Shared.Formatting;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace Ask.UI.Features.Archive.ViewModels
{
  /// <summary>
  /// Представляет узел дерева архива.
  /// </summary>
  public sealed class ArchiveTreeNode : INotifyPropertyChanged
  {
    /// <summary>
    /// Отображаемое имя узла дерева.
    /// </summary>
    public string DisplayName { get; private set; }

    /// <summary>
    /// Тип узла дерева архивов.
    /// </summary>
    public ArchiveTreeNodeKind Kind { get; private set; }

    /// <summary>
    /// Путь к архиву, связанному с узлом.
    /// </summary>
    public string ArchivePath { get; private set; }

    /// <summary>
    /// Имя файла внутри архива.
    /// </summary>
    public string EntryName { get; private set; }

    /// <summary>
    /// Путь к файлу на диске.
    /// </summary>
    public string FilePath { get; private set; }

    /// <summary>
    /// Состояние узла архива.
    /// </summary>
    private ArchiveNodeStatus _status;

    /// <summary>
    /// Количество ошибок, связанных с узлом.
    /// </summary>
    private int _errorCount;

    /// <summary>
    /// Состояние узла архива.
    /// </summary>
    public ArchiveNodeStatus Status
    {
      get => _status;
      private set
      {
        if (_status == value)
        {
          return;
        }

        _status = value;
        OnPropertyChanged();
        OnPropertyChanged(nameof(StatusVisibility));
        OnPropertyChanged(nameof(StatusBrush));
      }
    }

    /// <summary>
    /// Количество ошибок, связанных с узлом архива.
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
    /// Видимость индикатора состояния узла.
    /// </summary>
    public Visibility StatusVisibility => Status.ToVisibility();

    /// <summary>
    /// Цветовая кисть состояния узла.
    /// </summary>
    public Brush StatusBrush => Status.ToBrush();

    /// <summary>
    /// Признак раскрытого состояния узла дерева.
    /// </summary>
    public bool IsExpanded { get; set; }

    /// <summary>
    /// Дочерние узлы дерева архивов.
    /// </summary>
    public RangeObservableCollection<ArchiveTreeNode> Children { get; } = new RangeObservableCollection<ArchiveTreeNode>();

    /// <summary>
    /// Событие изменения свойств объекта.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Инициализирует экземпляр узла дерева архивов.
    /// </summary>
    private ArchiveTreeNode() { }

    /// <summary>
    /// Создаёт корневой узел дерева архивов.
    /// </summary>
    /// <param name="name">Отображаемое имя узла.</param>
    /// <returns>Корневой узел архива.</returns>
    public static ArchiveTreeNode CreateRoot(string name)
    {
      return new ArchiveTreeNode
      {
        DisplayName = name,
        Kind = ArchiveTreeNodeKind.Root,
      };
    }

    /// <summary>
    /// Создаёт корневой узел дерева архивов на проверке.
    /// </summary>
    /// <param name="name">Отображаемое имя узла.</param>
    /// <returns>Корневой узел архива на проверке.</returns>
    public static ArchiveTreeNode CreateReviewRoot(string name)
    {
      return new ArchiveTreeNode
      {
        DisplayName = name,
        Kind = ArchiveTreeNodeKind.ReviewRoot,
      };
    }

    /// <summary>
    /// Создаёт узел архива.
    /// </summary>
    /// <param name="name">Отображаемое имя архива.</param>
    /// <param name="archivePath">Путь к архиву.</param>
    /// <param name="status">Состояние архива.</param>
    /// <param name="errorCount">Количество ошибок архива.</param>
    /// <returns>Узел архива.</returns>
    public static ArchiveTreeNode CreateArchive(
      string name,
      string archivePath,
      ArchiveNodeStatus status = ArchiveNodeStatus.None,
      int errorCount = 0)
    {
      return new ArchiveTreeNode
      {
        DisplayName = name,
        Kind = ArchiveTreeNodeKind.Archive,
        ArchivePath = archivePath,
        Status = status,
        ErrorCount = errorCount,
      };
    }

    /// <summary>
    /// Создаёт узел архива на проверке.
    /// </summary>
    /// <param name="name">Отображаемое имя архива.</param>
    /// <param name="archivePath">Путь к архиву.</param>
    /// <param name="status">Состояние архива.</param>
    /// <param name="errorCount">Количество ошибок архива.</param>
    /// <returns>Узел архива на проверке.</returns>
    public static ArchiveTreeNode CreateReviewArchive(
      string name,
      string archivePath,
      ArchiveNodeStatus status,
      int errorCount)
    {
      return new ArchiveTreeNode
      {
        DisplayName = name,
        Kind = ArchiveTreeNodeKind.ReviewArchive,
        ArchivePath = archivePath,
        Status = status,
        ErrorCount = errorCount,
      };
    }

    /// <summary>
    /// Создаёт узел файла внутри архива.
    /// </summary>
    /// <param name="name">Отображаемое имя файла.</param>
    /// <param name="archivePath">Путь к архиву.</param>
    /// <param name="entryName">Имя файла внутри архива.</param>
    /// <param name="status">Состояние файла.</param>
    /// <param name="errorCount">Количество ошибок файла.</param>
    /// <returns>Узел файла архива.</returns>
    public static ArchiveTreeNode CreateFile(
      string name,
      string archivePath,
      string entryName,
      ArchiveNodeStatus status = ArchiveNodeStatus.None,
      int errorCount = 0)
    {
      return new ArchiveTreeNode
      {
        DisplayName = name,
        Kind = ArchiveTreeNodeKind.File,
        ArchivePath = archivePath,
        EntryName = entryName,
        Status = status,
        ErrorCount = errorCount,
      };
    }

    /// <summary>
    /// Создаёт узел файла архива на проверке.
    /// </summary>
    /// <param name="name">Отображаемое имя файла.</param>
    /// <param name="archivePath">Путь к архиву.</param>
    /// <param name="entryName">Имя файла внутри архива.</param>
    /// <param name="filePath">Путь к файлу на диске.</param>
    /// <param name="status">Состояние файла.</param>
    /// <param name="errorCount">Количество ошибок файла.</param>
    /// <returns>Узел файла архива на проверке.</returns>
    public static ArchiveTreeNode CreateReviewFile(
      string name,
      string archivePath,
      string entryName,
      string filePath,
      ArchiveNodeStatus status,
      int errorCount)
    {
      return new ArchiveTreeNode
      {
        DisplayName = name,
        Kind = ArchiveTreeNodeKind.ReviewFile,
        ArchivePath = archivePath,
        EntryName = entryName,
        FilePath = filePath,
        Status = status,
        ErrorCount = errorCount,
      };
    }

    /// <summary>
    /// Создаёт узел-заглушку для отображения служебного текста.
    /// </summary>
    /// <param name="text">Текст заглушки.</param>
    /// <returns>Узел-заглушка.</returns>
    public static ArchiveTreeNode CreatePlaceholder(string text)
    {
      return new ArchiveTreeNode
      {
        DisplayName = text,
        Kind = ArchiveTreeNodeKind.Placeholder,
      };
    }

    /// <summary>
    /// Обновляет состояние узла архива.
    /// </summary>
    /// <param name="status">Новое состояние узла.</param>
    /// <param name="errorCount">Количество ошибок.</param>
    public void UpdateState(ArchiveNodeStatus status, int errorCount)
    {
      Status = status;
      ErrorCount = errorCount;
    }

    /// <summary>
    /// Обновляет состояние узла архива на проверке.
    /// </summary>
    /// <param name="status">Новое состояние узла.</param>
    /// <param name="errorCount">Количество ошибок.</param>
    public void UpdateReviewState(ArchiveNodeStatus status, int errorCount)
    {
      UpdateState(status, errorCount);
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
