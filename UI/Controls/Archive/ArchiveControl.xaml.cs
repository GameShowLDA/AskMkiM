using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.Core.Shared.Metadata.Static;
using Ask.UI.Features.Notifications.Models;
using Ask.UI.Infrastructure.UI.Overlay.Notifications.Runtime;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using UI.Components.MultiEditorMethods;
using UI.Components.SearchControls;
using UI.Controls.TextEditorControl;
using UI.Services.Archive;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Orientation = System.Windows.Controls.Orientation;
using Path = System.IO.Path;
using UserControl = System.Windows.Controls.UserControl;

namespace UI.Controls.Archive
{
  /// <summary>
  /// Логика взаимодействия для ArchiveControl.xaml
  /// </summary>
  public partial class ArchiveControl : UserControl
  {
    private readonly Dictionary<string, IReadOnlyList<ArchiveEntryInfo>> _archiveEntriesCache =
      new Dictionary<string, IReadOnlyList<ArchiveEntryInfo>>(StringComparer.OrdinalIgnoreCase);
    private static readonly JsonSerializerOptions ManifestJsonOptions = new JsonSerializerOptions
    {
      PropertyNameCaseInsensitive = true
    };

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

    private readonly Dictionary<string, Dictionary<string, DateTime>> _manifestCache = new();

    public ArchiveControl()
    {
      InitializeComponent();
      EventAggregator.Subscribe<ArchiveEvents.Changed>(OnArchiveChanged);
      _archivesFolderPath = ArchiveDirectoryService.ResolveArchivesRootPath();

      _autoRefreshTimer = new DispatcherTimer
      {
        Interval = TimeSpan.FromMilliseconds(350),
      };
      _autoRefreshTimer.Tick += AutoRefreshTimer_Tick;

      _archivesWatcher = CreateArchivesWatcher(_archivesFolderPath);

      UpdateRightPanels(isFilesVisible: false, isEditorVisible: false);
      ResetTree();
    }

    private void OnArchiveChanged(ArchiveEvents.Changed change)
    {
      if (change == null || string.IsNullOrWhiteSpace(change.ArchivePath))
      {
        return;
      }

      if (!Dispatcher.CheckAccess())
      {
        Dispatcher.BeginInvoke(new Action(() => OnArchiveChanged(change)));
        return;
      }

      _ = HandleArchiveChangedAsync(change);
    }

    private async Task HandleArchiveChangedAsync(ArchiveEvents.Changed change)
    {
      try
      {
        InvalidateArchiveCaches(change.ArchivePath);

        switch (change.ChangeKind)
        {
          case ArchiveEvents.ArchiveChangeKind.ArchiveCreated:
          case ArchiveEvents.ArchiveChangeKind.ArchiveEntriesChanged:
            await RefreshTreePreservingStateAsync(preservePanels: true);
            break;

          case ArchiveEvents.ArchiveChangeKind.ArchiveDeleted:
            var selectedArchiveDeleted = IsSameArchivePath(_lastSelectedArchivePath, change.ArchivePath);
            if (selectedArchiveDeleted)
            {
              _lastSelectedArchivePath = null;
              _lastSelectedEntryName = null;
            }

            await RefreshTreePreservingStateAsync(preservePanels: !selectedArchiveDeleted);
            break;
        }
      }
      catch (Exception ex)
      {
        ShowArchiveNotification("Архивы", GetUserFriendlyArchiveErrorMessage(ex), NotificationType.Error);
      }
    }
    private async Task<Dictionary<string, DateTime>> GetManifestCacheAsync(string archivePath)
    {
      if (_manifestCache.TryGetValue(archivePath, out var cached))
        return cached;

      var result = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

      using var encryptionSession = ArchiveEncryptionSession.Acquire(archivePath);
      using var archive = ZipFile.OpenRead(archivePath);
      var manifestEntry = archive.GetEntry("__apkw_manifest.json");

      if (manifestEntry != null)
      {
        using var stream = manifestEntry.Open();

        var manifest = await JsonSerializer.DeserializeAsync<ArchiveManifest>(stream, ManifestJsonOptions);

        if (manifest?.Files != null)
        {
          foreach (var r in manifest.Files)
          {
            if (r == null || string.IsNullOrWhiteSpace(r.Name) || string.IsNullOrWhiteSpace(r.CreationDate))
            {
              continue;
            }

            if (DateTime.TryParse(r.CreationDate, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces, out var date))
            {
              result[NormalizeEntryName(r.Name)] = date;
            }
          }
        }
      }

      _manifestCache[archivePath] = result;
      return result;
    }

    private FileSystemWatcher CreateArchivesWatcher(string archivesFolderPath)
    {
      var watcher = new FileSystemWatcher(archivesFolderPath, "*")
      {
        IncludeSubdirectories = true,
        NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime,
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
      if (e.ChangeType == WatcherChangeTypes.Changed &&
          ArchiveEncryptionSession.WasRecentlyMutatedBySession(e.FullPath))
      {
        return;
      }

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
      _manifestCache.Clear();
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
      foreach (var directoryNode in rootNode.Children.Where(node =>
                 node.Kind == ArchiveTreeNodeKind.Directory &&
                 node.IsExpanded &&
                 !string.IsNullOrWhiteSpace(node.DirectoryPath)))
      {
        state.ExpandedDirectoryPaths.Add(Path.GetFullPath(directoryNode.DirectoryPath));
      }

      foreach (var archiveNode in GetExpandedArchiveNodes(rootNode))
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

      var rootNode = ArchiveTreeNode.CreateRoot("Архивы");
      rootNode.IsExpanded = state.IsRootExpanded;
      rootNode.Children.Add(ArchiveTreeNode.CreatePlaceholder("Загрузка..."));
      ArchivesTreeView.ItemsSource = new ObservableCollection<ArchiveTreeNode> { rootNode };

      if (state.IsRootExpanded || state.ExpandedDirectoryPaths.Count > 0 || state.ExpandedArchivePaths.Count > 0)
      {
        await LoadArchivesIntoRootAsync(rootNode, state);
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
          await ShowFileAsync(_lastSelectedArchivePath, _lastSelectedEntryName, false);
          return;
        }
      }

      await ShowArchiveInGridAsync(_lastSelectedArchivePath, clearEditor: true);
    }

    private async Task RefreshArchiveViewsAfterMutationAsync(string archivePath)
    {
      if (string.IsNullOrWhiteSpace(archivePath))
      {
        return;
      }

      var normalizedArchivePath = Path.GetFullPath(archivePath);
      InvalidateArchiveCaches(archivePath);

      _lastSelectedArchivePath = normalizedArchivePath;
      _lastSelectedEntryName = null;

      await RefreshTreePreservingStateAsync(preservePanels: true);
    }

    private void InvalidateArchiveCaches(string archivePath)
    {
      if (string.IsNullOrWhiteSpace(archivePath))
      {
        return;
      }

      _archiveEntriesCache.Remove(archivePath);
      _manifestCache.Remove(archivePath);

      var normalizedArchivePath = Path.GetFullPath(archivePath);
      if (!string.Equals(normalizedArchivePath, archivePath, StringComparison.OrdinalIgnoreCase))
      {
        _archiveEntriesCache.Remove(normalizedArchivePath);
        _manifestCache.Remove(normalizedArchivePath);
      }
    }

    private static bool IsSameArchivePath(string firstPath, string secondPath)
    {
      if (string.IsNullOrWhiteSpace(firstPath) || string.IsNullOrWhiteSpace(secondPath))
      {
        return false;
      }

      return string.Equals(Path.GetFullPath(firstPath), Path.GetFullPath(secondPath), StringComparison.OrdinalIgnoreCase);
    }

    private void ResetTree()
    {
      var rootNode = ArchiveTreeNode.CreateRoot("Архивы");
      rootNode.Children.Add(ArchiveTreeNode.CreatePlaceholder("Загрузка..."));
      ArchivesTreeView.ItemsSource = new ObservableCollection<ArchiveTreeNode> { rootNode };
      ClearFilePanels();
    }

    private void ClearFilePanels()
    {
      ApplyGridItemsSource(Array.Empty<ArchiveEntryInfo>());
      ArchiveFilesDataGrid.SelectedItem = null;
      FileContentTextBox.Content = null;
      FilesHintTextBlock.Text = "Выберите архив для просмотра файлов.";
      FileContentTextBox.Text = string.Empty;
      EditorHintTextBlock.Text = "Выберите файл в архиве для просмотра.";
      UpdateActionButtons();
      UpdateRightPanels(isFilesVisible: false, isEditorVisible: false);
    }

    private async Task LoadArchivesIntoRootAsync(ArchiveTreeNode rootNode, TreeRefreshState? state = null)
    {
      if (!HasPlaceholder(rootNode))
      {
        return;
      }

      rootNode.Children.Clear();
      var expandedArchiveNodes = new List<ArchiveTreeNode>();

      var directoryPaths = await Task.Run(() => ArchiveDirectoryService.GetArchiveDirectories(_archivesFolderPath));
      var rootArchivePaths = await Task.Run(() => ArchiveDirectoryService.GetArchivesInDirectory(_archivesFolderPath));

      if (directoryPaths.Count == 0 && rootArchivePaths.Count == 0)
      {
        rootNode.Children.Add(ArchiveTreeNode.CreatePlaceholder("Архивы не найдены."));
        return;
      }

      foreach (var directoryPath in directoryPaths)
      {
        var fullDirectoryPath = Path.GetFullPath(directoryPath);
        var isExpanded = state?.ExpandedDirectoryPaths.Contains(fullDirectoryPath) == true;

        var directoryNode = ArchiveTreeNode.CreateDirectory(Path.GetFileName(directoryPath), directoryPath);
        directoryNode.IsExpanded = isExpanded;
        directoryNode.Children.Add(ArchiveTreeNode.CreatePlaceholder("Разверните для загрузки архивов..."));
        rootNode.Children.Add(directoryNode);
      }

      foreach (var archivePath in rootArchivePaths)
      {
        var fullArchivePath = Path.GetFullPath(archivePath);
        var isExpanded = state?.ExpandedArchivePaths.Contains(fullArchivePath) == true;

        var archiveNode = ArchiveTreeNode.CreateArchive(Path.GetFileName(archivePath), archivePath);
        archiveNode.IsExpanded = isExpanded;
        archiveNode.Children.Add(ArchiveTreeNode.CreatePlaceholder("Разверните для загрузки файлов..."));
        rootNode.Children.Add(archiveNode);

        if (isExpanded)
        {
          expandedArchiveNodes.Add(archiveNode);
        }
      }

      foreach (var directoryNode in rootNode.Children.Where(node =>
                 node.Kind == ArchiveTreeNodeKind.Directory &&
                 node.IsExpanded &&
                 !string.IsNullOrWhiteSpace(node.DirectoryPath)))
      {
        await LoadArchivesIntoDirectoryAsync(directoryNode, state?.ExpandedArchivePaths);
      }

      foreach (var expandedNode in expandedArchiveNodes)
      {
        await LoadArchiveFilesIntoTreeAsync(expandedNode);
      }
    }

    private static IEnumerable<ArchiveTreeNode> GetExpandedArchiveNodes(ArchiveTreeNode rootNode)
    {
      foreach (var node in rootNode.Children)
      {
        if (node.Kind == ArchiveTreeNodeKind.Archive &&
            node.IsExpanded &&
            !string.IsNullOrWhiteSpace(node.ArchivePath))
        {
          yield return node;
        }

        if (node.Kind != ArchiveTreeNodeKind.Directory)
        {
          continue;
        }

        foreach (var childArchiveNode in node.Children.Where(child =>
                   child.Kind == ArchiveTreeNodeKind.Archive &&
                   child.IsExpanded &&
                   !string.IsNullOrWhiteSpace(child.ArchivePath)))
        {
          yield return childArchiveNode;
        }
      }
    }

    private async Task LoadArchivesIntoDirectoryAsync(ArchiveTreeNode directoryNode, ISet<string>? expandedArchivePaths = null)
    {
      if (directoryNode.Kind != ArchiveTreeNodeKind.Directory ||
          string.IsNullOrWhiteSpace(directoryNode.DirectoryPath) ||
          !HasPlaceholder(directoryNode))
      {
        return;
      }

      directoryNode.Children.Clear();

      var archivePaths = await Task.Run(() => ArchiveDirectoryService.GetArchivesInDirectory(directoryNode.DirectoryPath));
      if (archivePaths.Count == 0)
      {
        directoryNode.Children.Add(ArchiveTreeNode.CreatePlaceholder("Архивы не найдены."));
        return;
      }

      foreach (var archivePath in archivePaths)
      {
        var fullArchivePath = Path.GetFullPath(archivePath);
        var isExpanded = expandedArchivePaths != null && expandedArchivePaths.Contains(fullArchivePath);

        var archiveNode = ArchiveTreeNode.CreateArchive(Path.GetFileName(archivePath), archivePath);
        archiveNode.IsExpanded = isExpanded;
        archiveNode.Children.Add(ArchiveTreeNode.CreatePlaceholder("Разверните для загрузки файлов..."));
        directoryNode.Children.Add(archiveNode);

        if (isExpanded)
        {
          await LoadArchiveFilesIntoTreeAsync(archiveNode);
        }
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
          archiveNode.Children.Add(ArchiveTreeNode.CreatePlaceholder("Архив пуст."));
          return;
        }

        foreach (var entry in entries)
        {
          archiveNode.Children.Add(ArchiveTreeNode.CreateFile(entry.EntryName, archiveNode.ArchivePath, entry.EntryName));
        }
      }
      catch (Exception ex)
      {
        archiveNode.Children.Add(ArchiveTreeNode.CreatePlaceholder("Ошибка чтения архива."));
        ShowArchiveNotification("Архивы", GetUserFriendlyArchiveErrorMessage(ex), NotificationType.Error);
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
        try
        {
          return _archiveManager.IntegrityNotifications?.ToList() ?? new List<string>();
        }
        finally
        {
          _archiveManager.CloseArchive();
        }
      }
    }

    private string ReadArchiveEntryTextWithManager(string archivePath, string entryName)
    {
      lock (_archiveManagerSync)
      {
        EnsureArchiveOpenedInManagerCore(archivePath);
        try
        {
          return _archiveManager.GetFileText(entryName);
        }
        finally
        {
          _archiveManager.CloseArchive();
        }
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

    private IReadOnlyList<ArchiveEntryInfo> ReadArchiveEntries(string archivePath)
    {
      var items = new List<ArchiveEntryInfo>();

      using (var encryptionSession = ArchiveEncryptionSession.Acquire(archivePath))
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

          ArchiveEntryInfo? info = GetEntryInfoAsync(archivePath, entry).GetAwaiter().GetResult();
          if (info != null)
          {
            items.Add(info);
          }
        }
      }

      return items
        .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
        .ToList();
    }

    private async Task<ArchiveEntryInfo?> GetEntryInfoAsync(string archivePath, ZipArchiveEntry entry)
    {
      var manifest = await GetManifestCacheAsync(archivePath);
      manifest.TryGetValue(NormalizeEntryName(entry.FullName), out var creationDate);
      if (creationDate == default)
      {
        creationDate = entry.LastWriteTime.LocalDateTime;
      }

      Regex CommandStartRegex = new(@"^\s*\d+\s+\S+", RegexOptions.Compiled);
      var text = await Task.Run(() => ReadArchiveEntryTextWithManager(archivePath, NormalizeEntryName(entry.FullName)));
      if (string.IsNullOrWhiteSpace(text))
        return null;

      var lines = text
           .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
           .Select(l => l.Trim())
           .ToList();

      if (lines.Count == 0)
        return null;

      var startIndex = lines.FindIndex(l => CommandStartRegex.IsMatch(l));

      if (startIndex < 0)
        return null;

      var firstLine = Regex.Replace(lines[startIndex], @"^\s*\d+\s+\S+\s*", string.Empty);
      var starIndex = firstLine.IndexOf('*');

      string? name = null;
      string? nameOk = null;
      string? opk = null;
      string? opkFileName = null;
      string? ik = null;
      string? order = null;
      string? department = null;
      string? comment = null;
      List<string> kd = new();

      if (starIndex >= 0)
      {
        name = firstLine.Substring(0, starIndex).Trim();
        nameOk = firstLine[(starIndex + 1)..].Trim();
      }
      else
      {
        name = firstLine.Trim();
      }

      foreach (var line in lines.Skip(1))
      {
        var temp = Regex.Replace(line.ToLowerInvariant(), @"\s+", string.Empty);
        var eqIndex = line.IndexOf('=');
        var value = string.Empty;
        if (eqIndex >= 0)
        {
          value = line[(eqIndex + 1)..].Trim();
        }
        if (temp.StartsWith("опк"))
        {
          opk = value;
        }
        else if (temp.StartsWith("ик"))
        {
          ik = value;
        }
        else if (temp.StartsWith("кд"))
        {
          kd.Add(value);
        }
        else if (temp.StartsWith("заказ"))
        {
          order = value;
        }
        else if (temp.StartsWith("цех"))
        {
          department = value;
        }
        else if (temp.StartsWith("прим"))
        {
          comment = value;
        }
        opkFileName = entry.Name;
      }
      return new ArchiveEntryInfo(
        archivePath,
        NormalizeEntryName(entry.FullName),
        name,
        nameOk,
        order,
        opkFileName,
        kd,
        department,
        comment,
        opk,
        ik,
        creationDate);
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
      FilesPanel.Visibility = Visibility.Visible;

      ArchiveFilesDataGrid.Visibility =
          isFilesVisible ? Visibility.Visible : Visibility.Collapsed;

      ArchiveFilesTextBlock.Visibility = Visibility.Visible;

      EditorPanel.Visibility =
          isEditorVisible ? Visibility.Visible : Visibility.Collapsed;

      if (isEditorVisible)
      {
        FilesRowDefinition.Height = new GridLength(1, GridUnitType.Star);
        EditorRowDefinition.Height = new GridLength(1, GridUnitType.Star);

        EditorRowDefinition.MinHeight = 120;

        RightSplitter.Visibility = Visibility.Visible;
        SplitterRowDefinition.MinHeight = 4;
      }
      else
      {
        FilesRowDefinition.Height = new GridLength(1, GridUnitType.Star);

        EditorRowDefinition.Height = new GridLength(0);
        EditorRowDefinition.MinHeight = 0;

        RightSplitter.Visibility = Visibility.Collapsed;
        SplitterRowDefinition.MinHeight = 0;
      }
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

    private void UpdateActionButtons()
    {
      var hasArchive = !string.IsNullOrWhiteSpace(_lastSelectedArchivePath) && File.Exists(_lastSelectedArchivePath);
      ArchiveActionsPanel.Visibility = hasArchive ? Visibility.Visible : Visibility.Collapsed;
      DeleteArchiveFileButton.Visibility = hasArchive && !string.IsNullOrWhiteSpace(_lastSelectedEntryName)
        ? Visibility.Visible
        : Visibility.Collapsed;
    }

    private async Task ShowArchiveInGridAsync(string archivePath, bool clearEditor)
    {
      var integrityNotifications = await Task.Run(() => OpenArchiveInManager(archivePath));
      var entries = await GetArchiveEntriesAsync(archivePath);

      ApplyGridItemsSource(entries);
      var baseHint = entries.Count == 0
        ? "Архив пуст."
        : "Выберите файл в архиве для просмотра..";
      FilesHintTextBlock.Text = integrityNotifications.Count == 0
        ? baseHint
        : $"{baseHint} Integrity warnings: {integrityNotifications.Count}.";

      if (clearEditor)
      {
        _lastSelectedEntryName = null;
        ArchiveFilesDataGrid.SelectedItem = null;
        FileContentTextBox.Content = null;
        FileContentTextBox.Text = string.Empty;
        EditorHintTextBlock.Text = "Выберите файл в архиве для просмотра..";
        UpdateRightPanels(isFilesVisible: true, isEditorVisible: false);
      }
      else
      {
        var hasEditorText = !string.IsNullOrWhiteSpace(FileContentTextBox.Text);
        UpdateRightPanels(isFilesVisible: true, isEditorVisible: hasEditorText);
      }

      UpdateActionButtons();
    }

    private async Task ShowFileAsync(string archivePath, string entryName, bool fromGrid)
    {
      await ShowArchiveInGridAsync(archivePath, clearEditor: false);
      var text = await Task.Run(() => ReadArchiveEntryTextWithManager(archivePath, entryName));

      var textEditor = new TextEditorUI(FileType.OPKW);
      textEditor.Text = text;
      textEditor.IsReadOnly = true;

      // подсветка
      if (!textEditor.TextArea.TextView.LineTransformers
          .OfType<BracesCommentColorizer>()
          .Any())
      {
        textEditor.TextArea.TextView.LineTransformers
            .Add(new BracesCommentColorizer());
      }

      FileContentTextBox.Content = textEditor;

      EditorHintTextBlock.Text = "Содержимое файла доступно только для чтения.";
      UpdateActionButtons();
      UpdateRightPanels(true, true);
      if (!fromGrid)
      {
        var normalized = NormalizeEntryName(entryName);

        var selectedRow = ArchiveFilesDataGrid.Items
            .Cast<ArchiveEntryInfo>()
            .FirstOrDefault(x =>
                string.Equals(x.EntryName, normalized, StringComparison.OrdinalIgnoreCase));

        await SelectGridRow(selectedRow);
      }
    }

    private async Task LoadFileAsync(string archivePath, string entryName)
    {
      var text = await Task.Run(() =>
          ReadArchiveEntryTextWithManager(archivePath, entryName));

      FileContentTextBox.Text = text;
      EditorHintTextBlock.Text = "Содержимое файла доступно только для чтения.";

      UpdateActionButtons();
      UpdateRightPanels(true, true);
    }

    private async Task SelectGridRow(object selectedRow)
    {
      _suppressGridSelection = true;

      ArchiveFilesDataGrid.SelectedItem = selectedRow;
      ArchiveFilesDataGrid.ScrollIntoView(selectedRow);

      await Task.Yield();

      _suppressGridSelection = false;
    }

    public void ShowCreateArchiveDialog()
    {
      if (!Dispatcher.CheckAccess())
      {
        Dispatcher.BeginInvoke(new Action(ShowCreateArchiveDialog));
        return;
      }

      BeginCreateArchiveWorkflow();
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

        if (node.Kind == ArchiveTreeNodeKind.Directory)
        {
          await LoadArchivesIntoDirectoryAsync(node);
          return;
        }

        if (node.Kind == ArchiveTreeNodeKind.Archive)
        {
          await LoadArchiveFilesIntoTreeAsync(node);
        }
      }
      catch (Exception ex)
      {
        ShowArchiveNotification("Архивы", GetUserFriendlyArchiveErrorMessage(ex), NotificationType.Error);
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

    private ArchiveTreeNode? GetContextNode()
    {
      return _contextMenuNode ?? (ArchivesTreeView.SelectedItem as ArchiveTreeNode);
    }

    private void ArchivesTreeContextMenu_Opened(object sender, RoutedEventArgs e)
    {
      var node = GetContextNode();
      var isRoot = node?.Kind == ArchiveTreeNodeKind.Root;
      var isDirectory = node?.Kind == ArchiveTreeNodeKind.Directory;
      var isArchive = node?.Kind == ArchiveTreeNodeKind.Archive;
      var isFile = node?.Kind == ArchiveTreeNodeKind.File;

      CreateArchiveMenuItem.Visibility = isRoot || isDirectory ? Visibility.Visible : Visibility.Collapsed;
      OpenArchiveMenuItem.Visibility = isArchive ? Visibility.Visible : Visibility.Collapsed;
      DeleteArchiveMenuItem.Visibility = isArchive ? Visibility.Visible : Visibility.Collapsed;
      OpenArchiveFileMenuItem.Visibility = isFile ? Visibility.Visible : Visibility.Collapsed;
      DeleteArchiveFileMenuItem.Visibility = isFile ? Visibility.Visible : Visibility.Collapsed;

      if (!isRoot && !isDirectory && !isArchive && !isFile)
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

    private void CreateArchiveMenuItem_Click(object sender, RoutedEventArgs e)
    {
      BeginCreateArchiveWorkflow(GetPreferredDirectoryPath(GetContextNode()));
    }

    private async void OpenArchiveMenuItem_Click(object sender, RoutedEventArgs e)
    {
      var node = GetContextNode();
      if (node?.Kind != ArchiveTreeNodeKind.Archive || string.IsNullOrWhiteSpace(node.ArchivePath))
      {
        return;
      }

      await OpenArchiveAsync(node.ArchivePath);
    }

    private void AddFileToArchiveMenuItem_Click(object sender, RoutedEventArgs e)
    {
      var node = GetContextNode();
      if (node?.Kind != ArchiveTreeNodeKind.Archive || string.IsNullOrWhiteSpace(node.ArchivePath))
      {
        return;
      }

      AddFileToArchive(node.ArchivePath);
    }

    private void DeleteArchiveMenuItem_Click(object sender, RoutedEventArgs e)
    {
      var node = GetContextNode();
      if (node?.Kind != ArchiveTreeNodeKind.Archive || string.IsNullOrWhiteSpace(node.ArchivePath))
      {
        return;
      }

      DeleteArchive(node.ArchivePath, Path.GetFileNameWithoutExtension(node.ArchivePath));
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

      await OpenArchiveFileAsync(node.ArchivePath, node.EntryName);
    }

    private void DeleteArchiveFileMenuItem_Click(object sender, RoutedEventArgs e)
    {
      var node = GetContextNode();
      if (node?.Kind != ArchiveTreeNodeKind.File ||
          string.IsNullOrWhiteSpace(node.ArchivePath) ||
          string.IsNullOrWhiteSpace(node.EntryName))
      {
        return;
      }

      DeleteArchiveFile(node.ArchivePath, node.EntryName, node.DisplayName);
    }

    private void AddFileToArchiveButton_Click(object sender, RoutedEventArgs e)
    {
      if (!string.IsNullOrWhiteSpace(_lastSelectedArchivePath))
      {
        AddFileToArchive(_lastSelectedArchivePath);
      }
    }

    private void DeleteArchiveButton_Click(object sender, RoutedEventArgs e)
    {
      if (!string.IsNullOrWhiteSpace(_lastSelectedArchivePath))
      {
        DeleteArchive(_lastSelectedArchivePath, Path.GetFileNameWithoutExtension(_lastSelectedArchivePath));
      }
    }

    private void DeleteArchiveFileButton_Click(object sender, RoutedEventArgs e)
    {
      if (!string.IsNullOrWhiteSpace(_lastSelectedArchivePath) &&
          !string.IsNullOrWhiteSpace(_lastSelectedEntryName))
      {
        DeleteArchiveFile(_lastSelectedArchivePath, _lastSelectedEntryName, Path.GetFileName(_lastSelectedEntryName));
      }
    }

    private void BeginCreateArchiveWorkflow(string? initialDirectoryPath = null)
    {
      try
      {
        var createdArchivePath = ShowArchiveCreationDialog(initialDirectoryPath);
        if (string.IsNullOrWhiteSpace(createdArchivePath))
        {
          return;
        }

        var archiveDisplayName = Path.GetFileNameWithoutExtension(createdArchivePath);
        ShowArchiveNotification(
          "Создание архива",
          $"Архив '{archiveDisplayName}' успешно создан.",
          NotificationType.Success);
      }
      catch (Exception ex)
      {
        ShowArchiveNotification(
          "Создание архива",
          GetUserFriendlyCreateArchiveErrorMessage(ex),
          NotificationType.Error);
      }
    }

    private async Task OpenArchiveAsync(string archivePath)
    {
      try
      {
        _lastSelectedArchivePath = archivePath;
        _lastSelectedEntryName = null;
        await ShowArchiveInGridAsync(archivePath, clearEditor: true);
      }
      catch (Exception ex)
      {
        ShowArchiveNotification("Открытие архива", GetUserFriendlyArchiveErrorMessage(ex), NotificationType.Error);
      }
    }

    private async Task OpenArchiveFileAsync(string archivePath, string entryName)
    {
      try
      {
        _lastSelectedArchivePath = archivePath;
        _lastSelectedEntryName = entryName;
        await ShowFileAsync(archivePath, entryName, false);
      }
      catch (Exception ex)
      {
        ShowArchiveNotification("Открытие файла", GetUserFriendlyArchiveErrorMessage(ex), NotificationType.Error);
      }
    }

    private void AddFileToArchive(string archivePath)
    {
      var openFileDialog = new OpenFileDialog
      {
        Title = "Выберите файл для добавления",
        Filter = "OPKW file (*.opkw)|*.opkw",
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
          EnsureArchiveOpenedInManagerCore(archivePath);
          _archiveManager.AddFileToOpenedArchive(openFileDialog.FileName);
        }
      }
      catch (Exception ex)
      {
        ShowArchiveNotification("Добавление файла", GetUserFriendlyArchiveErrorMessage(ex), NotificationType.Error);
      }
    }

    private void DeleteArchive(string archivePath, string displayName)
    {
      var confirmation = MessageBox.Show(
        Window.GetWindow(this),
        $"Удалить архив '{displayName}'?",
        "Удаление архива",
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
          _archiveManager.DeleteArchive(archivePath);
        }
      }
      catch (Exception ex)
      {
        ShowArchiveNotification("Удаление архива", GetUserFriendlyArchiveErrorMessage(ex), NotificationType.Error);
      }
    }

    private void DeleteArchiveFile(string archivePath, string entryName, string displayName)
    {
      var confirmation = MessageBox.Show(
        Window.GetWindow(this),
        $"Удалить файл '{displayName}' из архива?",
        "Удаление файла",
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
          EnsureArchiveOpenedInManagerCore(archivePath);
          _archiveManager.DeleteFileFromOpenedArchive(entryName);
        }
      }
      catch (Exception ex)
      {
        ShowArchiveNotification("Удаление файла", GetUserFriendlyArchiveErrorMessage(ex), NotificationType.Error);
      }
    }

    private void ConvertToPkwButton_Click(object sender, RoutedEventArgs e)
    {
      var fileText = GetFileContentText();
      if (string.IsNullOrWhiteSpace(fileText))
      {
        ShowArchiveNotification("Конвертация в PKW", "Нет данных для сохранения.", NotificationType.Warning);
        return;
      }

      var suggestedFileName = GetSuggestedPkwFileName();
      SaveFileManager.SaveFileAs(fileText, suggestedFileName);
    }

    private string GetFileContentText()
    {
      if (FileContentTextBox.Content is TextEditorUI contentTextEditor)
      {
        return contentTextEditor.Text ?? string.Empty;
      }

      return FileContentTextBox.Text ?? string.Empty;
    }

    private string GetSuggestedPkwFileName()
    {
      var rawName = string.IsNullOrWhiteSpace(_lastSelectedEntryName)
        ? "converted_from_archive"
        : Path.GetFileNameWithoutExtension(_lastSelectedEntryName);

      return string.IsNullOrWhiteSpace(rawName)
        ? "converted_from_archive"
        : rawName;
    }

    private string GetPreferredDirectoryPath(ArchiveTreeNode? node = null)
    {
      node ??= ArchivesTreeView.SelectedItem as ArchiveTreeNode;
      if (node == null)
      {
        return GetCurrentArchiveDirectoryPath() ?? _archivesFolderPath;
      }

      return node.Kind switch
      {
        ArchiveTreeNodeKind.Directory when !string.IsNullOrWhiteSpace(node.DirectoryPath) => node.DirectoryPath,
        ArchiveTreeNodeKind.Archive when !string.IsNullOrWhiteSpace(node.ArchivePath) => Path.GetDirectoryName(Path.GetFullPath(node.ArchivePath)) ?? _archivesFolderPath,
        ArchiveTreeNodeKind.File when !string.IsNullOrWhiteSpace(node.ArchivePath) => Path.GetDirectoryName(Path.GetFullPath(node.ArchivePath)) ?? _archivesFolderPath,
        _ => GetCurrentArchiveDirectoryPath() ?? _archivesFolderPath,
      };
    }

    private string? GetCurrentArchiveDirectoryPath()
    {
      return string.IsNullOrWhiteSpace(_lastSelectedArchivePath)
        ? null
        : Path.GetDirectoryName(Path.GetFullPath(_lastSelectedArchivePath));
    }

    private string? ShowArchiveCreationDialog(string? initialDirectoryPath)
    {
      var archivesRootPath = ArchiveDirectoryService.ResolveArchivesRootPath();
      var dialog = CreateDialogWindow("Создание архива");
      var shell = CreateDialogShell();

      var layout = new Grid
      {
        MinWidth = 460,
      };
      layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
      layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
      layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
      layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
      layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

      var titleLabel = new TextBlock
      {
        Text = "Выберите каталог для нового архива:",
        Margin = new Thickness(0, 0, 0, 4),
        Foreground = GetThemeBrush("ForegroundSolidColorBrush", Colors.Black),
        FontFamily = Application.Current?.Resources["WinstonMedium"] as FontFamily,
        FontSize = 16,
        TextWrapping = TextWrapping.Wrap,
      };

      var selectorGrid = new Grid
      {
        Margin = new Thickness(0, 8, 0, 0),
      };
      selectorGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
      selectorGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

      var directoryPlaceholderBorder = CreateDialogPlaceholderBorder();
      var directoryPlaceholderText = new TextBlock
      {
        Text = "В хранилище архивов еще не созданы каталоги.",
        Foreground = GetThemeBrush("ForegrounfBrushes45", Color.FromArgb(180, 64, 64, 64)),
        FontSize = 15,
        TextWrapping = TextWrapping.Wrap,
        VerticalAlignment = VerticalAlignment.Center,
      };
      directoryPlaceholderBorder.Child = directoryPlaceholderText;

      var directorySelector = new ComboBox
      {
        MinWidth = 280,
        Height = 42,
        VerticalContentAlignment = VerticalAlignment.Center,
      };
      ApplyArchiveDirectorySelectorStyle(directorySelector);

      var createDirectoryButton = new Button
      {
        Content = "Создать каталог",
        MinWidth = 170,
        Margin = new Thickness(12, 0, 0, 0),
      };
      ApplyDialogButtonStyle(createDirectoryButton);

      Grid.SetColumn(directoryPlaceholderBorder, 0);
      Grid.SetColumn(directorySelector, 0);
      Grid.SetColumn(createDirectoryButton, 1);
      selectorGrid.Children.Add(directoryPlaceholderBorder);
      selectorGrid.Children.Add(directorySelector);
      selectorGrid.Children.Add(createDirectoryButton);

      var listLabel = new TextBlock
      {
        Text = "Архивы выбранного каталога:",
        Margin = new Thickness(0, 12, 0, 4),
        Foreground = GetThemeBrush("ForegroundSolidColorBrush", Colors.Black),
        FontFamily = Application.Current?.Resources["WinstonMedium"] as FontFamily,
        FontSize = 16,
        TextWrapping = TextWrapping.Wrap,
      };

      var archivesListBorder = new Border
      {
        Background = GetThemeBrush("PrimarySolidColorBrush", Color.FromRgb(239, 239, 224)),
        BorderBrush = GetThemeBrush("ForegroundSolidColorBrush60", Color.FromArgb(120, 0, 0, 0)),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(10),
        Margin = new Thickness(0, 8, 0, 0),
        Padding = new Thickness(6),
      };

      var archivesListBox = new ListBox
      {
        MinWidth = 400,
        MinHeight = 180,
        MaxHeight = 260,
        Background = Brushes.Transparent,
        BorderThickness = new Thickness(0),
        Foreground = GetThemeBrush("ForegroundSolidColorBrush", Colors.Black),
        FontSize = 15,
        HorizontalContentAlignment = HorizontalAlignment.Stretch,
        VerticalContentAlignment = VerticalAlignment.Center,
      };
      ScrollViewer.SetHorizontalScrollBarVisibility(archivesListBox, ScrollBarVisibility.Disabled);
      ScrollViewer.SetVerticalScrollBarVisibility(archivesListBox, ScrollBarVisibility.Auto);
      ApplyArchiveListItemStyle(archivesListBox);
      archivesListBorder.Child = archivesListBox;

      var buttonsPanel = new StackPanel
      {
        Orientation = Orientation.Horizontal,
        HorizontalAlignment = HorizontalAlignment.Right,
        Margin = new Thickness(0, 12, 0, 0),
      };

      var createArchiveButton = new Button
      {
        Content = "Создать архив",
        MinWidth = 160,
        IsDefault = true,
        Margin = new Thickness(0, 0, 8, 0),
      };
      ApplyDialogButtonStyle(createArchiveButton);

      var cancelButton = new Button
      {
        Content = "Отмена",
        MinWidth = 120,
        IsCancel = true,
      };
      ApplyDialogButtonStyle(cancelButton);

      buttonsPanel.Children.Add(createArchiveButton);
      buttonsPanel.Children.Add(cancelButton);

      Grid.SetRow(titleLabel, 0);
      Grid.SetRow(selectorGrid, 1);
      Grid.SetRow(listLabel, 2);
      Grid.SetRow(archivesListBorder, 3);
      Grid.SetRow(buttonsPanel, 4);
      layout.Children.Add(titleLabel);
      layout.Children.Add(selectorGrid);
      layout.Children.Add(listLabel);
      layout.Children.Add(archivesListBorder);
      layout.Children.Add(buttonsPanel);
      shell.Child = layout;
      dialog.Content = shell;

      void RefreshDirectorySelector(string? selectedDirectoryPath = null)
      {
        var directoryOptions = BuildArchiveDirectoryOptions(archivesRootPath);
        var hasDirectories = directoryOptions.Count > 0;

        directorySelector.ItemsSource = directoryOptions;
        directorySelector.DisplayMemberPath = nameof(ArchiveDirectoryOption.DisplayName);
        directorySelector.Visibility = hasDirectories ? Visibility.Visible : Visibility.Collapsed;
        directoryPlaceholderBorder.Visibility = hasDirectories ? Visibility.Collapsed : Visibility.Visible;

        if (hasDirectories)
        {
          directorySelector.SelectedItem = directoryOptions.FirstOrDefault(option =>
            string.Equals(option.DirectoryPath, selectedDirectoryPath, StringComparison.OrdinalIgnoreCase))
            ?? directoryOptions.FirstOrDefault();
        }
        else
        {
          directorySelector.SelectedItem = null;
        }

        createArchiveButton.IsEnabled = hasDirectories;
      }

      void RefreshArchivesList()
      {
        var selectedDirectoryPath = (directorySelector.SelectedItem as ArchiveDirectoryOption)?.DirectoryPath;
        PopulateArchiveList(archivesListBox, selectedDirectoryPath);
      }

      directorySelector.SelectionChanged += (_, _) => RefreshArchivesList();
      createDirectoryButton.Click += (_, _) =>
      {
        var directoryName = PromptForDirectoryName("new_folder");
        if (string.IsNullOrWhiteSpace(directoryName))
        {
          return;
        }

        try
        {
          var createdDirectoryPath = ArchiveDirectoryService.CreateDirectory(archivesRootPath, directoryName);
          RefreshDirectorySelector(createdDirectoryPath);
          RefreshArchivesList();
        }
        catch (Exception ex)
        {
          ShowArchiveNotification(
            "Создание каталога",
            GetUserFriendlyCreateDirectoryErrorMessage(ex),
            NotificationType.Error);
        }
      };

      createArchiveButton.Click += (_, _) =>
      {
        var selectedDirectoryPath = (directorySelector.SelectedItem as ArchiveDirectoryOption)?.DirectoryPath;
        if (string.IsNullOrWhiteSpace(selectedDirectoryPath))
        {
          ShowArchiveNotification(
            "Создание архива",
            "Сначала создайте каталог в хранилище архивов.",
            NotificationType.Warning);
          return;
        }

        var suggestedArchiveName = "new_archive";

        while (true)
        {
          var archiveName = PromptForArchiveName(suggestedArchiveName);
          if (string.IsNullOrWhiteSpace(archiveName))
          {
            return;
          }

          suggestedArchiveName = archiveName;

          try
          {
            string createdArchivePath;
            lock (_archiveManagerSync)
            {
              createdArchivePath = _archiveManager.CreateArchive(archiveName, selectedDirectoryPath);
            }

            dialog.Tag = createdArchivePath;
            dialog.DialogResult = true;
            return;
          }
          catch (Exception ex)
          {
            ShowArchiveNotification(
              "Создание архива",
              GetUserFriendlyCreateArchiveErrorMessage(ex),
              NotificationType.Error);
          }
        }
      };

      dialog.Loaded += (_, _) =>
      {
        RefreshDirectorySelector(initialDirectoryPath);
        RefreshArchivesList();
      };

      return dialog.ShowDialog() == true
        ? dialog.Tag as string
        : null;
    }

    private string? PromptForDirectoryName(string suggestedDirectoryName)
    {
      var dialog = CreateDialogWindow("Создание каталога");
      var shell = CreateDialogShell();

      var layout = new Grid
      {
        MinWidth = 420,
      };
      layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
      layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
      layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

      var label = new TextBlock
      {
        Text = "Введите название нового каталога:",
        Margin = new Thickness(0, 0, 0, 4),
        Foreground = GetThemeBrush("ForegroundSolidColorBrush", Colors.Black),
        FontFamily = Application.Current?.Resources["WinstonMedium"] as FontFamily,
        FontSize = 16,
        TextWrapping = TextWrapping.Wrap,
      };

      var inputBorder = new Border
      {
        Background = GetThemeBrush("PrimarySolidColorBrush", Color.FromRgb(239, 239, 224)),
        BorderBrush = GetThemeBrush("ForegroundSolidColorBrush60", Color.FromArgb(120, 0, 0, 0)),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(10),
        Margin = new Thickness(0, 8, 0, 0),
        Padding = new Thickness(10, 8, 10, 8),
      };

      var inputBox = new TextBox
      {
        MinWidth = 360,
        Background = Brushes.Transparent,
        BorderThickness = new Thickness(0),
        Text = string.IsNullOrWhiteSpace(suggestedDirectoryName) ? "new_folder" : suggestedDirectoryName,
        Foreground = GetThemeBrush("ForegroundSolidColorBrush", Colors.Black),
        FontSize = 15,
      };

      inputBorder.Child = inputBox;

      var buttonsPanel = new StackPanel
      {
        Orientation = Orientation.Horizontal,
        HorizontalAlignment = HorizontalAlignment.Right,
        Margin = new Thickness(0, 12, 0, 0),
      };

      var createButton = new Button
      {
        Content = "Создать",
        MinWidth = 140,
        IsDefault = true,
        Margin = new Thickness(0, 0, 8, 0),
      };
      ApplyDialogButtonStyle(createButton);
      createButton.Click += (_, _) => dialog.DialogResult = true;

      var cancelButton = new Button
      {
        Content = "Отмена",
        MinWidth = 120,
        IsCancel = true,
      };
      ApplyDialogButtonStyle(cancelButton);

      buttonsPanel.Children.Add(createButton);
      buttonsPanel.Children.Add(cancelButton);

      Grid.SetRow(label, 0);
      Grid.SetRow(inputBorder, 1);
      Grid.SetRow(buttonsPanel, 2);
      layout.Children.Add(label);
      layout.Children.Add(inputBorder);
      layout.Children.Add(buttonsPanel);
      shell.Child = layout;
      dialog.Content = shell;

      dialog.Loaded += (_, _) =>
      {
        inputBox.Focus();
        inputBox.SelectAll();
      };

      return dialog.ShowDialog() == true
        ? inputBox.Text?.Trim()
        : null;
    }

    private string PromptForArchiveName(string suggestedArchiveName)
    {
      var dialog = CreateDialogWindow("Создание архива");
      var shell = CreateDialogShell();

      var layout = new Grid
      {
        MinWidth = 420,
      };
      layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
      layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
      layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

      var label = new TextBlock
      {
        Text = "Введите название нового архива:",
        Margin = new Thickness(0, 0, 0, 4),
        Foreground = GetThemeBrush("ForegroundSolidColorBrush", Colors.Black),
        FontFamily = Application.Current?.Resources["WinstonMedium"] as FontFamily,
        FontSize = 16,
        TextWrapping = TextWrapping.Wrap,
      };

      var inputBorder = new Border
      {
        Background = GetThemeBrush("PrimarySolidColorBrush", Color.FromRgb(239, 239, 224)),
        BorderBrush = GetThemeBrush("ForegroundSolidColorBrush60", Color.FromArgb(120, 0, 0, 0)),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(10),
        Margin = new Thickness(0, 8, 0, 0),
        Padding = new Thickness(10, 8, 10, 8),
      };

      var inputBox = new TextBox
      {
        MinWidth = 360,
        Background = Brushes.Transparent,
        BorderThickness = new Thickness(0),
        Text = string.IsNullOrWhiteSpace(suggestedArchiveName) ? "new_archive" : suggestedArchiveName,
        Foreground = GetThemeBrush("ForegroundSolidColorBrush", Colors.Black),
        FontSize = 15,
      };

      inputBorder.Child = inputBox;

      var buttonsPanel = new StackPanel
      {
        Orientation = Orientation.Horizontal,
        HorizontalAlignment = HorizontalAlignment.Right,
        Margin = new Thickness(0, 12, 0, 0),
      };

      var createButton = new Button
      {
        Content = "Создать",
        MinWidth = 140,
        IsDefault = true,
        Margin = new Thickness(0, 0, 8, 0),
      };
      ApplyDialogButtonStyle(createButton);
      createButton.Click += (_, _) => dialog.DialogResult = true;

      var cancelButton = new Button
      {
        Content = "Отмена",
        MinWidth = 120,
        IsCancel = true,
      };
      ApplyDialogButtonStyle(cancelButton);

      buttonsPanel.Children.Add(createButton);
      buttonsPanel.Children.Add(cancelButton);

      Grid.SetRow(label, 0);
      Grid.SetRow(inputBorder, 1);
      Grid.SetRow(buttonsPanel, 2);
      layout.Children.Add(label);
      layout.Children.Add(inputBorder);
      layout.Children.Add(buttonsPanel);
      shell.Child = layout;
      dialog.Content = shell;

      dialog.Loaded += (_, _) =>
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
        if (node.Kind == ArchiveTreeNodeKind.Root || node.Kind == ArchiveTreeNodeKind.Directory)
        {
          lock (_archiveManagerSync)
          {
            _archiveManager.CloseArchive();
          }

          _lastSelectedArchivePath = null;
          _lastSelectedEntryName = null;
          ClearFilePanels();
          if (node.Kind == ArchiveTreeNodeKind.Directory)
          {
            FilesHintTextBlock.Text = "Выберите архив внутри каталога.";
          }
          return;
        }

        if (node.Kind == ArchiveTreeNodeKind.Archive && node.ArchivePath != null)
        {
          await OpenArchiveAsync(node.ArchivePath);
          return;
        }

        if (node.Kind == ArchiveTreeNodeKind.File &&
            node.ArchivePath != null &&
            !string.IsNullOrWhiteSpace(node.EntryName))
        {
          await OpenArchiveFileAsync(node.ArchivePath, node.EntryName);
        }
      }
      catch (Exception ex)
      {
        ShowArchiveNotification("Архивы", GetUserFriendlyArchiveErrorMessage(ex), NotificationType.Error);
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
        _lastSelectedEntryName = null;
        UpdateActionButtons();
        return;
      }

      try
      {
        _lastSelectedArchivePath = selected.ArchivePath;
        _lastSelectedEntryName = selected.EntryName;

        var text = await Task.Run(() => ReadArchiveEntryTextWithManager(selected.ArchivePath, selected.EntryName));

        var textEditor = new TextEditorUI(FileType.OPKW);
        textEditor.Text = text;


        // Добавляем подсветку только один раз
        if (!textEditor.TextArea.TextView.LineTransformers
            .OfType<BracesCommentColorizer>()
            .Any())
        {
          textEditor.TextArea.TextView.LineTransformers
              .Add(new BracesCommentColorizer());
        }

        FileContentTextBox.Content = textEditor;
        textEditor.IsReadOnly = true;
        textEditor.TextArea.TextView.Redraw();

        EditorHintTextBlock.Text = "Содержимое файла доступно только для чтения.";
        UpdateActionButtons();
        UpdateRightPanels(isFilesVisible: true, isEditorVisible: true);
      }
      catch (Exception ex)
      {
        ShowArchiveNotification("Архивы", GetUserFriendlyArchiveErrorMessage(ex), NotificationType.Error);
      }
    }

    private Window CreateDialogWindow(string title)
    {
      return new Window
      {
        Title = title,
        Owner = Window.GetWindow(this),
        WindowStartupLocation = WindowStartupLocation.CenterOwner,
        ResizeMode = ResizeMode.NoResize,
        SizeToContent = SizeToContent.WidthAndHeight,
        ShowInTaskbar = false,
        WindowStyle = WindowStyle.None,
        AllowsTransparency = true,
        Background = Brushes.Transparent,
      };
    }

    private Border CreateDialogShell()
    {
      return new Border
      {
        Background = GetThemeBrush("IsCheckedColorSolidColorBrush", Color.FromRgb(230, 232, 236)),
        BorderBrush = GetThemeBrush("ForegroundSolidColorBrush60", Color.FromRgb(120, 130, 140)),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(20),
        Padding = new Thickness(20),
      };
    }

    private List<ArchiveDirectoryOption> BuildArchiveDirectoryOptions(string archivesRootPath)
    {
      return ArchiveDirectoryService.GetArchiveDirectories(archivesRootPath)
        .Select(directoryPath => new ArchiveDirectoryOption(Path.GetFileName(directoryPath), directoryPath))
        .ToList();
    }

    private void PopulateArchiveList(ListBox listBox, string? directoryPath)
    {
      listBox.Items.Clear();

      if (string.IsNullOrWhiteSpace(directoryPath))
      {
        return;
      }

      foreach (var archivePath in ArchiveDirectoryService.GetArchivesInDirectory(directoryPath))
      {
        listBox.Items.Add(new ListBoxItem
        {
          Content = Path.GetFileName(archivePath),
          Tag = archivePath,
        });
      }
    }

    private void ApplyArchiveListItemStyle(ListBox listBox)
    {
      var accentBrush = GetThemeBrush("ActiveForegroundSolidColorBrush80", Color.FromArgb(120, 164, 235, 158));
      var itemStyle = new Style(typeof(ListBoxItem));
      itemStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10, 8, 10, 8)));
      itemStyle.Setters.Add(new Setter(Control.MarginProperty, new Thickness(0, 2, 0, 2)));
      itemStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
      itemStyle.Setters.Add(new Setter(Control.BorderBrushProperty, Brushes.Transparent));
      itemStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));

      var hoverTrigger = new Trigger { Property = ListBoxItem.IsMouseOverProperty, Value = true };
      hoverTrigger.Setters.Add(new Setter(Control.BackgroundProperty, accentBrush));
      itemStyle.Triggers.Add(hoverTrigger);

      var selectedTrigger = new Trigger { Property = ListBoxItem.IsSelectedProperty, Value = true };
      selectedTrigger.Setters.Add(new Setter(Control.BackgroundProperty, accentBrush));
      selectedTrigger.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.SemiBold));
      itemStyle.Triggers.Add(selectedTrigger);

      listBox.ItemContainerStyle = itemStyle;
    }

    private void ApplyArchiveDirectorySelectorStyle(ComboBox comboBox)
    {
      var comboBoxStyle =
        TryFindResource("CustomComboBoxStyle") as Style ??
        Application.Current?.TryFindResource("CustomComboBoxStyle") as Style;

      if (comboBoxStyle != null)
      {
        comboBox.Style = comboBoxStyle;
        comboBox.HorizontalAlignment = HorizontalAlignment.Stretch;
        return;
      }

      comboBox.Background = GetThemeBrush("PrimarySolidColorBrush", Color.FromRgb(239, 239, 224));
      comboBox.Foreground = GetThemeBrush("ForegroundSolidColorBrush", Colors.Black);
      comboBox.BorderBrush = GetThemeBrush("ForegroundSolidColorBrush60", Color.FromArgb(120, 0, 0, 0));
      comboBox.BorderThickness = new Thickness(1);
      comboBox.Padding = new Thickness(10, 6, 10, 6);
    }

    private Border CreateDialogPlaceholderBorder()
    {
      return new Border
      {
        MinWidth = 280,
        Height = 42,
        Background = GetThemeBrush("PrimarySolidColorBrush", Color.FromRgb(239, 239, 224)),
        BorderBrush = GetThemeBrush("ForegroundSolidColorBrush60", Color.FromArgb(120, 0, 0, 0)),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(10),
        Padding = new Thickness(12, 0, 12, 0),
      };
    }

    private void ApplyDialogButtonStyle(Button button)
    {
      if (TryFindResource("ButtonStyleV10") is Style style)
      {
        button.Style = style;
      }

      button.Height = 44;
      button.Padding = new Thickness(14, 6, 14, 6);
      button.FontSize = 16;
    }

    private Brush GetThemeBrush(string key, Color fallbackColor)
    {
      if (TryFindResource(key) is Brush brush)
      {
        return brush;
      }

      if (Application.Current?.Resources[key] is Brush appBrush)
      {
        return appBrush;
      }

      return new SolidColorBrush(fallbackColor);
    }

    private void ShowArchiveNotification(string title, string message, NotificationType notificationType)
    {
      NotificationHostService.Instance.Show(title, message, notificationType);
    }

    private static string GetUserFriendlyCreateArchiveErrorMessage(Exception ex)
    {
      if (ex is InvalidOperationException invalidOperation &&
          invalidOperation.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
      {
        return "Архив с таким именем уже существует. Выберите другое имя.";
      }

      if (ex is ArgumentException)
      {
        return "Имя архива содержит недопустимые символы.";
      }

      if (ex is DirectoryNotFoundException directoryNotFoundException)
      {
        return directoryNotFoundException.Message;
      }

      if (ex is IOException ioException &&
          ioException.Message.Contains("being used by another process", StringComparison.OrdinalIgnoreCase))
      {
        return "Архив сейчас используется другим процессом. Повторите попытку.";
      }

      return "Не удалось создать архив.";
    }

    private static string GetUserFriendlyCreateDirectoryErrorMessage(Exception ex)
    {
      if (ex is InvalidOperationException invalidOperation &&
          invalidOperation.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
      {
        return "Каталог с таким именем уже существует. Выберите другое имя.";
      }

      if (ex is ArgumentException)
      {
        return "Имя каталога содержит недопустимые символы.";
      }

      if (ex is DirectoryNotFoundException directoryNotFoundException)
      {
        return directoryNotFoundException.Message;
      }

      return "Не удалось создать каталог.";
    }

    private static string GetUserFriendlyArchiveErrorMessage(Exception ex)
    {
      if (ex is InvalidOperationException invalidOperation &&
          invalidOperation.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
      {
        return "Файл или архив с таким именем уже существует.";
      }

      if (ex is FileNotFoundException)
      {
        return "Архив или файл не найден.";
      }

      if (ex is IOException ioException &&
          ioException.Message.Contains("being used by another process", StringComparison.OrdinalIgnoreCase))
      {
        return "Архив сейчас используется другим процессом. Повторите попытку.";
      }

      if (ex is InvalidDataException invalidDataException)
      {
        return invalidDataException.Message;
      }

      if (ex is ArgumentException argumentException && !string.IsNullOrWhiteSpace(argumentException.Message))
      {
        return argumentException.Message;
      }

      return "Не удалось выполнить операцию с архивом.";
    }

    private sealed class TreeRefreshState
    {
      public bool IsRootExpanded { get; set; }
      public HashSet<string> ExpandedDirectoryPaths { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      public HashSet<string> ExpandedArchivePaths { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class ArchiveDirectoryOption
    {
      public ArchiveDirectoryOption(string displayName, string directoryPath)
      {
        DisplayName = displayName;
        DirectoryPath = directoryPath;
      }

      public string DisplayName { get; }
      public string DirectoryPath { get; }
    }

  }
}
