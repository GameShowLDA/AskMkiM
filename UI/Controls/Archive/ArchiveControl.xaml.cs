using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using UI.Services.Archive;
using Path = System.IO.Path;

namespace UI.Controls.Archive
{
  /// <summary>
  /// Логика взаимодействия для ArchiveControl.xaml
  /// </summary>
  public partial class ArchiveControl : UserControl
  {
    private static readonly string[] ArchivesFolderCandidates = new[]
    {
      System.IO.Path.Combine(@"D:\AskMkiM\Bin", "Archives"),
      Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Archives"),
      Path.Combine(Directory.GetCurrentDirectory(), "Archives"),
    };

    private readonly Dictionary<string, IReadOnlyList<ArchiveEntryInfo>> _archiveEntriesCache =
      new Dictionary<string, IReadOnlyList<ArchiveEntryInfo>>(StringComparer.OrdinalIgnoreCase);

    private readonly string _archivesFolderPath;
    private readonly FileSystemWatcher _archivesWatcher;
    private readonly DispatcherTimer _autoRefreshTimer;
    private readonly ArchiveManager _archiveManager = new ArchiveManager();
    private readonly object _archiveManagerSync = new object();

    private IReadOnlyList<ArchiveEntryInfo> _currentGridEntries = Array.Empty<ArchiveEntryInfo>();
    private bool _suppressGridSelection;
    private string _lastSelectedArchivePath;
    private string _lastSelectedEntryName;
    private ArchiveTreeNode _contextMenuNode;
    public ArchiveControl()
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
      await RefreshTreePreservingStateAsync(preservePanels: true);
    }

    private TreeRefreshState CaptureTreeRefreshState()
    {
      var state = new TreeRefreshState();
      var rootNode = GetRootNode();
      if (rootNode == null)
      {
        return state;
      }

      state.IsRootExpanded = rootNode.IsExpanded;
      foreach (var archiveNode in rootNode.Children.Where(node =>
                 node.Kind == ArchiveTreeNodeKind.Archive &&
                 node.IsExpanded &&
                 !string.IsNullOrWhiteSpace(node.ArchivePath)))
      {
        state.ExpandedArchivePaths.Add(Path.GetFullPath(archiveNode.ArchivePath));
      }

      return state;
    }

    private ArchiveTreeNode GetRootNode()
    {
      var roots = ArchivesTreeView.ItemsSource as IEnumerable<ArchiveTreeNode>;
      return roots?.FirstOrDefault();
    }

    private async Task RefreshTreePreservingStateAsync(bool preservePanels)
    {
      var state = CaptureTreeRefreshState();

      var rootNode = ArchiveTreeNode.CreateRoot("Archives");
      rootNode.IsExpanded = state.IsRootExpanded;
      rootNode.Children.Add(ArchiveTreeNode.CreatePlaceholder("Loading..."));
      ArchivesTreeView.ItemsSource = new ObservableCollection<ArchiveTreeNode> { rootNode };

      if (state.IsRootExpanded || state.ExpandedArchivePaths.Count > 0)
      {
        await LoadArchivesIntoRootAsync(rootNode, state.ExpandedArchivePaths);
      }

      if (preservePanels)
      {
        await RestoreRightPanelsAfterRefreshAsync();
        return;
      }

      ClearFilePanels();
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

    private async Task LoadArchivesIntoRootAsync(ArchiveTreeNode rootNode, ISet<string> expandedArchivePaths = null)
    {
      if (!HasPlaceholder(rootNode))
      {
        return;
      }

      rootNode.Children.Clear();
      var expandedArchiveNodes = new List<ArchiveTreeNode>();

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
        var fullArchivePath = Path.GetFullPath(archivePath);
        var isExpanded = expandedArchivePaths != null && expandedArchivePaths.Contains(fullArchivePath);

        var archiveNode = ArchiveTreeNode.CreateArchive(Path.GetFileName(archivePath), archivePath);
        archiveNode.IsExpanded = isExpanded;
        archiveNode.Children.Add(ArchiveTreeNode.CreatePlaceholder("Expand to load files..."));
        rootNode.Children.Add(archiveNode);

        if (isExpanded)
        {
          expandedArchiveNodes.Add(archiveNode);
        }
      }

      foreach (var expandedNode in expandedArchiveNodes)
      {
        await LoadArchiveFilesIntoTreeAsync(expandedNode);
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
        MessageBox.Show(Window.GetWindow(this), ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

    private IReadOnlyList<string> OpenArchiveInManager(string archivePath)
    {
      lock (_archiveManagerSync)
      {
        _archiveManager.OpenArchive(archivePath);
        return _archiveManager.IntegrityNotifications?.ToList() ?? new List<string>();
      }
    }

    private string ReadArchiveEntryTextWithManager(string archivePath, string entryName)
    {
      lock (_archiveManagerSync)
      {
        EnsureArchiveOpenedInManagerCore(archivePath);
        return _archiveManager.GetFileText(entryName);
      }
    }

    private void EnsureArchiveOpenedInManagerCore(string archivePath)
    {
      var fullArchivePath = Path.GetFullPath(archivePath);
      if (!string.Equals(_archiveManager.OpenedArchivePath, fullArchivePath, StringComparison.OrdinalIgnoreCase))
      {
        _archiveManager.OpenArchive(fullArchivePath);
      }
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
      var integrityNotifications = await Task.Run(() => OpenArchiveInManager(archivePath));
      var entries = await GetArchiveEntriesAsync(archivePath);

      ApplyGridItemsSource(entries);
      var baseHint = entries.Count == 0
        ? "Archive is empty."
        : "Select a file in the table or tree.";
      FilesHintTextBlock.Text = integrityNotifications.Count == 0
        ? baseHint
        : $"{baseHint} Integrity warnings: {integrityNotifications.Count}.";

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

      var text = await Task.Run(() => ReadArchiveEntryTextWithManager(archivePath, entryName));
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
        MessageBox.Show(Window.GetWindow(this), ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void TreeViewItem_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
      var item = sender as TreeViewItem;
      if (item == null)
      {
        return;
      }

      _contextMenuNode = item.DataContext as ArchiveTreeNode;
    }

    private ArchiveTreeNode GetContextNode()
    {
      return _contextMenuNode ?? (ArchivesTreeView.SelectedItem as ArchiveTreeNode);
    }

    private void ArchivesTreeContextMenu_Opened(object sender, RoutedEventArgs e)
    {
      var node = GetContextNode();
      var isRoot = node?.Kind == ArchiveTreeNodeKind.Root;
      var isArchive = node?.Kind == ArchiveTreeNodeKind.Archive;
      var isFile = node?.Kind == ArchiveTreeNodeKind.File;

      CreateArchiveMenuItem.Visibility = isRoot ? Visibility.Visible : Visibility.Collapsed;
      OpenArchiveMenuItem.Visibility = isArchive ? Visibility.Visible : Visibility.Collapsed;
      DeleteArchiveMenuItem.Visibility = isArchive ? Visibility.Visible : Visibility.Collapsed;
      AddFileToArchiveMenuItem.Visibility = isArchive ? Visibility.Visible : Visibility.Collapsed;
      OpenArchiveFileMenuItem.Visibility = isFile ? Visibility.Visible : Visibility.Collapsed;
      DeleteArchiveFileMenuItem.Visibility = isFile ? Visibility.Visible : Visibility.Collapsed;

      if (!isRoot && !isArchive && !isFile)
      {
        var contextMenu = sender as ContextMenu;
        if (contextMenu != null)
        {
          contextMenu.IsOpen = false;
        }
      }
    }

    private void ArchivesTreeContextMenu_Closed(object sender, RoutedEventArgs e)
    {
      _contextMenuNode = null;
    }

    private async void CreateArchiveMenuItem_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        var archiveName = PromptForArchiveName();
        if (string.IsNullOrWhiteSpace(archiveName))
        {
          return;
        }

        lock (_archiveManagerSync)
        {
          _archiveManager.CreateArchive(archiveName);
        }

        _archiveEntriesCache.Clear();
        await RefreshTreePreservingStateAsync(preservePanels: true);
      }
      catch (Exception ex)
      {
        MessageBox.Show(Window.GetWindow(this), ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private async void OpenArchiveMenuItem_Click(object sender, RoutedEventArgs e)
    {
      var node = GetContextNode();
      if (node?.Kind != ArchiveTreeNodeKind.Archive || string.IsNullOrWhiteSpace(node.ArchivePath))
      {
        return;
      }

      try
      {
        _lastSelectedArchivePath = node.ArchivePath;
        _lastSelectedEntryName = null;
        await ShowArchiveInGridAsync(node.ArchivePath, clearEditor: true);
      }
      catch (Exception ex)
      {
        MessageBox.Show(Window.GetWindow(this), ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private async void AddFileToArchiveMenuItem_Click(object sender, RoutedEventArgs e)
    {
      var node = GetContextNode();
      if (node?.Kind != ArchiveTreeNodeKind.Archive || string.IsNullOrWhiteSpace(node.ArchivePath))
      {
        return;
      }

      var openFileDialog = new OpenFileDialog
      {
        Title = "Select file to add",
        CheckFileExists = true,
        Multiselect = false,
      };

      if (openFileDialog.ShowDialog(Window.GetWindow(this)) != true)
      {
        return;
      }

      try
      {
        lock (_archiveManagerSync)
        {
          EnsureArchiveOpenedInManagerCore(node.ArchivePath);
          _archiveManager.AddFileToOpenedArchive(openFileDialog.FileName);
        }

        _archiveEntriesCache.Remove(node.ArchivePath);
        await RefreshTreePreservingStateAsync(preservePanels: true);
      }
      catch (Exception ex)
      {
        MessageBox.Show(Window.GetWindow(this), ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private async void DeleteArchiveMenuItem_Click(object sender, RoutedEventArgs e)
    {
      var node = GetContextNode();
      if (node?.Kind != ArchiveTreeNodeKind.Archive || string.IsNullOrWhiteSpace(node.ArchivePath))
      {
        return;
      }

      var confirmation = MessageBox.Show(
        Window.GetWindow(this),
        $"Delete archive '{node.DisplayName}'?",
        "Delete archive",
        MessageBoxButton.YesNo,
        MessageBoxImage.Warning);

      if (confirmation != MessageBoxResult.Yes)
      {
        return;
      }

      try
      {
        var activeArchiveDeleted = false;

        lock (_archiveManagerSync)
        {
          _archiveManager.DeleteArchive(node.ArchivePath);
        }

        if (string.Equals(_lastSelectedArchivePath, node.ArchivePath, StringComparison.OrdinalIgnoreCase))
        {
          activeArchiveDeleted = true;
          _lastSelectedArchivePath = null;
          _lastSelectedEntryName = null;
        }

        _archiveEntriesCache.Clear();
        await RefreshTreePreservingStateAsync(preservePanels: !activeArchiveDeleted);
      }
      catch (Exception ex)
      {
        MessageBox.Show(Window.GetWindow(this), ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private async void OpenArchiveFileMenuItem_Click(object sender, RoutedEventArgs e)
    {
      var node = GetContextNode();
      if (node?.Kind != ArchiveTreeNodeKind.File ||
          string.IsNullOrWhiteSpace(node.ArchivePath) ||
          string.IsNullOrWhiteSpace(node.EntryName))
      {
        return;
      }

      try
      {
        _lastSelectedArchivePath = node.ArchivePath;
        _lastSelectedEntryName = node.EntryName;
        await ShowFileAsync(node.ArchivePath, node.EntryName);
      }
      catch (Exception ex)
      {
        MessageBox.Show(Window.GetWindow(this), ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private async void DeleteArchiveFileMenuItem_Click(object sender, RoutedEventArgs e)
    {
      var node = GetContextNode();
      if (node?.Kind != ArchiveTreeNodeKind.File ||
          string.IsNullOrWhiteSpace(node.ArchivePath) ||
          string.IsNullOrWhiteSpace(node.EntryName))
      {
        return;
      }

      var confirmation = MessageBox.Show(
        Window.GetWindow(this),
        $"Delete file '{node.DisplayName}' from archive?",
        "Delete file",
        MessageBoxButton.YesNo,
        MessageBoxImage.Warning);

      if (confirmation != MessageBoxResult.Yes)
      {
        return;
      }

      try
      {
        lock (_archiveManagerSync)
        {
          EnsureArchiveOpenedInManagerCore(node.ArchivePath);
          _archiveManager.DeleteFileFromOpenedArchive(node.EntryName);
        }

        _archiveEntriesCache.Remove(node.ArchivePath);
        await RefreshTreePreservingStateAsync(preservePanels: true);
      }
      catch (Exception ex)
      {
        //MessageBox.Show(this, ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private string PromptForArchiveName()
    {
      var dialog = new Window
      {
        Title = "Create archive",
        //Owner = this,
        WindowStartupLocation = WindowStartupLocation.CenterOwner,
        ResizeMode = ResizeMode.NoResize,
        SizeToContent = SizeToContent.WidthAndHeight,
        ShowInTaskbar = false,
      };

      var layout = new Grid
      {
        Margin = new Thickness(12),
      };
      layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
      layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
      layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

      var label = new TextBlock
      {
        Text = "Archive name:",
      };

      var inputBox = new TextBox
      {
        MinWidth = 320,
        Margin = new Thickness(0, 8, 0, 0),
        Text = "new_archive",
      };

      var buttonsPanel = new StackPanel
      {
        Orientation = Orientation.Horizontal,
        HorizontalAlignment = HorizontalAlignment.Right,
        Margin = new Thickness(0, 12, 0, 0),
      };

      var createButton = new Button
      {
        Content = "Create",
        MinWidth = 90,
        IsDefault = true,
      };
      createButton.Click += (o, args) => dialog.DialogResult = true;

      var cancelButton = new Button
      {
        Content = "Cancel",
        MinWidth = 90,
        IsCancel = true,
        Margin = new Thickness(8, 0, 0, 0),
      };

      buttonsPanel.Children.Add(createButton);
      buttonsPanel.Children.Add(cancelButton);

      Grid.SetRow(label, 0);
      Grid.SetRow(inputBox, 1);
      Grid.SetRow(buttonsPanel, 2);
      layout.Children.Add(label);
      layout.Children.Add(inputBox);
      layout.Children.Add(buttonsPanel);
      dialog.Content = layout;

      dialog.Loaded += (o, args) =>
      {
        inputBox.Focus();
        inputBox.SelectAll();
      };

      return dialog.ShowDialog() == true
        ? inputBox.Text?.Trim()
        : null;
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
        MessageBox.Show(Window.GetWindow(this), ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        var text = await Task.Run(() => ReadArchiveEntryTextWithManager(selected.ArchivePath, selected.EntryName));
        FileContentTextBox.Text = text;
        EditorHintTextBlock.Text = "File content is shown in read-only mode.";
        UpdateRightPanels(isFilesVisible: true, isEditorVisible: true);
      }
      catch (Exception ex)
      {
        MessageBox.Show(Window.GetWindow(this), ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private sealed class TreeRefreshState
    {
      public bool IsRootExpanded { get; set; }
      public HashSet<string> ExpandedArchivePaths { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

  }
}
