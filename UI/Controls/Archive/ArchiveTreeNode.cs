using System.Collections.ObjectModel;

namespace UI.Controls.Archive
{
  internal sealed class ArchiveTreeNode
  {
    public string DisplayName { get; private set; }
    public ArchiveTreeNodeKind Kind { get; private set; }
    public string ArchivePath { get; private set; }
    public string EntryName { get; private set; }
    public bool IsExpanded { get; set; }
    public ObservableCollection<ArchiveTreeNode> Children { get; } = new ObservableCollection<ArchiveTreeNode>();

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

    public static ArchiveTreeNode CreateArchive(string name, string archivePath)
    {
      return new ArchiveTreeNode
      {
        DisplayName = name,
        Kind = ArchiveTreeNodeKind.Archive,
        ArchivePath = archivePath,
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

    public static ArchiveTreeNode CreatePlaceholder(string text)
    {
      return new ArchiveTreeNode
      {
        DisplayName = text,
        Kind = ArchiveTreeNodeKind.Placeholder,
      };
    }
  }
}
