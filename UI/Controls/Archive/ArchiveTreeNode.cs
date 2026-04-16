using System.Collections.ObjectModel;
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
      }
    }

    public Visibility StatusVisibility => Status.ToVisibility();
    public Brush StatusBrush => Status.ToBrush();
    public bool IsExpanded { get; set; }
    public ObservableCollection<ArchiveTreeNode> Children { get; } = new ObservableCollection<ArchiveTreeNode>();

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

    public static ArchiveTreeNode CreateArchive(string name, string archivePath)
    {
      return new ArchiveTreeNode
      {
        DisplayName = name,
        Kind = ArchiveTreeNodeKind.Archive,
        ArchivePath = archivePath,
      };
    }

    public static ArchiveTreeNode CreateReviewArchive(string name, string archivePath, ArchiveNodeStatus status, int errorCount)
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

    public static ArchiveTreeNode CreateFile(string name, string archivePath, string entryName)
    {
      return new ArchiveTreeNode
      {
        DisplayName = name,
        Kind = ArchiveTreeNodeKind.File,
        ArchivePath = archivePath,
        EntryName = entryName,
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

    public void UpdateReviewState(ArchiveNodeStatus status, int errorCount)
    {
      Status = status;
      ErrorCount = errorCount;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
