using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace TestArchive
{
  public partial class ArchiveExplorerWindow : Window
  {
    private static readonly string[] ArchivesFolderCandidates = new[]
    {
      Path.Combine(@"D:\AskMkiM\Bin", "Archives"),
      Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Archives"),
      Path.Combine(Directory.GetCurrentDirectory(), "Archives"),
    };

    private readonly Dictionary<string, IReadOnlyList<ArchiveEntryInfo>> _archiveEntriesCache =
      new Dictionary<string, IReadOnlyList<ArchiveEntryInfo>>(StringComparer.OrdinalIgnoreCase);

    private readonly string _archivesFolderPath;
    private readonly FileSystemWatcher _archivesWatcher;
    private readonly DispatcherTimer _autoRefreshTimer;

    private IReadOnlyList<ArchiveEntryInfo> _currentGridEntries = Array.Empty<ArchiveEntryInfo>();
    private bool _suppressGridSelection;
    private string _lastSelectedArchivePath;
    private string _lastSelectedEntryName;

    public ArchiveExplorerWindow()
    {
      InitializeComponent();

      _archivesFolderPath = ResolveArchivesFolderPath();

      _autoRefreshTimer = new DispatcherTimer
      {
        Interval = TimeSpan.FromMilliseconds(350),
      };
      _autoRefreshTimer.Tick += AutoRefreshTimer_Tick;

      _archivesWatcher = CreateArchivesWatcher(_archivesFolderPath);

      UpdateRightPanels(isFilesVisible: false, isEditorVisible: false);
      ResetTree();
    }

    protected override void OnClosed(EventArgs e)
    {
      _autoRefreshTimer.Stop();

      _archivesWatcher.EnableRaisingEvents = false;
      _archivesWatcher.Dispose();

      base.OnClosed(e);
    }

    private string ResolveArchivesFolderPath()
    {
      var existing = ArchivesFolderCandidates.FirstOrDefault(Directory.Exists);
      if (!string.IsNullOrWhiteSpace(existing))
      {
        return existing;
      }

      var fallback = ArchivesFolderCandidates[0];
      Directory.CreateDirectory(fallback);
      return fallback;
    }

    private FileSystemWatcher CreateArchivesWatcher(string archivesFolderPath)
    {
      var watcher = new FileSystemWatcher(archivesFolderPath, "*.apkw")
      {
        IncludeSubdirectories = false,
        NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime,
        EnableRaisingEvents = true,
      };

      watcher.Created += OnArchivesWatcherChanged;
      watcher.Changed += OnArchivesWatcherChanged;
      watcher.Deleted += OnArchivesWatcherChanged;
      watcher.Renamed += OnArchivesWatcherRenamed;

      return watcher;
    }

    private void OnArchivesWatcherChanged(object sender, FileSystemEventArgs e)
    {
      ScheduleAutoRefresh();
    }

    private void OnArchivesWatcherRenamed(object sender, RenamedEventArgs e)
    {
      ScheduleAutoRefresh();
    }

    private void ScheduleAutoRefresh()
    {
      Dispatcher.BeginInvoke(new Action(() =>
      {
        _autoRefreshTimer.Stop();
        _autoRefreshTimer.Start();
      }));
    }

    private async void AutoRefreshTimer_Tick(object sender, EventArgs e)
    {
      _autoRefreshTimer.Stop();
      _archiveEntriesCache.Clear();
      ResetTree();
      await RestoreRightPanelsAfterRefreshAsync();
    }

    private async Task RestoreRightPanelsAfterRefreshAsync()
    {
      if (string.IsNullOrWhiteSpace(_lastSelectedArchivePath) || !File.Exists(_lastSelectedArchivePath))
      {
        return;
      }

      if (!string.IsNullOrWhiteSpace(_lastSelectedEntryName))
      {
        var entries = await GetArchiveEntriesAsync(_lastSelectedArchivePath);
        var hasFile = entries.Any(item =>
          string.Equals(item.EntryName, NormalizeEntryName(_lastSelectedEntryName), StringComparison.OrdinalIgnoreCase));

        if (hasFile)
        {
          await ShowFileAsync(_lastSelectedArchivePath, _lastSelectedEntryName);
          return;
        }
      }

      await ShowArchiveInGridAsync(_lastSelectedArchivePath, clearEditor: true);
    }

    private void ResetTree()
    {
      var rootNode = ArchiveTreeNode.CreateRoot("Archives");
      rootNode.Children.Add(ArchiveTreeNode.CreatePlaceholder("Loading..."));
      ArchivesTreeView.ItemsSource = new ObservableCollection<ArchiveTreeNode> { rootNode };
      ClearFilePanels();
    }

    private void ClearFilePanels()
    {
      ApplyGridItemsSource(Array.Empty<ArchiveEntryInfo>());
      FilesHintTextBlock.Text = "Select an archive in the tree to view files.";
      FileContentTextBox.Text = string.Empty;
      EditorHintTextBlock.Text = "Select a file in the tree or table to view text.";
      UpdateRightPanels(isFilesVisible: false, isEditorVisible: false);
    }

    private async Task LoadArchivesIntoRootAsync(ArchiveTreeNode rootNode)
    {
      if (!HasPlaceholder(rootNode))
      {
        return;
      }

      rootNode.Children.Clear();

      var archivePaths = await Task.Run(() =>
        Directory.EnumerateFiles(_archivesFolderPath, "*.apkw", SearchOption.TopDirectoryOnly)
          .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
          .ToList());

      if (archivePaths.Count == 0)
      {
        rootNode.Children.Add(ArchiveTreeNode.CreatePlaceholder("No archives found."));
        return;
      }

      foreach (var archivePath in archivePaths)
      {
        var archiveNode = ArchiveTreeNode.CreateArchive(Path.GetFileName(archivePath), archivePath);
        archiveNode.Children.Add(ArchiveTreeNode.CreatePlaceholder("Expand to load files..."));
        rootNode.Children.Add(archiveNode);
      }
    }

    private async Task LoadArchiveFilesIntoTreeAsync(ArchiveTreeNode archiveNode)
    {
      if (archiveNode.ArchivePath == null || !HasPlaceholder(archiveNode))
      {
        return;
      }

      archiveNode.Children.Clear();

      try
      {
        var entries = await GetArchiveEntriesAsync(archiveNode.ArchivePath);
        if (entries.Count == 0)
        {
          archiveNode.Children.Add(ArchiveTreeNode.CreatePlaceholder("Archive is empty."));
          return;
        }

        foreach (var entry in entries)
        {
          archiveNode.Children.Add(ArchiveTreeNode.CreateFile(entry.Name, archiveNode.ArchivePath, entry.EntryName));
        }
      }
      catch (Exception ex)
      {
        archiveNode.Children.Add(ArchiveTreeNode.CreatePlaceholder("Archive read error."));
        MessageBox.Show(this, ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private async Task<IReadOnlyList<ArchiveEntryInfo>> GetArchiveEntriesAsync(string archivePath)
    {
      if (_archiveEntriesCache.TryGetValue(archivePath, out var cached))
      {
        return cached;
      }

      var entries = await Task.Run(() => ReadArchiveEntries(archivePath));
      _archiveEntriesCache[archivePath] = entries;
      return entries;
    }

    private static IReadOnlyList<ArchiveEntryInfo> ReadArchiveEntries(string archivePath)
    {
      var items = new List<ArchiveEntryInfo>();

      using (var archiveStream = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
      using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read, leaveOpen: false))
      {
        foreach (var entry in archive.Entries)
        {
          if (string.IsNullOrWhiteSpace(entry.Name))
          {
            continue;
          }

          if (string.Equals(NormalizeEntryName(entry.FullName), "__apkw_manifest.json", StringComparison.OrdinalIgnoreCase))
          {
            continue;
          }

          items.Add(new ArchiveEntryInfo(
            archivePath,
            NormalizeEntryName(entry.FullName),
            entry.Length,
            entry.CompressedLength,
            entry.LastWriteTime));
        }
      }

      return items
        .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
        .ToList();
    }

    private static string ReadArchiveEntryText(string archivePath, string entryName)
    {
      using (var archiveStream = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
      using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read, leaveOpen: false))
      {
        var normalizedName = NormalizeEntryName(entryName);
        var entry = archive.Entries.FirstOrDefault(item =>
          string.Equals(NormalizeEntryName(item.FullName), normalizedName, StringComparison.OrdinalIgnoreCase));

        if (entry == null)
        {
          throw new FileNotFoundException("File not found in archive.", normalizedName);
        }

        using (var entryStream = entry.Open())
        using (var buffer = new MemoryStream())
        {
          entryStream.CopyTo(buffer);
          return DecodeText(buffer.ToArray());
        }
      }
    }

    private static string DecodeText(byte[] bytes)
    {
      var utf8Strict = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
      try
      {
        return utf8Strict.GetString(bytes);
      }
      catch (DecoderFallbackException)
      {
      }

      var cp866 = Encoding.GetEncoding(866, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
      try
      {
        return cp866.GetString(bytes);
      }
      catch (DecoderFallbackException)
      {
      }

      return Encoding.UTF8.GetString(bytes);
    }

    private static string NormalizeEntryName(string entryName)
    {
      return (entryName ?? string.Empty).Replace('\\', '/').TrimStart('/');
    }

    private static bool HasPlaceholder(ArchiveTreeNode node)
    {
      return node.Children.Count == 1 && node.Children[0].Kind == ArchiveTreeNodeKind.Placeholder;
    }

    private void UpdateRightPanels(bool isFilesVisible, bool isEditorVisible)
    {
      FilesPanel.Visibility = isFilesVisible ? Visibility.Visible : Visibility.Collapsed;
      EditorPanel.Visibility = isEditorVisible ? Visibility.Visible : Visibility.Collapsed;

      if (isFilesVisible && isEditorVisible)
      {
        FilesRowDefinition.Height = new GridLength(1, GridUnitType.Star);
        EditorRowDefinition.Height = new GridLength(1, GridUnitType.Star);
        RightSplitter.Visibility = Visibility.Visible;
        return;
      }

      if (isFilesVisible)
      {
        FilesRowDefinition.Height = new GridLength(1, GridUnitType.Star);
        EditorRowDefinition.Height = new GridLength(0);
        RightSplitter.Visibility = Visibility.Collapsed;
        return;
      }

      if (isEditorVisible)
      {
        FilesRowDefinition.Height = new GridLength(0);
        EditorRowDefinition.Height = new GridLength(1, GridUnitType.Star);
        RightSplitter.Visibility = Visibility.Collapsed;
        return;
      }

      FilesRowDefinition.Height = new GridLength(0);
      EditorRowDefinition.Height = new GridLength(0);
      RightSplitter.Visibility = Visibility.Collapsed;
    }

    private void ApplyGridItemsSource(IReadOnlyList<ArchiveEntryInfo> entries)
    {
      _currentGridEntries = entries ?? Array.Empty<ArchiveEntryInfo>();

      if (ArchiveFilesDataGrid == null)
      {
        return;
      }

      ArchiveFilesDataGrid.ItemsSource = _currentGridEntries;
    }

    private async Task ShowArchiveInGridAsync(string archivePath, bool clearEditor)
    {
      var entries = await GetArchiveEntriesAsync(archivePath);

      ApplyGridItemsSource(entries);
      FilesHintTextBlock.Text = entries.Count == 0
        ? "Archive is empty."
        : "Select a file in the table or tree.";

      if (clearEditor)
      {
        FileContentTextBox.Text = string.Empty;
        EditorHintTextBlock.Text = "Select a file in the tree or table to view text.";
        UpdateRightPanels(isFilesVisible: true, isEditorVisible: false);
      }
      else
      {
        var hasEditorText = !string.IsNullOrWhiteSpace(FileContentTextBox.Text);
        UpdateRightPanels(isFilesVisible: true, isEditorVisible: hasEditorText);
      }
    }

    private async Task ShowFileAsync(string archivePath, string entryName)
    {
      await ShowArchiveInGridAsync(archivePath, clearEditor: false);

      var selectedRow = _currentGridEntries.FirstOrDefault(item =>
        string.Equals(item.EntryName, NormalizeEntryName(entryName), StringComparison.OrdinalIgnoreCase));

      if (selectedRow != null)
      {
        _suppressGridSelection = true;
        ArchiveFilesDataGrid.SelectedItem = selectedRow;
        ArchiveFilesDataGrid.ScrollIntoView(selectedRow);
        _suppressGridSelection = false;
      }

      var text = await Task.Run(() => ReadArchiveEntryText(archivePath, entryName));
      FileContentTextBox.Text = text;
      EditorHintTextBlock.Text = "File content is shown in read-only mode.";
      UpdateRightPanels(isFilesVisible: true, isEditorVisible: true);
    }

    private async void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
    {
      if (!ReferenceEquals(sender, e.OriginalSource))
      {
        return;
      }

      var item = sender as TreeViewItem;
      var node = item?.DataContext as ArchiveTreeNode;
      if (node == null)
      {
        return;
      }

      try
      {
        if (node.Kind == ArchiveTreeNodeKind.Root)
        {
          await LoadArchivesIntoRootAsync(node);
          return;
        }

        if (node.Kind == ArchiveTreeNodeKind.Archive)
        {
          await LoadArchiveFilesIntoTreeAsync(node);
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show(this, ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private async void ArchivesTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
      var node = e.NewValue as ArchiveTreeNode;
      if (node == null)
      {
        return;
      }

      try
      {
        if (node.Kind == ArchiveTreeNodeKind.Root)
        {
          _lastSelectedArchivePath = null;
          _lastSelectedEntryName = null;
          ClearFilePanels();
          return;
        }

        if (node.Kind == ArchiveTreeNodeKind.Archive && node.ArchivePath != null)
        {
          _lastSelectedArchivePath = node.ArchivePath;
          _lastSelectedEntryName = null;
          await ShowArchiveInGridAsync(node.ArchivePath, clearEditor: true);
          return;
        }

        if (node.Kind == ArchiveTreeNodeKind.File &&
            node.ArchivePath != null &&
            !string.IsNullOrWhiteSpace(node.EntryName))
        {
          _lastSelectedArchivePath = node.ArchivePath;
          _lastSelectedEntryName = node.EntryName;
          await ShowFileAsync(node.ArchivePath, node.EntryName);
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show(this, ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private async void ArchiveFilesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (_suppressGridSelection)
      {
        return;
      }

      var selected = ArchiveFilesDataGrid.SelectedItem as ArchiveEntryInfo;
      if (selected == null)
      {
        return;
      }

      try
      {
        _lastSelectedArchivePath = selected.ArchivePath;
        _lastSelectedEntryName = selected.EntryName;

        var text = await Task.Run(() => ReadArchiveEntryText(selected.ArchivePath, selected.EntryName));
        FileContentTextBox.Text = text;
        EditorHintTextBlock.Text = "File content is shown in read-only mode.";
        UpdateRightPanels(isFilesVisible: true, isEditorVisible: true);
      }
      catch (Exception ex)
      {
        MessageBox.Show(this, ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

  }

  internal enum ArchiveTreeNodeKind
  {
    Root,
    Archive,
    File,
    Placeholder,
  }

  internal sealed class ArchiveTreeNode
  {
    public string DisplayName { get; private set; }
    public ArchiveTreeNodeKind Kind { get; private set; }
    public string ArchivePath { get; private set; }
    public string EntryName { get; private set; }
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

  internal sealed class ArchiveEntryInfo
  {
    public string ArchivePath { get; }
    public string EntryName { get; }
    public string Name { get; }
    public string Extension => string.IsNullOrWhiteSpace(Path.GetExtension(Name)) ? "(none)" : Path.GetExtension(Name).ToLowerInvariant();
    public long SizeBytes { get; }
    public long PackedBytes { get; }
    public DateTime LastModified { get; }

    public ArchiveEntryInfo(string archivePath, string entryName, long sizeBytes, long packedBytes, DateTimeOffset lastModified)
    {
      ArchivePath = archivePath;
      EntryName = entryName;
      Name = entryName;
      SizeBytes = sizeBytes;
      PackedBytes = packedBytes;
      LastModified = lastModified.LocalDateTime;
    }
  }
}
