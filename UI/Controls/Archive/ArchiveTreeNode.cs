using Ask.UI.Shared.Formatting;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace UI.Controls.Archive
{
  internal sealed class ArchiveTreeNode : INotifyPropertyChanged
  {
    public string DisplayName { get; private set; }
    public ArchiveTreeNodeKind Kind { get; private set; }
    public string ArchivePath { get; private set; }
    public string EntryName { get; private set; }
    public string FilePath { get; private set; }
    private ArchiveNodeStatus _status;
    private int _errorCount;

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
    public Visibility StatusVisibility => Status.ToVisibility();
    public Brush StatusBrush => Status.ToBrush();
    public bool IsExpanded { get; set; }
    public RangeObservableCollection<ArchiveTreeNode> Children { get; } = new RangeObservableCollection<ArchiveTreeNode>();

    public event PropertyChangedEventHandler? PropertyChanged;

    private ArchiveTreeNode()
    {
    }

    public static ArchiveTreeNode CreateRoot(string name)
    {
      return new ArchiveTreeNode
      {
        DisplayName = name,
        Kind = ArchiveTreeNodeKind.Root,
      };
    }

    public static ArchiveTreeNode CreateReviewRoot(string name)
    {
      return new ArchiveTreeNode
      {
        DisplayName = name,
        Kind = ArchiveTreeNodeKind.ReviewRoot,
      };
    }

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

    public static ArchiveTreeNode CreatePlaceholder(string text)
    {
      return new ArchiveTreeNode
      {
        DisplayName = text,
        Kind = ArchiveTreeNodeKind.Placeholder,
      };
    }

    public void UpdateState(ArchiveNodeStatus status, int errorCount)
    {
      Status = status;
      ErrorCount = errorCount;
    }

    public void UpdateReviewState(ArchiveNodeStatus status, int errorCount)
    {
      UpdateState(status, errorCount);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
