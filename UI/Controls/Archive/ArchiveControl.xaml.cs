using Ask.Core.Shared.Metadata.Static;
using System.Collections.ObjectModel;
using Ask.UI.Features.Notifications.Models;
using Ask.UI.Infrastructure.UI.Overlay.Notifications.Runtime;
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
    private static readonly string[] ArchivesFolderCandidates = new[]
    {
      Path.Combine(AppContext.BaseDirectory, FileLocations.ArchiveDirectory),
      Path.Combine(Directory.GetCurrentDirectory(), FileLocations.ArchiveDirectory),
    };

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
    private async Task<Dictionary<string, DateTime>> GetManifestCacheAsync(string archivePath)
    {
      if (_manifestCache.TryGetValue(archivePath, out var cached))
        return cached;

      var result = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

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

      var rootNode = ArchiveTreeNode.CreateRoot("Архивы");
      rootNode.IsExpanded = state.IsRootExpanded;
      rootNode.Children.Add(ArchiveTreeNode.CreatePlaceholder("Загрузка..."));
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
      var rootNode = ArchiveTreeNode.CreateRoot("Архивы");
      rootNode.Children.Add(ArchiveTreeNode.CreatePlaceholder("Загрузка..."));
      ArchivesTreeView.ItemsSource = new ObservableCollection<ArchiveTreeNode> { rootNode };
      ClearFilePanels();
    }

    private void ClearFilePanels()
    {
      ApplyGridItemsSource(Array.Empty<ArchiveEntryInfo>());
      FilesHintTextBlock.Text = "Выберите архив для просмотра файлов.";
      FileContentTextBox.Text = string.Empty;
      EditorHintTextBlock.Text = "Выберите файл в архиве для просмотра.";
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
        rootNode.Children.Add(ArchiveTreeNode.CreatePlaceholder("Архивы не найдены."));
        return;
      }

      foreach (var archivePath in archivePaths)
      {
        var fullArchivePath = Path.GetFullPath(archivePath);
        var isExpanded = expandedArchivePaths != null && expandedArchivePaths.Contains(fullArchivePath);

        var archiveNode = ArchiveTreeNode.CreateArchive(Path.GetFileName(archivePath), archivePath);
        archiveNode.IsExpanded = isExpanded;
        archiveNode.Children.Add(ArchiveTreeNode.CreatePlaceholder("Разверните для загрузки файлов..."));
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

    private IReadOnlyList<ArchiveEntryInfo> ReadArchiveEntries(string archivePath)
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

      Regex CommandStartRegex = new (@"^\s*\d+\s+\S+", RegexOptions.Compiled);
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
        FileContentTextBox.Text = string.Empty;
        EditorHintTextBlock.Text = "Выберите файл в архиве для просмотра..";
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
      EditorHintTextBlock.Text = "Содержимое файла доступно только для чтения.";
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
      //AddFileToArchiveMenuItem.Visibility = isArchive ? Visibility.Visible : Visibility.Collapsed;
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
            createdArchivePath = _archiveManager.CreateArchive(archiveName);
          }

          _archiveEntriesCache.Clear();
          _manifestCache.Clear();
          await RefreshTreePreservingStateAsync(preservePanels: true);

          var archiveDisplayName = Path.GetFileNameWithoutExtension(createdArchivePath);
          ShowArchiveNotification(
            "Создание архива",
            $"Архив '{archiveDisplayName}' успешно создан.",
            NotificationType.Success);
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
        ShowArchiveNotification("Открытие архива", GetUserFriendlyArchiveErrorMessage(ex), NotificationType.Error);
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
          EnsureArchiveOpenedInManagerCore(node.ArchivePath);
          _archiveManager.AddFileToOpenedArchive(openFileDialog.FileName);
        }

        _archiveEntriesCache.Remove(node.ArchivePath);
        _manifestCache.Remove(node.ArchivePath);
        await RefreshTreePreservingStateAsync(preservePanels: true);
      }
      catch (Exception ex)
      {
        ShowArchiveNotification("Добавление файла", GetUserFriendlyArchiveErrorMessage(ex), NotificationType.Error);
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
        $"Удалить архив '{node.DisplayName}'?",
        "Удаление архива",
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
        _manifestCache.Clear();
        await RefreshTreePreservingStateAsync(preservePanels: !activeArchiveDeleted);
      }
      catch (Exception ex)
      {
        ShowArchiveNotification("Удаление архива", GetUserFriendlyArchiveErrorMessage(ex), NotificationType.Error);
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
        ShowArchiveNotification("Открытие файла", GetUserFriendlyArchiveErrorMessage(ex), NotificationType.Error);
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
        $"Удалить файл '{node.DisplayName}' из архива?",
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
          EnsureArchiveOpenedInManagerCore(node.ArchivePath);
          _archiveManager.DeleteFileFromOpenedArchive(node.EntryName);
        }

        _archiveEntriesCache.Remove(node.ArchivePath);
        _manifestCache.Remove(node.ArchivePath);
        await RefreshTreePreservingStateAsync(preservePanels: true);
      }
      catch (Exception ex)
      {
        ShowArchiveNotification("Удаление файла", GetUserFriendlyArchiveErrorMessage(ex), NotificationType.Error);
      }
    }

    private string PromptForArchiveName(string suggestedArchiveName)
    {
      var dialog = new Window
      {
        Title = "Создание архива",
        Owner = Window.GetWindow(this),
        WindowStartupLocation = WindowStartupLocation.CenterOwner,
        ResizeMode = ResizeMode.NoResize,
        SizeToContent = SizeToContent.WidthAndHeight,
        ShowInTaskbar = false,
        WindowStyle = WindowStyle.None,
        AllowsTransparency = true,
        Background = Brushes.Transparent,
      };

      var shell = new Border
      {
        Background = GetThemeBrush("IsCheckedColorSolidColorBrush", Color.FromRgb(230, 232, 236)),
        BorderBrush = GetThemeBrush("ForegroundSolidColorBrush60", Color.FromRgb(120, 130, 140)),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(20),
        Padding = new Thickness(20),
      };

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
        return;
      }

      try
      {
        _lastSelectedArchivePath = selected.ArchivePath;
        _lastSelectedEntryName = selected.EntryName;

        var text = await Task.Run(() => ReadArchiveEntryTextWithManager(selected.ArchivePath, selected.EntryName));
        FileContentTextBox.Text = text;
        EditorHintTextBlock.Text = "Содержимое файла доступно только для чтения.";
        UpdateRightPanels(isFilesVisible: true, isEditorVisible: true);
      }
      catch (Exception ex)
      {
        ShowArchiveNotification("Архивы", GetUserFriendlyArchiveErrorMessage(ex), NotificationType.Error);
      }
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

      if (ex is IOException ioException &&
          ioException.Message.Contains("being used by another process", StringComparison.OrdinalIgnoreCase))
      {
        return "Архив сейчас используется другим процессом. Повторите попытку.";
      }

      return "Не удалось создать архив.";
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
      public HashSet<string> ExpandedArchivePaths { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

  }
}
