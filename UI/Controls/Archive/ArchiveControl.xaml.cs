using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.Core.Shared.Metadata.Static;
using Ask.UI.Features.Notifications.Models;
using Ask.UI.Infrastructure.UI.Overlay.Notifications.Runtime;
using Message;
using MigraDoc.DocumentObjectModel.Tables;
using SQLitePCL;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using UI.Components.MultiEditorMethods;
using UI.Components.SearchControls;
using UI.Controls.TextEditorControl;
using UI.Services.Archive;
using static Ask.LogLib.LoggerUtility;
using Button = System.Windows.Controls.Button;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Orientation = System.Windows.Controls.Orientation;
using Path = System.IO.Path;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using Table = System.Windows.Documents.Table;
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
    private ArchiveClipboardEntry? _archiveClipboardEntry;

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

      if (state.IsRootExpanded || state.ExpandedArchivePaths.Count > 0)
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
      var archivePaths = await Task.Run(() => ArchiveDirectoryService.GetArchivesInDirectory(_archivesFolderPath));
      if (archivePaths.Count == 0)
      {
        rootNode.Children.Add(ArchiveTreeNode.CreatePlaceholder("Архивы не найдены."));
        return;
      }

      foreach (var archivePath in archivePaths)
      {
        var fullArchivePath = Path.GetFullPath(archivePath);
        var isExpanded = state?.ExpandedArchivePaths.Contains(fullArchivePath) == true;

        var archiveNode = ArchiveTreeNode.CreateArchive(Path.GetFileName(archivePath), archivePath);
        archiveNode.IsExpanded = isExpanded;
        archiveNode.Children.Add(ArchiveTreeNode.CreatePlaceholder("Разверните для загрузки файлов..."));
        rootNode.Children.Add(archiveNode);

        if (isExpanded)
        {
          await LoadArchiveFilesIntoTreeAsync(archiveNode);
        }
      }
    }

    private static IEnumerable<ArchiveTreeNode> GetExpandedArchiveNodes(ArchiveTreeNode rootNode)
    {
      foreach (var node in rootNode.Children.Where(node =>
                 node.Kind == ArchiveTreeNodeKind.Archive &&
                 node.IsExpanded &&
                 !string.IsNullOrWhiteSpace(node.ArchivePath)))
      {
        yield return node;
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

      _suppressGridSelection = true;
      try
      {
        ArchiveFilesDataGrid.ItemsSource = _currentGridEntries;
      }
      finally
      {
        _suppressGridSelection = false;
      }
    }

    private bool HasArchiveClipboardEntry()
    {
      return _archiveClipboardEntry != null;
    }

    private void StoreArchiveClipboardEntry(string archivePath, string entryName, string displayName, ArchiveClipboardOperation operation)
    {
      var normalizedArchivePath = Path.GetFullPath(archivePath);
      var normalizedEntryName = NormalizeEntryName(entryName);
      var normalizedDisplayName = Path.GetFileName(string.IsNullOrWhiteSpace(displayName) ? normalizedEntryName : displayName);

      _archiveClipboardEntry = new ArchiveClipboardEntry(
        normalizedArchivePath,
        normalizedEntryName,
        normalizedDisplayName,
        operation);

      LogInformation(
        $"Файл '{normalizedDisplayName}' помещён в буфер архива. Режим: {(operation == ArchiveClipboardOperation.Cut ? "вырезать" : "копировать")}. Источник: '{Path.GetFileNameWithoutExtension(normalizedArchivePath)}'.");

      UpdateActionButtons();
    }

    private void ClearArchiveClipboardEntry(bool wasConsumed)
    {
      if (_archiveClipboardEntry == null)
      {
        return;
      }

      if (wasConsumed)
      {
        LogInformation($"Буфер архива очищен после вставки файла '{_archiveClipboardEntry.DisplayName}'.");
      }

      _archiveClipboardEntry = null;
      UpdateActionButtons();
    }

    private async Task PasteArchiveClipboardToAsync(string targetArchivePath)
    {
      var clipboardEntry = _archiveClipboardEntry;
      if (clipboardEntry == null)
      {
        return;
      }

      var fullTargetArchivePath = Path.GetFullPath(targetArchivePath);
      var sourceArchiveDisplayName = Path.GetFileNameWithoutExtension(clipboardEntry.SourceArchivePath);
      var targetArchiveDisplayName = Path.GetFileNameWithoutExtension(fullTargetArchivePath);

      try
      {
        LogInformation(
          $"Начата вставка файла '{clipboardEntry.DisplayName}' из архива '{sourceArchiveDisplayName}' в архив '{targetArchiveDisplayName}'. Режим: {(clipboardEntry.Operation == ArchiveClipboardOperation.Cut ? "вырезать" : "копировать")}.");

        var insertedEntryName = await Task.Run(() =>
        {
          lock (_archiveManagerSync)
          {
            return clipboardEntry.Operation == ArchiveClipboardOperation.Cut
              ? _archiveManager.MoveFileBetweenArchives(clipboardEntry.SourceArchivePath, clipboardEntry.EntryName, fullTargetArchivePath)
              : _archiveManager.CopyFileBetweenArchives(clipboardEntry.SourceArchivePath, clipboardEntry.EntryName, fullTargetArchivePath);
          }
        });

        var successMessage = clipboardEntry.Operation == ArchiveClipboardOperation.Cut
          ? $"Файл '{Path.GetFileName(insertedEntryName)}' успешно перемещён из архива '{sourceArchiveDisplayName}' в архив '{targetArchiveDisplayName}'."
          : $"Файл '{Path.GetFileName(insertedEntryName)}' успешно скопирован из архива '{sourceArchiveDisplayName}' в архив '{targetArchiveDisplayName}'.";

        LogInformation(successMessage);
        ShowArchiveNotification("Вставка файла", successMessage, NotificationType.Success);

        if (clipboardEntry.Operation == ArchiveClipboardOperation.Cut)
        {
          ClearArchiveClipboardEntry(wasConsumed: true);
        }
        else
        {
          UpdateActionButtons();
        }
      }
      catch (Exception ex)
      {
        LogError(
          $"Ошибка вставки файла '{clipboardEntry.DisplayName}' из архива '{sourceArchiveDisplayName}' в архив '{targetArchiveDisplayName}': {ex}");
        ShowArchiveNotification("Вставка файла", GetUserFriendlyArchiveErrorMessage(ex), NotificationType.Error);
        UpdateActionButtons();
      }
    }

    private void UpdateActionButtons()
    {
      UpdatePanelTitles();

      var hasArchive = !string.IsNullOrWhiteSpace(_lastSelectedArchivePath) && File.Exists(_lastSelectedArchivePath);
      var hasSelectedFile = hasArchive && !string.IsNullOrWhiteSpace(_lastSelectedEntryName);
      var hasClipboardEntry = HasArchiveClipboardEntry();

      ArchiveActionsPanel.Visibility = hasArchive ? Visibility.Visible : Visibility.Collapsed;
      PasteIntoArchiveButton.Visibility = hasArchive && hasClipboardEntry ? Visibility.Visible : Visibility.Collapsed;
      DeleteArchiveFileButton.Visibility = hasSelectedFile ? Visibility.Visible : Visibility.Collapsed;
      CopyArchiveFileButton.Visibility = hasSelectedFile ? Visibility.Visible : Visibility.Collapsed;
      CutArchiveFileButton.Visibility = hasSelectedFile ? Visibility.Visible : Visibility.Collapsed;
      PasteArchiveFileButton.Visibility = hasSelectedFile && hasClipboardEntry ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdatePanelTitles()
    {
      SetPanelTitle(SelectedArchiveNameTextBlock, GetArchiveDisplayName(_lastSelectedArchivePath));
      SetPanelTitle(SelectedArchiveFileNameTextBlock, GetFileDisplayName(_lastSelectedEntryName));
    }

    private static string? GetArchiveDisplayName(string archivePath)
    {
      return string.IsNullOrWhiteSpace(archivePath)
        ? null
        : Path.GetFileName(archivePath);
    }

    private static string? GetFileDisplayName(string entryName)
    {
      return string.IsNullOrWhiteSpace(entryName)
        ? null
        : Path.GetFileName(entryName);
    }

    private static void SetPanelTitle(TextBlock textBlock, string? value)
    {
      var hasValue = !string.IsNullOrWhiteSpace(value);

      textBlock.Text = hasValue ? value : string.Empty;
      textBlock.ToolTip = hasValue ? value : null;
      textBlock.Visibility = hasValue ? Visibility.Visible : Visibility.Collapsed;
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
        _suppressGridSelection = true;
        ArchiveFilesDataGrid.SelectedItem = null;
        _suppressGridSelection = false;
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
      var normalizedEntryName = NormalizeEntryName(entryName);

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
      _lastSelectedArchivePath = archivePath;
      _lastSelectedEntryName = normalizedEntryName;

      EditorHintTextBlock.Text = "Содержимое файла доступно только для чтения.";
      UpdateActionButtons();
      UpdateRightPanels(true, true);
      if (!fromGrid)
      {
        var selectedRow = ArchiveFilesDataGrid.Items
            .Cast<ArchiveEntryInfo>()
            .FirstOrDefault(x =>
                string.Equals(x.EntryName, normalizedEntryName, StringComparison.OrdinalIgnoreCase));

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

      try
      {
        ArchiveFilesDataGrid.SelectedItem = selectedRow;

        if (selectedRow is ArchiveEntryInfo selectedEntry)
        {
          _lastSelectedArchivePath = selectedEntry.ArchivePath;
          _lastSelectedEntryName = selectedEntry.EntryName;
        }

        if (selectedRow != null)
        {
          ArchiveFilesDataGrid.ScrollIntoView(selectedRow);
        }

        await Task.Yield();
      }
      finally
      {
        _suppressGridSelection = false;
      }
    }

    /// <summary>
    /// Открывает диалог создания нового архива.
    /// </summary>
    /// <remarks>
    /// Если вызов происходит не из UI-потока, выполнение будет перенаправлено в диспетчер.
    /// Запускает процесс создания архива с пользовательским вводом имени.
    /// </remarks>
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

    private ArchiveTreeNode GetNodeForPrint()
    {
      return _contextMenuNode ?? ArchivesTreeView.SelectedItem as ArchiveTreeNode;
    }

    private void ArchivesTreeContextMenu_Closed(object sender, RoutedEventArgs e)
    {
      _contextMenuNode = null;
    }

    private async Task<(IReadOnlyList<ArchiveEntryInfo> entries, string archivePath)> EnsureEntriesForPrintAsync()
    {
      var node = GetNodeForPrint();

      if (node == null || node.Kind != ArchiveTreeNodeKind.Archive || node.ArchivePath == null)
        return (null, null);

      var archivePath = node.ArchivePath;

      // Если уже открыт нужный архив и есть данные — используем их
      if (_lastSelectedArchivePath == archivePath &&
          _currentGridEntries != null &&
          _currentGridEntries.Count > 0)
      {
        return (_currentGridEntries, archivePath);
      }

      // Иначе — загружаем
      var entries = await GetArchiveEntriesAsync(archivePath);

      return (entries, archivePath);
    }

    private async Task PrintArchiveCatalogAsync()
    {
      var (entries, archivePath) = await EnsureEntriesForPrintAsync();

      if (entries == null || entries.Count == 0)
      {
        MessageBoxCustom.Show("Нет данных для печати", "Ошибка печати", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }

      var archiveName = System.IO.Path.GetFileName(archivePath);

      var printDialog = new PrintDialog();

      var printCapabilities = printDialog.PrintQueue.GetPrintCapabilities(printDialog.PrintTicket);

      double hardMarginX = printCapabilities.PageImageableArea.OriginWidth;
      double hardMarginY = printCapabilities.PageImageableArea.OriginHeight;


      if (printDialog.ShowDialog() == true)
      {
        var document = CreatePrintDocument(
          entries,
          archiveName,
          hardMarginX,
          hardMarginY,
          printDialog.PrintableAreaWidth,
          printDialog.PrintableAreaHeight);
        IDocumentPaginatorSource idpSource = document;
        printDialog.PrintDocument(idpSource.DocumentPaginator, "Каталог архива");
      }
    }

    private FlowDocument CreatePrintDocument(
      IReadOnlyList<ArchiveEntryInfo> entries,
      string archiveName,
      double hardMarginX,
      double hardMarginY,
      double printableAreaWidth,
      double printableAreaHeight)
    {
      var cellPadding = new Thickness(2);
      
      var doc = new FlowDocument
      {
        FontFamily = new FontFamily("Segoe UI"),
        FontSize = 9.5,
        PagePadding = new Thickness(
          hardMarginX,
          hardMarginY,
          hardMarginX,
          hardMarginY),
        PageWidth = printableAreaWidth + hardMarginX * 2,
        PageHeight = printableAreaHeight + hardMarginY * 2,
        ColumnWidth = double.PositiveInfinity
      };
      
      var availableTableWidth = Math.Max(0, printableAreaWidth - doc.PagePadding.Left - doc.PagePadding.Right);

      // Заголовок
      doc.Blocks.Add(new Paragraph(new Run($"Каталог архива {archiveName}"))
      {
        FontSize = 14,
        FontWeight = FontWeights.Bold,
        TextAlignment = TextAlignment.Center,
        Margin = new Thickness(0, 0, 0, 12)
      });

      var table = new Table
      {
        CellSpacing = 0
      };

      double MeasureColumnWidth(string sampleText, FontWeight fontWeight)
      {
        var formattedText = new FormattedText(
          sampleText,
          CultureInfo.CurrentCulture,
          FlowDirection.LeftToRight,
          new Typeface(doc.FontFamily, FontStyles.Normal, fontWeight, FontStretches.Normal),
          doc.FontSize,
          Brushes.Black,
          VisualTreeHelper.GetDpi(this).PixelsPerDip);

        var horizontalPadding = cellPadding.Left + cellPadding.Right;
        const double bordersWidth = 2;

        return Math.Ceiling(formattedText.WidthIncludingTrailingWhitespace + horizontalPadding + bordersWidth);
      }

      double MeasureCharactersWidth(int characterCount, char sampleChar)
      {
        return MeasureColumnWidth(new string(sampleChar, characterCount), FontWeights.Normal);
      }

      var designationColumnWidth = Math.Max(
        MeasureColumnWidth("Обозначение", FontWeights.Bold),
        MeasureCharactersWidth(24, 'A'));

      var nameOkColumnWidth = Math.Max(
        MeasureColumnWidth("Наименование ОК", FontWeights.Bold),
        MeasureCharactersWidth(24, 'A'));

      var opkColumnWidth = Math.Max(
        MeasureColumnWidth("ОПК", FontWeights.Bold),
        MeasureCharactersWidth(24, 'A'));

      var orderColumnWidth = Math.Max(
        MeasureColumnWidth("Заказ", FontWeights.Bold),
        MeasureCharactersWidth(8, '0'));

      var opkFileColumnWidth = Math.Max(
        MeasureColumnWidth("Файл ОПК", FontWeights.Bold),
        MeasureCharactersWidth(16, 'A'));

      var createdColumnWidth = Math.Max(
        MeasureColumnWidth("Создан", FontWeights.Bold),
        MeasureCharactersWidth(10, '0'));

      var departmentColumnWidth = Math.Max(
        MeasureColumnWidth("Цех", FontWeights.Bold),
        MeasureCharactersWidth(4, '0'));

      var commentColumnWidth = MeasureColumnWidth("Примечание", FontWeights.Bold);
      var columnWidths = new[]
      {
        designationColumnWidth,
        nameOkColumnWidth,
        opkColumnWidth,
        orderColumnWidth,
        opkFileColumnWidth,
        createdColumnWidth,
        departmentColumnWidth,
        commentColumnWidth
      };

      var requiredWidth = columnWidths.Sum();
      if (requiredWidth > availableTableWidth && availableTableWidth > 0)
      {
        var scale = availableTableWidth / requiredWidth;
        for (int i = 0; i < columnWidths.Length; i++)
        {
          columnWidths[i] = Math.Floor(columnWidths[i] * scale);
        }
      }

      var fixedColumnsWidth = columnWidths.Take(7).Sum();
      columnWidths[7] = Math.Max(columnWidths[7], Math.Max(0, availableTableWidth - fixedColumnsWidth));

      foreach (var columnWidth in columnWidths)
      {
        table.Columns.Add(new TableColumn { Width = new GridLength(columnWidth) });
      }

      // Заголовки
      var headerRow = new TableRow();
      headerRow.Cells.Add(new TableCell(CreateCell("Обозначение")));
      headerRow.Cells.Add(new TableCell(CreateCell("Наименование ОК")));
      headerRow.Cells.Add(new TableCell(CreateCell("ОПК")));
      headerRow.Cells.Add(new TableCell(CreateCell("Заказ")));
      headerRow.Cells.Add(new TableCell(CreateCell("Файл ОПК")));
      headerRow.Cells.Add(new TableCell(CreateCell("Создан")));
      headerRow.Cells.Add(new TableCell(CreateCell("Цех")));
      headerRow.Cells.Add(new TableCell(CreateCell("Примечание")));

      foreach (var cell in headerRow.Cells)
      {
        cell.BorderBrush = Brushes.Black;
        cell.BorderThickness = new Thickness(0.5);
        cell.Padding = cellPadding;
        cell.FontWeight = FontWeights.Bold;
      }

      var rowGroup = new TableRowGroup();
      rowGroup.Rows.Add(headerRow);

      // Данные
      foreach (var item in entries)
      {
        var row = new TableRow();

        row.Cells.Add(new TableCell(CreateCell(item.Name)));
        row.Cells.Add(new TableCell(CreateCell(item.NameOK)));
        row.Cells.Add(new TableCell(CreateCell(item.OPK)));
        row.Cells.Add(new TableCell(CreateCell(item.Order)));
        row.Cells.Add(new TableCell(CreateCell(item.OpkFileName)));
        row.Cells.Add(new TableCell(CreateCell(item.CreationDate.ToString("dd.MM.yyyy"))));
        row.Cells.Add(new TableCell(CreateCell(item.Department)));
        row.Cells.Add(new TableCell(CreateCell(item.Comment)));

        foreach (var cell in row.Cells)
        {
          cell.BorderBrush = Brushes.Black;
          cell.BorderThickness = new Thickness(0.5);
          cell.Padding = cellPadding;
        }

        rowGroup.Rows.Add(row);
      }

      table.RowGroups.Add(rowGroup);
      doc.Blocks.Add(table);

      return doc;
    }

    Paragraph CreateCell(string text) =>
    new Paragraph(new Run(text ?? ""))
    {
      Margin = new Thickness(0),
      TextAlignment = TextAlignment.Left
    };

    private void ArchivesTreeContextMenu_Opened(object sender, RoutedEventArgs e)
    {
      var node = GetContextNode();
      var isRoot = node?.Kind == ArchiveTreeNodeKind.Root;
      var isArchive = node?.Kind == ArchiveTreeNodeKind.Archive;
      var isFile = node?.Kind == ArchiveTreeNodeKind.File;
      var hasClipboardEntry = HasArchiveClipboardEntry();
      var canManageArchives = isRoot;

      CreateArchiveMenuItem.Visibility = isRoot ? Visibility.Visible : Visibility.Collapsed;
      PrintArchiveCatalogMenuItem.Visibility = isArchive ? Visibility.Visible : Visibility.Collapsed;
      UploadArchiveMenuItem.Visibility = canManageArchives ? Visibility.Visible : Visibility.Collapsed;
      DownloadArchivesMenuItem.Visibility = canManageArchives ? Visibility.Visible : Visibility.Collapsed;
      OpenArchiveMenuItem.Visibility = isArchive ? Visibility.Visible : Visibility.Collapsed;
      SaveArchiveMenuItem.Visibility = isArchive ? Visibility.Visible : Visibility.Collapsed;
      DeleteArchiveMenuItem.Visibility = isArchive ? Visibility.Visible : Visibility.Collapsed;
      AddFileToArchiveMenuItem.Visibility = isArchive ? Visibility.Visible : Visibility.Collapsed;
      OpenArchiveFileMenuItem.Visibility = isFile ? Visibility.Visible : Visibility.Collapsed;
      CopyArchiveFileMenuItem.Visibility = isFile ? Visibility.Visible : Visibility.Collapsed;
      CutArchiveFileMenuItem.Visibility = isFile ? Visibility.Visible : Visibility.Collapsed;
      PasteArchiveFileMenuItem.Visibility = (isFile || isArchive) && hasClipboardEntry ? Visibility.Visible : Visibility.Collapsed;
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

    private void CreateArchiveMenuItem_Click(object sender, RoutedEventArgs e)
    {
      BeginCreateArchiveWorkflow();
    }

    private void UploadArchiveMenuItem_Click(object sender, RoutedEventArgs e)
    {
      ArchiveTransferUiService.UploadArchive();
    }

    private void DownloadArchivesMenuItem_Click(object sender, RoutedEventArgs e)
    {
      ArchiveTransferUiService.DownloadArchives();
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

    private void SaveArchiveMenuItem_Click(object sender, RoutedEventArgs e)
    {
      var node = GetContextNode();
      if (node?.Kind != ArchiveTreeNodeKind.Archive || string.IsNullOrWhiteSpace(node.ArchivePath))
      {
        return;
      }

      _lastSelectedArchivePath = node.ArchivePath;
      _lastSelectedEntryName = null;
      SaveSelectedArchiveToDisk();
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

    private void CopyArchiveFileMenuItem_Click(object sender, RoutedEventArgs e)
    {
      var node = GetContextNode();
      if (node?.Kind != ArchiveTreeNodeKind.File ||
          string.IsNullOrWhiteSpace(node.ArchivePath) ||
          string.IsNullOrWhiteSpace(node.EntryName))
      {
        return;
      }

      StoreArchiveClipboardEntry(node.ArchivePath, node.EntryName, node.DisplayName, ArchiveClipboardOperation.Copy);
    }

    private void CutArchiveFileMenuItem_Click(object sender, RoutedEventArgs e)
    {
      var node = GetContextNode();
      if (node?.Kind != ArchiveTreeNodeKind.File ||
          string.IsNullOrWhiteSpace(node.ArchivePath) ||
          string.IsNullOrWhiteSpace(node.EntryName))
      {
        return;
      }

      StoreArchiveClipboardEntry(node.ArchivePath, node.EntryName, node.DisplayName, ArchiveClipboardOperation.Cut);
    }

    private async void PasteArchiveFileMenuItem_Click(object sender, RoutedEventArgs e)
    {
      var node = GetContextNode();
      if ((node?.Kind != ArchiveTreeNodeKind.File && node?.Kind != ArchiveTreeNodeKind.Archive) ||
          string.IsNullOrWhiteSpace(node.ArchivePath))
      {
        return;
      }

      await PasteArchiveClipboardToAsync(node.ArchivePath);
    }

    private void AddFileToArchiveButton_Click(object sender, RoutedEventArgs e)
    {
      if (!string.IsNullOrWhiteSpace(_lastSelectedArchivePath))
      {
        AddFileToArchive(_lastSelectedArchivePath);
      }
    }

    private void SaveArchiveToDiskButton_Click(object sender, RoutedEventArgs e)
    {
      SaveSelectedArchiveToDisk();
      ResetArchiveActionButtonFocus();
    }

    private void DeleteArchiveButton_Click(object sender, RoutedEventArgs e)
    {
      if (!string.IsNullOrWhiteSpace(_lastSelectedArchivePath))
      {
        DeleteArchive(_lastSelectedArchivePath, Path.GetFileNameWithoutExtension(_lastSelectedArchivePath));
      }
    }

    private async void PasteIntoArchiveButton_Click(object sender, RoutedEventArgs e)
    {
      if (!string.IsNullOrWhiteSpace(_lastSelectedArchivePath))
      {
        await PasteArchiveClipboardToAsync(_lastSelectedArchivePath);
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

    private void CopyArchiveFileButton_Click(object sender, RoutedEventArgs e)
    {
      if (!string.IsNullOrWhiteSpace(_lastSelectedArchivePath) &&
          !string.IsNullOrWhiteSpace(_lastSelectedEntryName))
      {
        StoreArchiveClipboardEntry(
          _lastSelectedArchivePath,
          _lastSelectedEntryName,
          Path.GetFileName(_lastSelectedEntryName),
          ArchiveClipboardOperation.Copy);
      }
    }

    private void CutArchiveFileButton_Click(object sender, RoutedEventArgs e)
    {
      if (!string.IsNullOrWhiteSpace(_lastSelectedArchivePath) &&
          !string.IsNullOrWhiteSpace(_lastSelectedEntryName))
      {
        StoreArchiveClipboardEntry(
          _lastSelectedArchivePath,
          _lastSelectedEntryName,
          Path.GetFileName(_lastSelectedEntryName),
          ArchiveClipboardOperation.Cut);
      }
    }

    private async void PasteArchiveFileButton_Click(object sender, RoutedEventArgs e)
    {
      if (!string.IsNullOrWhiteSpace(_lastSelectedArchivePath))
      {
        await PasteArchiveClipboardToAsync(_lastSelectedArchivePath);
      }
    }
    
    private async void PrintArchiveCatalogButton_Click(object sender, RoutedEventArgs e)
    {
      await PrintArchiveCatalogAsync();
      ResetArchiveActionButtonFocus();
    }

    private void ResetArchiveActionButtonFocus()
    {
      Dispatcher.BeginInvoke(new Action(() =>
      {
        Keyboard.ClearFocus();
      }), DispatcherPriority.Background);
    }

    private async void PrintArchiveCatalogMenuItem_Click(object sender, RoutedEventArgs e)
    {
      await PrintArchiveCatalogAsync();
    }

    private void BeginCreateArchiveWorkflow()
    {
      try
      {
        var createdArchivePath = ShowArchiveCreationDialog();
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

        ShowArchiveNotification(
          "Добавление файла",
          $"Файл '{Path.GetFileName(openFileDialog.FileName)}' успешно добавлен в архив '{Path.GetFileNameWithoutExtension(archivePath)}'.",
          NotificationType.Success);
      }
      catch (Exception ex)
      {
        ShowArchiveNotification("Добавление файла", GetUserFriendlyArchiveErrorMessage(ex), NotificationType.Error);
      }
    }

    private void SaveSelectedArchiveToDisk()
    {
      if (string.IsNullOrWhiteSpace(_lastSelectedArchivePath) || !File.Exists(_lastSelectedArchivePath))
      {
        ShowArchiveNotification("Сохранение архива", "Выберите архив для сохранения на диск.", NotificationType.Warning);
        return;
      }

      var saveFileDialog = new SaveFileDialog
      {
        Title = "Сохранить архив на диск",
        Filter = "Архив ASK (*.apkw)|*.apkw",
        DefaultExt = ".apkw",
        AddExtension = true,
        FileName = Path.GetFileName(_lastSelectedArchivePath),
        OverwritePrompt = true,
      };

      if (saveFileDialog.ShowDialog(Window.GetWindow(this)) != true)
      {
        return;
      }

      try
      {
        var savedArchivePath = ArchiveTransferService.ExportArchive(_lastSelectedArchivePath, saveFileDialog.FileName);
        ShowArchiveNotification(
          "Сохранение архива",
          $"Архив '{Path.GetFileName(savedArchivePath)}' успешно сохранён на диск.",
          NotificationType.Success);
      }
      catch (Exception ex)
      {
        ShowArchiveNotification("Сохранение архива", GetUserFriendlyArchiveErrorMessage(ex), NotificationType.Error);
      }
    }

    private void DeleteArchive(string archivePath, string displayName)
    {
      var confirmation = Message.MessageBoxCustom.Show(
        $"Удалить архив '{displayName}'?",
        "Удаление архива",
        MessageBoxButton.YesNo,
        MessageBoxImage.Question);

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

        ShowArchiveNotification(
          "Удаление архива",
          $"Архив '{displayName}' успешно удалён.",
          NotificationType.Success);
      }
      catch (Exception ex)
      {
        ShowArchiveNotification("Удаление архива", GetUserFriendlyArchiveErrorMessage(ex), NotificationType.Error);
      }
    }

    private void DeleteArchiveFile(string archivePath, string entryName, string displayName)
    {
      var confirmation = Message.MessageBoxCustom.Show(
        $"Удалить файл '{displayName}' из архива?",
        "Удаление файла",
        MessageBoxButton.YesNo,
        MessageBoxImage.Question);

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

        ShowArchiveNotification(
          "Удаление файла",
          $"Файл '{displayName}' успешно удалён из архива '{Path.GetFileNameWithoutExtension(archivePath)}'.",
          NotificationType.Success);
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

    private string? ShowArchiveCreationDialog()
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

      var listLabel = new TextBlock
      {
        Text = "Доступные архивы:",
        Margin = new Thickness(0, 0, 0, 4),
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

      Grid.SetRow(listLabel, 0);
      Grid.SetRow(archivesListBorder, 1);
      Grid.SetRow(buttonsPanel, 2);
      layout.Children.Add(listLabel);
      layout.Children.Add(archivesListBorder);
      layout.Children.Add(buttonsPanel);
      shell.Child = layout;
      dialog.Content = shell;

      void RefreshArchivesList(string? selectedArchivePath = null)
      {
        PopulateArchiveList(archivesListBox, archivesRootPath);

        if (string.IsNullOrWhiteSpace(selectedArchivePath))
        {
          return;
        }

        var selectedItem = archivesListBox.Items
          .OfType<ListBoxItem>()
          .FirstOrDefault(item => string.Equals(item.Tag as string, selectedArchivePath, StringComparison.OrdinalIgnoreCase));

        if (selectedItem != null)
        {
          archivesListBox.SelectedItem = selectedItem;
          archivesListBox.ScrollIntoView(selectedItem);
        }
      }

      createArchiveButton.Click += (_, _) =>
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

            RefreshArchivesList(createdArchivePath);
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
        RefreshArchivesList(_lastSelectedArchivePath);
      };

      return dialog.ShowDialog() == true
        ? dialog.Tag as string
        : null;
    }

    private string PromptForArchiveName(string suggestedArchiveName)
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

      var listLabel = new TextBlock
      {
        Text = "Доступные архивы:",
        Margin = new Thickness(0, 0, 0, 4),
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
        //VerticalContentAlignment = VerticalAlignment.Center,
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

      Grid.SetRow(listLabel, 0);
      Grid.SetRow(archivesListBorder, 1);
      Grid.SetRow(buttonsPanel, 2);
      layout.Children.Add(listLabel);
      layout.Children.Add(archivesListBorder);
      layout.Children.Add(buttonsPanel);
      shell.Child = layout;
      dialog.Content = shell;

      void RefreshArchivesList(string? selectedArchivePath = null)
      {
        PopulateArchiveList(archivesListBox, archivesRootPath);

        if (string.IsNullOrWhiteSpace(selectedArchivePath))
        {
          return;
        }

        var selectedItem = archivesListBox.Items
          .OfType<ListBoxItem>()
          .FirstOrDefault(item => string.Equals(item.Tag as string, selectedArchivePath, StringComparison.OrdinalIgnoreCase));

        if (selectedItem != null)
        {
          archivesListBox.SelectedItem = selectedItem;
          archivesListBox.ScrollIntoView(selectedItem);
        }
      }

      createArchiveButton.Click += (_, _) =>
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

            RefreshArchivesList(createdArchivePath);
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
        RefreshArchivesList(_lastSelectedArchivePath);
      };

      return dialog.ShowDialog() == true
        ? dialog.Tag as string
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
          lock (_archiveManagerSync)
          {
            _archiveManager.CloseArchive();
          }

          _lastSelectedArchivePath = null;
          _lastSelectedEntryName = null;
          ClearFilePanels();
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

    private static string GetUserFriendlyArchiveErrorMessage(Exception ex)
    {
      if (ex is InvalidOperationException invalidOperation &&
          !string.IsNullOrWhiteSpace(invalidOperation.Message))
      {
        return invalidOperation.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase)
          ? "Файл или архив с таким именем уже существует."
          : invalidOperation.Message;
      }

      if (ex is InvalidOperationException invalidOperationWithMessage &&
          !string.IsNullOrWhiteSpace(invalidOperationWithMessage.Message))
      {
        return invalidOperationWithMessage.Message;
      }

      if (ex is FileNotFoundException)
      {
        return "Архив или файл не найден.";
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

      if (ex is IOException ioExceptionWithMessage &&
          !string.IsNullOrWhiteSpace(ioExceptionWithMessage.Message))
      {
        return ioExceptionWithMessage.Message;
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

    private enum ArchiveClipboardOperation
    {
      Copy,
      Cut
    }

    private sealed class ArchiveClipboardEntry
    {
      public ArchiveClipboardEntry(string sourceArchivePath, string entryName, string displayName, ArchiveClipboardOperation operation)
      {
        SourceArchivePath = sourceArchivePath;
        EntryName = entryName;
        DisplayName = displayName;
        Operation = operation;
      }

      public string SourceArchivePath { get; }
      public string EntryName { get; }
      public string DisplayName { get; }
      public ArchiveClipboardOperation Operation { get; }
    }

    private sealed class TreeRefreshState
    {
      public bool IsRootExpanded { get; set; }
      public HashSet<string> ExpandedArchivePaths { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

  }
}
