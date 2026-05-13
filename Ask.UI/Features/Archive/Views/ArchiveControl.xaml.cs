using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Services.FileFormats;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.Engine.ControlCommandAnalyser;
using Ask.UI.Features.Notifications.Models;
using Ask.UI.Infrastructure.UI.Overlay.Notifications.Runtime;
using Ask.UI.Shared.Formatting;
using Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using Ask.UI.Features.Archive.Contracts;
using Ask.UI.Features.Archive.ViewModels;
using Ask.UI.Shared.Components.Progress;
using Ask.UI.Features.Archive.Services;
using static Ask.LogLib.LoggerUtility;
using Button = System.Windows.Controls.Button;
using Orientation = System.Windows.Controls.Orientation;
using Path = System.IO.Path;
using Table = System.Windows.Documents.Table;
using UserControl = System.Windows.Controls.UserControl;

namespace Ask.UI.Features.Archive.Views
{
  /// <summary>
  /// Логика взаимодействия для ArchiveControl.xaml
  /// </summary>
  public partial class ArchiveControl : UserControl, IArchiveViewActions
  {
    private ArchiveViewModel ViewModel => (ArchiveViewModel)DataContext;

    /// <summary>
    /// Кэш архивных записей по строковому ключу (без учёта регистра).
    /// Используется для ускорения доступа и избежания повторной загрузки данных.
    /// </summary>
    private readonly Dictionary<string, IReadOnlyList<ArchiveEntryInfo>> _archiveEntriesCache = new Dictionary<string, IReadOnlyList<ArchiveEntryInfo>>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Настройки сериализации JSON для манифеста (имена свойств без учёта регистра).
    /// </summary>
    private static readonly JsonSerializerOptions ManifestJsonOptions = new JsonSerializerOptions
    {
      PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Путь к директории с архивами.
    /// </summary>
    private readonly string _archivesFolderPath;

    /// <summary>
    /// Путь к директории архивов на проверке.
    /// </summary>
    private readonly string _reviewArchivesFolderPath;

    /// <summary>
    /// Таймер для отложенного автообновления UI.
    /// </summary>
    private readonly DispatcherTimer _autoRefreshTimer;

    /// <summary>
    /// Менеджер для работы с архивами (чтение, изменение, операции с файлами).
    /// </summary>
    private readonly ArchiveManager _archiveManager = new ArchiveManager();

    /// <summary>
    /// Объект синхронизации для потокобезопасной работы с ArchiveManager.
    /// </summary>
    private readonly object _archiveManagerSync = new object();

    /// <summary>
    /// Объект синхронизации для контроля подавления обновлений review-архивов.
    /// </summary>
    private readonly object _reviewRefreshSuppressionSync = new object();

    /// <summary>
    /// Кэш путей review-файлов, недавно изменённых (с временем истечения подавления обновлений).
    /// </summary>
    private readonly Dictionary<string, DateTime> _recentlyMutatedReviewPaths = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Текущий список записей, отображаемых в таблице.
    /// </summary>
    private IReadOnlyList<ArchiveEntryInfo> _currentGridEntries = Array.Empty<ArchiveEntryInfo>();

    /// <summary>
    /// Флаг подавления обработки изменения выбора в таблице.
    /// </summary>
    private bool _suppressGridSelection;

    /// <summary>
    /// Путь к последнему выбранному архиву.
    /// </summary>
    private string _lastSelectedArchivePath;

    /// Имя последнего выбранного файла в архиве.
    /// </summary>
    private string _lastSelectedEntryName;

    /// <summary>
    /// Путь к последнему выбранному review-файлу.
    /// </summary>
    private string _lastSelectedReviewFilePath;

    /// <summary>
    /// Флаг, указывающий, что выбранный файл относится к review.
    /// </summary>
    private bool _lastSelectedIsReviewEntry;

    /// <summary>
    /// Узел, для которого открыт контекстное меню.
    /// </summary>
    private ArchiveTreeNode _contextMenuNode;

    /// <summary>
    /// Буфер обмена для операций с файлами архива.
    /// </summary>
    private ArchiveClipboardEntry? _archiveClipboardEntry;

    /// <summary>
    /// Кэш манифестов архивов (имя файла → дата создания).
    /// </summary>
    private readonly Dictionary<string, Dictionary<string, DateTime>> _manifestCache = new();

    /// <summary>
    /// Инициализирует контрол: подписка на события, настройка путей, таймера автообновления и начального состояния UI.
    /// </summary>
    public ArchiveControl()
    {
      InitializeComponent();
      var viewModel = new ArchiveViewModel();
      viewModel.AttachActions(this);
      DataContext = viewModel;
      EventAggregator.Subscribe<ArchiveEvents.Changed>(OnArchiveChanged);
      _archivesFolderPath = ArchiveDirectoryService.ResolveArchivesRootPath();
      _reviewArchivesFolderPath = ArchiveDirectoryService.ResolveReviewArchivesRootPath();

      _autoRefreshTimer = new DispatcherTimer
      {
        Interval = TimeSpan.FromMilliseconds(350),
      };

      _autoRefreshTimer.Tick += AutoRefreshTimer_Tick;


      UpdateRightPanels(isFilesVisible: false, isEditorVisible: false);
      ResetTree();
    }

    /// <summary>
    /// Запускает процесс создания нового архива.
    /// </summary>
    public void CreateArchive() => BeginCreateArchiveWorkflow();

    /// <summary>
    /// Выполняет загрузку архива в приложение.
    /// </summary>
    public void UploadArchive() => ArchiveTransferUiService.UploadArchive();

    /// <summary>
    /// Выполняет скачивание архивов на диск.
    /// </summary>
    public void DownloadArchives() => ArchiveTransferUiService.DownloadArchives();

    /// <summary>
    /// Открывает архив, выбранный в контекстном меню.
    /// </summary>
    /// <returns>Асинхронная задача открытия архива.</returns>
    public async Task OpenContextArchiveAsync()
    {
      var node = GetContextNode();
      if (node == null || string.IsNullOrWhiteSpace(node.ArchivePath))
      {
        return;
      }

      if (node.Kind == ArchiveTreeNodeKind.Archive || node.Kind == ArchiveTreeNodeKind.ReviewArchive)
      {
        await OpenArchiveAsync(node.ArchivePath);
      }
    }

    /// <summary>
    /// Сохраняет архив, выбранный в контекстном меню, на диск.
    /// </summary>
    public void SaveContextArchive()
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

    /// <summary>
    /// Выполняет печать каталога архива, выбранного в контекстном меню.
    /// </summary>
    /// <returns>Асинхронная задача печати каталога архива.</returns>
    public Task PrintContextArchiveCatalogAsync() => PrintArchiveCatalogAsync();

    /// <summary>
    /// Добавляет файл в архив, выбранный в контекстном меню.
    /// </summary>
    public void AddFileToContextArchive()
    {
      var node = GetContextNode();
      if (node?.Kind != ArchiveTreeNodeKind.Archive || string.IsNullOrWhiteSpace(node.ArchivePath))
      {
        return;
      }

      AddFileToArchive(node.ArchivePath);
    }

    /// <summary>
    /// Удаляет архив, выбранный в контекстном меню.
    /// </summary>
    /// <returns>Асинхронная задача удаления архива.</returns>
    public async Task DeleteContextArchiveAsync()
    {
      var node = GetContextNode();
      if (node == null || string.IsNullOrWhiteSpace(node.ArchivePath))
      {
        return;
      }

      if (node.Kind == ArchiveTreeNodeKind.Archive)
      {
        DeleteArchive(node.ArchivePath, Path.GetFileNameWithoutExtension(node.ArchivePath));
        return;
      }

      if (node.Kind == ArchiveTreeNodeKind.ReviewArchive)
      {
        await DeleteReviewArchiveAsync(node.ArchivePath, Path.GetFileName(node.ArchivePath));
      }
    }

    /// <summary>
    /// Открывает файл или архив, выбранный в контекстном меню.
    /// </summary>
    /// <returns>Асинхронная задача открытия файла или архива.</returns>
    public async Task OpenContextFileAsync()
    {
      var node = GetContextNode();
      if (node == null || string.IsNullOrWhiteSpace(node.ArchivePath))
      {
        return;
      }

      if ((node.Kind == ArchiveTreeNodeKind.File || node.Kind == ArchiveTreeNodeKind.ReviewFile) && !string.IsNullOrWhiteSpace(node.EntryName))
      {
        await OpenArchiveFileAsync(node.ArchivePath, node.EntryName);
        return;
      }

      if (node.Kind == ArchiveTreeNodeKind.ReviewArchive)
      {
        await OpenArchiveAsync(node.ArchivePath);
      }
    }

    /// <summary>
    /// Открывает файл проверки в редакторе из контекстного меню.
    /// </summary>
    public void OpenContextFileInEditor()
    {
      var node = GetContextNode();
      if (node?.Kind != ArchiveTreeNodeKind.ReviewFile || string.IsNullOrWhiteSpace(node.FilePath))
      {
        return;
      }

      FileInteractionEventAdapter.RaiseOpenFileInEditorAgain(node.FilePath);
    }

    /// <summary>
    /// Копирует файл архива в буфер обмена архивов.
    /// </summary>
    public void CopyContextFile()
    {
      var node = GetContextNode();
      if (node?.Kind != ArchiveTreeNodeKind.File || string.IsNullOrWhiteSpace(node.ArchivePath) || string.IsNullOrWhiteSpace(node.EntryName))
      {
        return;
      }

      StoreArchiveClipboardEntry(node.ArchivePath, node.EntryName, node.DisplayName, ArchiveClipboardOperation.Copy);
    }

    /// <summary>
    /// Вырезает файл архива в буфер обмена архивов.
    /// </summary>
    public void CutContextFile()
    {
      var node = GetContextNode();
      if (node?.Kind != ArchiveTreeNodeKind.File || string.IsNullOrWhiteSpace(node.ArchivePath) || string.IsNullOrWhiteSpace(node.EntryName))
      {
        return;
      }

      StoreArchiveClipboardEntry(node.ArchivePath, node.EntryName, node.DisplayName, ArchiveClipboardOperation.Cut);
    }

    /// <summary>
    /// Вставляет файл из буфера обмена архивов в выбранный архив.
    /// </summary>
    /// <returns>Асинхронная задача вставки файла.</returns>
    public async Task PasteContextFileAsync()
    {
      var node = GetContextNode();
      if ((node?.Kind != ArchiveTreeNodeKind.File && node?.Kind != ArchiveTreeNodeKind.Archive) || string.IsNullOrWhiteSpace(node.ArchivePath))
      {
        return;
      }

      await PasteArchiveClipboardToAsync(node.ArchivePath);
    }

    /// <summary>
    /// Удаляет файл, выбранный в контекстном меню.
    /// </summary>
    /// <returns>Асинхронная задача удаления файла.</returns>
    public async Task DeleteContextFileAsync()
    {
      var node = GetContextNode();
      if (node == null || string.IsNullOrWhiteSpace(node.ArchivePath) || string.IsNullOrWhiteSpace(node.EntryName))
      {
        return;
      }

      if (node.Kind == ArchiveTreeNodeKind.File)
      {
        DeleteArchiveFile(node.ArchivePath, node.EntryName, node.DisplayName);
        return;
      }

      if (node.Kind == ArchiveTreeNodeKind.ReviewFile)
      {
        await DeleteReviewFileAsync(node.ArchivePath, node.EntryName, node.DisplayName, node.FilePath);
      }
    }

    /// <summary>
    /// Добавляет файл в текущий выбранный архив.
    /// </summary>
    public void AddFileToSelectedArchive()
    {
      if (!string.IsNullOrWhiteSpace(_lastSelectedArchivePath))
      {
        AddFileToArchive(_lastSelectedArchivePath);
      }
    }

    /// <summary>
    /// Обрабатывает событие изменения архива: проверяет входные данные,
    /// при необходимости переключает выполнение в UI-поток и запускает обработку.
    /// </summary>
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

    /// <summary>
    /// Обрабатывает изменение архива: инвалидирует кэш и обновляет дерево с учётом текущего состояния.
    /// </summary>
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

    /// <summary>
    /// Возвращает кэш манифеста архива (имя файла → дата создания),
    /// при отсутствии — считывает и парсит его из архива.
    /// </summary>
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

    /// <summary>
    /// Обрабатывает изменения файловой системы: фильтрует служебные и недавние изменения,
    /// при необходимости запускает отложенное обновление UI.
    /// </summary>
    private void OnArchivesWatcherChanged(object sender, FileSystemEventArgs e)
    {
      if (IsUnderReviewWorkspace(e.FullPath))
      {
        return;
      }

      if (WasRecentlyMutatedReviewPath(e.FullPath))
      {
        return;
      }

      if (e.ChangeType == WatcherChangeTypes.Changed &&
          ArchiveEncryptionSession.WasRecentlyMutatedBySession(e.FullPath))
      {
        return;
      }

      ScheduleAutoRefresh();
    }

    /// <summary>
    /// Обрабатывает переименование в файловой системе: игнорирует служебные и недавние изменения,
    /// при необходимости инициирует обновление UI.
    /// </summary>
    private void OnArchivesWatcherRenamed(object sender, RenamedEventArgs e)
    {
      if (IsUnderReviewWorkspace(e.OldFullPath) || IsUnderReviewWorkspace(e.FullPath))
      {
        return;
      }

      if (WasRecentlyMutatedReviewPath(e.OldFullPath) || WasRecentlyMutatedReviewPath(e.FullPath))
      {
        return;
      }

      ScheduleAutoRefresh();
    }

    /// <summary>
    /// Помечает путь как недавно изменённый для временного подавления автообновления.
    /// </summary>
    private void MarkReviewPathAsRecentlyMutated(string? path)
    {
      if (string.IsNullOrWhiteSpace(path))
      {
        return;
      }

      var fullPath = Path.GetFullPath(path);
      lock (_reviewRefreshSuppressionSync)
      {
        _recentlyMutatedReviewPaths[fullPath] = DateTime.UtcNow.AddSeconds(3);
      }
    }

    /// <summary>
    /// Проверяет, был ли путь недавно изменён (с учётом очистки устаревших записей).
    /// </summary>
    private bool WasRecentlyMutatedReviewPath(string? path)
    {
      if (string.IsNullOrWhiteSpace(path))
      {
        return false;
      }

      var fullPath = Path.GetFullPath(path);
      var now = DateTime.UtcNow;

      lock (_reviewRefreshSuppressionSync)
      {
        if (_recentlyMutatedReviewPaths.Count > 0)
        {
          var expiredPaths = _recentlyMutatedReviewPaths
            .Where(pair => pair.Value <= now)
            .Select(pair => pair.Key)
            .ToList();

          foreach (var expiredPath in expiredPaths)
          {
            _recentlyMutatedReviewPaths.Remove(expiredPath);
          }
        }

        return _recentlyMutatedReviewPaths.TryGetValue(fullPath, out var expiresAt) && expiresAt > now;
      }
    }

    /// <summary>
    /// Проверяет, находится ли путь внутри директории архивов на проверке.
    /// </summary>
    private bool IsUnderReviewWorkspace(string? path)
    {
      if (string.IsNullOrWhiteSpace(path))
      {
        return false;
      }

      var fullPath = Path.GetFullPath(path);
      return fullPath.StartsWith(_reviewArchivesFolderPath, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Планирует отложенное обновление UI через перезапуск таймера в UI-потоке.
    /// </summary>
    private void ScheduleAutoRefresh()
    {
      Dispatcher.BeginInvoke(new Action(() =>
      {
        _autoRefreshTimer.Stop();
        _autoRefreshTimer.Start();
      }));
    }

    /// <summary>
    /// Обрабатывает срабатывание таймера: очищает кэши и обновляет дерево с сохранением состояния.
    /// </summary>
    private async void AutoRefreshTimer_Tick(object sender, EventArgs e)
    {
      _autoRefreshTimer.Stop();
      _archiveEntriesCache.Clear();
      _manifestCache.Clear();
      await RefreshTreePreservingStateAsync(preservePanels: true);
    }

    /// <summary>
    /// Сохраняет текущее состояние дерева (раскрытые узлы и выбранные архивы).
    /// </summary>
    private TreeRefreshState CaptureTreeRefreshState()
    {
      var state = new TreeRefreshState();
      foreach (var rootNode in GetRootNodes())
      {
        switch (rootNode.Kind)
        {
          case ArchiveTreeNodeKind.Root:
            state.IsArchiveRootExpanded = rootNode.IsExpanded;
            foreach (var archiveNode in GetExpandedNodes(rootNode, ArchiveTreeNodeKind.Archive))
            {
              state.ExpandedArchivePaths.Add(Path.GetFullPath(archiveNode.ArchivePath));
            }
            break;

          case ArchiveTreeNodeKind.ReviewRoot:
            state.IsReviewRootExpanded = rootNode.IsExpanded;
            foreach (var reviewNode in GetExpandedNodes(rootNode, ArchiveTreeNodeKind.ReviewArchive))
            {
              state.ExpandedReviewArchivePaths.Add(Path.GetFullPath(reviewNode.ArchivePath));
            }
            break;
        }
      }

      return state;
    }

    /// <summary>
    /// Возвращает корневые узлы дерева архивов.
    /// </summary>
    private IReadOnlyList<ArchiveTreeNode> GetRootNodes()
    {
      return ViewModel.TreeNodes.ToList();
    }


    /// <summary>
    /// Возвращает корневой узел дерева по указанному типу.
    /// </summary>
    private ArchiveTreeNode? GetRootNode(ArchiveTreeNodeKind rootKind)
    {
      return GetRootNodes().FirstOrDefault(node => node.Kind == rootKind);
    }

    /// <summary>
    /// Обновляет дерево архивов с восстановлением состояния и опциональным сохранением правых панелей.
    /// </summary>
    private async Task RefreshTreePreservingStateAsync(bool preservePanels)
    {
      var state = CaptureTreeRefreshState();

      var archiveRootNode = ArchiveTreeNode.CreateRoot("Архивы");
      archiveRootNode.IsExpanded = state.IsArchiveRootExpanded;
      archiveRootNode.Children.Add(ArchiveTreeNode.CreatePlaceholder("Загрузка..."));

      var reviewRootNode = ArchiveTreeNode.CreateReviewRoot("Архивы на проверке");
      reviewRootNode.IsExpanded = state.IsReviewRootExpanded;
      reviewRootNode.Children.Add(ArchiveTreeNode.CreatePlaceholder("Загрузка..."));

      ViewModel.SetTreeRoots(new[] { archiveRootNode, reviewRootNode });

      if (state.IsArchiveRootExpanded || state.ExpandedArchivePaths.Count > 0)
      {
        await LoadArchivesIntoRootAsync(archiveRootNode, state);
      }

      if (state.IsReviewRootExpanded || state.ExpandedReviewArchivePaths.Count > 0)
      {
        await LoadReviewArchivesIntoRootAsync(reviewRootNode, state);
      }

      if (preservePanels)
      {
        await RestoreRightPanelsAfterRefreshAsync();
        return;
      }

      ClearFilePanels();
    }

    /// <summary>
    /// Восстанавливает состояние правых панелей (список файлов и редактор) после обновления дерева.
    /// </summary>
    private async Task RestoreRightPanelsAfterRefreshAsync()
    {
      if (string.IsNullOrWhiteSpace(_lastSelectedArchivePath))
      {
        return;
      }

      if (IsReviewArchivePath(_lastSelectedArchivePath))
      {
        if (!Directory.Exists(_lastSelectedArchivePath))
        {
          return;
        }

        if (!string.IsNullOrWhiteSpace(_lastSelectedEntryName))
        {
          var entries = await GetReviewEntriesAsync(_lastSelectedArchivePath);
          var hasFile = entries.Any(item =>
            string.Equals(item.EntryName, NormalizeEntryName(_lastSelectedEntryName), StringComparison.OrdinalIgnoreCase));

          if (hasFile)
          {
            await ShowReviewFileAsync(_lastSelectedArchivePath, _lastSelectedEntryName, false);
            return;
          }
        }

        await ShowReviewArchiveInGridAsync(_lastSelectedArchivePath, clearEditor: true);
        return;
      }

      if (!File.Exists(_lastSelectedArchivePath))
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

    /// <summary>
    /// Инвалидирует кэши архива (записи и манифест) по указанному пути.
    /// </summary>
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

    /// <summary>
    /// Проверяет равенство путей архивов с учётом нормализации и регистра.
    /// </summary>
    private static bool IsSameArchivePath(string firstPath, string secondPath)
    {
      if (string.IsNullOrWhiteSpace(firstPath) || string.IsNullOrWhiteSpace(secondPath))
      {
        return false;
      }

      return string.Equals(Path.GetFullPath(firstPath), Path.GetFullPath(secondPath), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Сбрасывает дерево архивов в начальное состояние и очищает панели файлов.
    /// </summary>
    private void ResetTree()
    {
      var archiveRootNode = ArchiveTreeNode.CreateRoot("Архивы");
      archiveRootNode.Children.Add(ArchiveTreeNode.CreatePlaceholder("Загрузка..."));

      var reviewRootNode = ArchiveTreeNode.CreateReviewRoot("Архивы на проверке");
      reviewRootNode.Children.Add(ArchiveTreeNode.CreatePlaceholder("Загрузка..."));

      ViewModel.SetTreeRoots(new[] { archiveRootNode, reviewRootNode });
      ClearFilePanels();
    }

    /// <summary>
    /// Очищает панели файлов и редактора, сбрасывая состояние выбора.
    /// </summary>
    private void ClearFilePanels()
    {
      ApplyGridItemsSource(Array.Empty<ArchiveEntryInfo>());
      ViewModel.SelectedFileEntry = null;
      ViewModel.ClearPreview();
      ViewModel.SetFilesHint("Выберите архив для просмотра файлов.");
      ViewModel.SetEditorHint("Выберите файл в архиве для просмотра.");

      _lastSelectedReviewFilePath = null;
      _lastSelectedIsReviewEntry = false;
      UpdateActionButtons();
      UpdateRightPanels(isFilesVisible: false, isEditorVisible: false);
    }


    /// <summary>
    /// Загружает список архивов в корневой узел дерева с учётом состояния раскрытия.
    /// </summary>
    private async Task LoadArchivesIntoRootAsync(ArchiveTreeNode rootNode, TreeRefreshState? state = null)
    {
      if (!HasPlaceholder(rootNode))
      {
        return;
      }

      var archivePaths = await Task.Run(() => ArchiveDirectoryService.GetArchivesInDirectory(_archivesFolderPath));
      if (archivePaths.Count == 0)
      {
        rootNode.Children.ReplaceRange([ArchiveTreeNode.CreatePlaceholder("Архивы не найдены.")]);
        return;
      }

      var archiveNodes = new List<ArchiveTreeNode>();
      foreach (var archivePath in archivePaths)
      {
        var fullArchivePath = Path.GetFullPath(archivePath);
        var isExpanded = state?.ExpandedArchivePaths.Contains(fullArchivePath) == true;

        var archiveNode = ArchiveTreeNode.CreateArchive(Path.GetFileName(archivePath), archivePath);
        archiveNode.IsExpanded = isExpanded;
        archiveNode.Children.Add(ArchiveTreeNode.CreatePlaceholder("Разверните для загрузки файлов..."));

        if (isExpanded)
        {
          await LoadArchiveFilesIntoTreeAsync(archiveNode);
        }

        archiveNodes.Add(archiveNode);
      }

      rootNode.Children.ReplaceRange(archiveNodes);
    }

    /// <summary>
    /// Возвращает раскрытые дочерние узлы указанного типа.
    /// </summary>
    /// <param name="rootNode">Корневой узел дерева.</param>
    /// <param name="expectedKind">Тип узлов для выборки.</param>
    /// <returns>Коллекция раскрытых узлов.</returns>
    private static IEnumerable<ArchiveTreeNode> GetExpandedNodes(ArchiveTreeNode rootNode, ArchiveTreeNodeKind expectedKind)
    {
      foreach (var node in rootNode.Children.Where(node =>
                 node.Kind == expectedKind &&
                 node.IsExpanded &&
                 !string.IsNullOrWhiteSpace(node.ArchivePath)))
      {
        yield return node;
      }
    }

    /// <summary>
    /// Загружает список архивов на проверке в корневой узел с учётом состояния и расчётом статусов.
    /// </summary>
    /// <param name="rootNode">Корневой узел дерева.</param>
    /// <param name="state">Сохранённое состояние дерева (опционально).</param>
    /// <returns>Асинхронная задача загрузки.</returns>
    private async Task LoadReviewArchivesIntoRootAsync(ArchiveTreeNode rootNode, TreeRefreshState? state = null)
    {
      if (!HasPlaceholder(rootNode))
      {
        return;
      }

      var reviewDirectories = await Task.Run(() => ArchiveDirectoryService.GetReviewDirectories(_reviewArchivesFolderPath));
      if (reviewDirectories.Count == 0)
      {
        rootNode.Children.ReplaceRange([ArchiveTreeNode.CreatePlaceholder("Архивы на проверке не найдены.")]);
        return;
      }

      var reviewNodes = new List<ArchiveTreeNode>();
      foreach (var reviewDirectory in reviewDirectories)
      {
        var fullReviewDirectory = Path.GetFullPath(reviewDirectory);
        var isExpanded = state?.ExpandedReviewArchivePaths.Contains(fullReviewDirectory) == true;
        var entries = await GetReviewEntriesAsync(fullReviewDirectory);
        var totalErrors = entries.Sum(entry => entry.ErrorCount);
        var status = entries.Count == 0
          ? ArchiveNodeStatus.None
          : totalErrors > 0 ? ArchiveNodeStatus.Error : ArchiveNodeStatus.Success;

        var reviewNode = ArchiveTreeNode.CreateReviewArchive(Path.GetFileName(reviewDirectory), fullReviewDirectory, status, totalErrors);
        reviewNode.IsExpanded = isExpanded;
        reviewNode.Children.Add(ArchiveTreeNode.CreatePlaceholder("Разверните для загрузки файлов..."));

        if (isExpanded)
        {
          await LoadReviewFilesIntoTreeAsync(reviewNode);
        }

        reviewNodes.Add(reviewNode);
      }

      rootNode.Children.ReplaceRange(reviewNodes);
    }

    /// <summary>
    /// Загружает файлы архива в узел дерева и обновляет их статус.
    /// </summary>
    /// <param name="archiveNode">Узел архива.</param>
    /// <returns>Асинхронная задача загрузки.</returns>
    private async Task LoadArchiveFilesIntoTreeAsync(ArchiveTreeNode archiveNode)
    {
      if (archiveNode.ArchivePath == null || !HasPlaceholder(archiveNode))
      {
        return;
      }

      try
      {
        var entries = await GetArchiveEntriesAsync(archiveNode.ArchivePath);
        UpdateArchiveNodeState(archiveNode, entries);
        if (entries.Count == 0)
        {
          archiveNode.Children.ReplaceRange([ArchiveTreeNode.CreatePlaceholder("Архив пуст.")]);
          return;
        }

        archiveNode.Children.ReplaceRange(entries.Select(entry =>
        {
          var status = entry.ErrorCount > 0 ? ArchiveNodeStatus.Error : ArchiveNodeStatus.Success;
          return ArchiveTreeNode.CreateFile(
            entry.EntryName,
            archiveNode.ArchivePath,
            entry.EntryName,
            status,
            entry.ErrorCount);
        }));
      }
      catch (Exception ex)
      {
        archiveNode.Children.ReplaceRange([ArchiveTreeNode.CreatePlaceholder("Ошибка чтения архива.")]);
        ShowArchiveNotification("Архивы", GetUserFriendlyArchiveErrorMessage(ex), NotificationType.Error);
      }
    }

    /// <summary>
    /// Загружает файлы архива на проверке в узел дерева и обновляет их статус.
    /// </summary>
    /// <param name="reviewNode">Узел архива на проверке.</param>
    /// <returns>Асинхронная задача загрузки.</returns>
    private async Task LoadReviewFilesIntoTreeAsync(ArchiveTreeNode reviewNode)
    {
      if (reviewNode.ArchivePath == null || !HasPlaceholder(reviewNode))
      {
        return;
      }

      try
      {
        var entries = await GetReviewEntriesAsync(reviewNode.ArchivePath);
        UpdateArchiveNodeState(reviewNode, entries);
        if (entries.Count == 0)
        {
          reviewNode.Children.ReplaceRange([ArchiveTreeNode.CreatePlaceholder("Файлы на проверке не найдены.")]);
          return;
        }

        reviewNode.Children.ReplaceRange(entries.Select(entry =>
        {
          var status = entry.ErrorCount > 0 ? ArchiveNodeStatus.Error : ArchiveNodeStatus.Success;
          return ArchiveTreeNode.CreateReviewFile(
            entry.EntryName,
            reviewNode.ArchivePath,
            entry.EntryName,
            entry.SourceFilePath ?? string.Empty,
            status,
            entry.ErrorCount);
        }));
      }
      catch (Exception ex)
      {
        reviewNode.Children.ReplaceRange([ArchiveTreeNode.CreatePlaceholder("Ошибка чтения файлов.")]);
        ShowArchiveNotification("Архивы на проверке", GetUserFriendlyArchiveErrorMessage(ex), NotificationType.Error);
      }
    }

    /// <summary>
    /// Ищет узел архива на проверке по пути.
    /// </summary>
    /// <param name="reviewArchivePath">Путь к архиву на проверке.</param>
    /// <returns>Найденный узел или null.</returns>
    private ArchiveTreeNode? FindReviewArchiveNode(string reviewArchivePath)
    {
      var reviewRootNode = GetRootNode(ArchiveTreeNodeKind.ReviewRoot);
      if (reviewRootNode == null)
      {
        return null;
      }

      return reviewRootNode.Children.FirstOrDefault(node =>
        node.Kind == ArchiveTreeNodeKind.ReviewArchive &&
        IsSameArchivePath(node.ArchivePath, reviewArchivePath));
    }

    /// <summary>
    /// Ищет узел архива по пути.
    /// </summary>
    /// <param name="archivePath">Путь к архиву.</param>
    /// <returns>Найденный узел или null.</returns>
    private ArchiveTreeNode? FindArchiveNode(string archivePath)
    {
      var archiveRootNode = GetRootNode(ArchiveTreeNodeKind.Root);
      if (archiveRootNode == null)
      {
        return null;
      }

      return archiveRootNode.Children.FirstOrDefault(node =>
        node.Kind == ArchiveTreeNodeKind.Archive &&
        IsSameArchivePath(node.ArchivePath, archivePath));
    }

    /// <summary>
    /// Обновляет статус узла архива на основе данных записей.
    /// </summary>
    /// <param name="node">Узел архива.</param>
    /// <param name="entries">Коллекция записей архива.</param>
    private static void UpdateArchiveNodeState(ArchiveTreeNode node, IReadOnlyCollection<ArchiveEntryInfo> entries)
    {
      var totalErrors = entries.Sum(entry => entry.ErrorCount);
      var status = entries.Count == 0
        ? ArchiveNodeStatus.None
        : totalErrors > 0 ? ArchiveNodeStatus.Error : ArchiveNodeStatus.Success;

      node.UpdateReviewState(status, totalErrors);
    }

    /// <summary>
    /// Обновляет состояние узла архива и его файлов в дереве.
    /// </summary>
    /// <param name="archivePath">Путь к архиву.</param>
    /// <param name="entries">Список записей архива.</param>
    private void UpdateArchiveTreeState(string archivePath, IReadOnlyList<ArchiveEntryInfo> entries)
    {
      var archiveNode = IsReviewArchivePath(archivePath)
        ? FindReviewArchiveNode(archivePath)
        : FindArchiveNode(archivePath);

      if (archiveNode == null)
      {
        return;
      }

      UpdateArchiveNodeState(archiveNode, entries);

      if (HasPlaceholder(archiveNode))
      {
        return;
      }

      foreach (var entry in entries)
      {
        var fileNode = archiveNode.Children.FirstOrDefault(node =>
          (node.Kind == ArchiveTreeNodeKind.File || node.Kind == ArchiveTreeNodeKind.ReviewFile) &&
          string.Equals(node.EntryName, entry.EntryName, StringComparison.OrdinalIgnoreCase));

        fileNode?.UpdateReviewState(
          entry.ErrorCount > 0 ? ArchiveNodeStatus.Error : ArchiveNodeStatus.Success,
          entry.ErrorCount);
      }
    }

    /// <summary>
    /// Ищет узел файла в архиве на проверке.
    /// </summary>
    /// <param name="reviewArchivePath">Путь к архиву на проверке.</param>
    /// <param name="entryName">Имя файла.</param>
    /// <returns>Найденный узел или null.</returns>
    private ArchiveTreeNode? FindReviewFileNode(string reviewArchivePath, string entryName)
    {
      var reviewArchiveNode = FindReviewArchiveNode(reviewArchivePath);
      if (reviewArchiveNode == null)
      {
        return null;
      }

      var normalizedEntryName = NormalizeEntryName(entryName);
      return reviewArchiveNode.Children.FirstOrDefault(node =>
        node.Kind == ArchiveTreeNodeKind.ReviewFile &&
        string.Equals(node.EntryName, normalizedEntryName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Ищет запись файла в текущем списке таблицы для архива на проверке.
    /// </summary>
    /// <param name="reviewArchivePath">Путь к архиву на проверке.</param>
    /// <param name="entryName">Имя файла.</param>
    /// <returns>Найденная запись или null.</returns>
    private ArchiveEntryInfo? FindReviewGridEntry(string reviewArchivePath, string entryName)
    {
      var normalizedEntryName = NormalizeEntryName(entryName);
      return _currentGridEntries.FirstOrDefault(entry =>
        entry.IsReviewEntry &&
        IsSameArchivePath(entry.ArchivePath, reviewArchivePath) &&
        string.Equals(entry.EntryName, normalizedEntryName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Обновляет состояние файла в дереве на месте.
    /// </summary>
    /// <param name="reviewArchivePath">Путь к архиву на проверке.</param>
    /// <param name="entryName">Имя файла.</param>
    /// <param name="errorCount">Количество ошибок.</param>
    private void UpdateReviewStateInPlace(string reviewArchivePath, string entryName, int errorCount)
    {
      var normalizedReviewArchivePath = Path.GetFullPath(reviewArchivePath);
      var normalizedEntryName = NormalizeEntryName(entryName);

      var reviewEntry = FindReviewGridEntry(normalizedReviewArchivePath, normalizedEntryName);
      reviewEntry?.UpdateReviewState(errorCount);

      if (_archiveEntriesCache.TryGetValue(normalizedReviewArchivePath, out var cachedEntries))
      {
        var cachedEntry = cachedEntries.FirstOrDefault(entry =>
          entry.IsReviewEntry &&
          string.Equals(entry.EntryName, normalizedEntryName, StringComparison.OrdinalIgnoreCase));
        cachedEntry?.UpdateReviewState(errorCount);
      }

      var reviewFileNode = FindReviewFileNode(normalizedReviewArchivePath, normalizedEntryName);
      reviewFileNode?.UpdateReviewState(errorCount > 0 ? ArchiveNodeStatus.Error : ArchiveNodeStatus.Success, errorCount);

      var reviewArchiveNode = FindReviewArchiveNode(normalizedReviewArchivePath);
      if (reviewArchiveNode != null)
      {
        var reviewEntries = _currentGridEntries
          .Where(entry => entry.IsReviewEntry && IsSameArchivePath(entry.ArchivePath, normalizedReviewArchivePath))
          .ToList();

        var totalErrors = reviewEntries.Sum(entry => entry.ErrorCount);
        var status = reviewEntries.Count == 0
          ? ArchiveNodeStatus.None
          : totalErrors > 0 ? ArchiveNodeStatus.Error : ArchiveNodeStatus.Success;

        reviewArchiveNode.UpdateReviewState(status, totalErrors);
      }
    }

    /// <summary>
    /// Возвращает записи архива с использованием кэша или считывает их при отсутствии.
    /// </summary>
    /// <param name="archivePath">Путь к архиву.</param>
    /// <returns>Список записей архива.</returns>
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

    /// <summary>
    /// Возвращает записи архива на проверке с использованием кэша или считывает их при отсутствии.
    /// </summary>
    /// <param name="reviewDirectoryPath">Путь к директории с архивами на проверке.</param>
    /// <returns>Список записей архива на проверке.</returns>
    private async Task<IReadOnlyList<ArchiveEntryInfo>> GetReviewEntriesAsync(string reviewDirectoryPath)
    {
      if (_archiveEntriesCache.TryGetValue(reviewDirectoryPath, out var cached))
      {
        return cached;
      }

      var entries = await Task.Run(() => ReadReviewEntries(reviewDirectoryPath));
      _archiveEntriesCache[reviewDirectoryPath] = entries;
      return entries;
    }

    /// <summary>
    /// Открывает архив через менеджер и возвращает уведомления о целостности.
    /// </summary>
    /// <param name="archivePath">Путь к архиву.</param>
    /// <returns>Список уведомлений о целостности.</returns>
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

    /// <summary>
    /// Читает текст записи архива с использованием менеджера.
    /// </summary>
    /// <param name="archivePath">Путь к архиву.</param>
    /// <param name="entryName">Имя записи.</param>
    /// <returns>Текст записи.</returns>
    private string ReadArchiveEntryTextWithManager(string archivePath, string entryName)
    {
      lock (_archiveManagerSync)
      {
        EnsureArchiveOpenedInManagerCore(archivePath);
        try
        {
          return CommandTranslationManager.NormalizeCommandMnemonics(
            TextSanitizer.RemoveLegacyControlChars(_archiveManager.GetFileText(entryName)));
        }
        finally
        {
          _archiveManager.CloseArchive();
        }
      }
    }

    /// <summary>
    /// Обеспечивает открытие архива в менеджере.
    /// </summary>
    /// <param name="archivePath">Путь к архиву.</param>
    private void EnsureArchiveOpenedInManagerCore(string archivePath)
    {
      var fullArchivePath = Path.GetFullPath(archivePath);
      if (!string.Equals(_archiveManager.OpenedArchivePath, fullArchivePath, StringComparison.OrdinalIgnoreCase))
      {
        _archiveManager.OpenArchive(fullArchivePath);
      }
    }

    /// <summary>
    /// Читает записи архива.
    /// </summary>
    /// <param name="archivePath">Путь к архиву.</param>
    /// <returns>Список записей архива.</returns>
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

          ArchiveEntryInfo? info = GetArchiveEntryInfoAsync(archivePath, entry).GetAwaiter().GetResult();
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

    /// <summary>
    /// Читает записи архива на проверке.
    /// </summary>
    /// <param name="reviewDirectoryPath">Путь к директории с архивами на проверке.</param>
    /// <returns>Список записей архива на проверке.</returns>
    private IReadOnlyList<ArchiveEntryInfo> ReadReviewEntries(string reviewDirectoryPath)
    {
      var directoryPath = Path.GetFullPath(reviewDirectoryPath);
      if (!Directory.Exists(directoryPath))
      {
        return [];
      }

      var items = new List<ArchiveEntryInfo>();
      foreach (var filePath in Directory.EnumerateFiles(directoryPath, "*.*", SearchOption.TopDirectoryOnly)
                 .Where(IsSupportedReviewFilePath)
                 .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
      {
        var fileType = DeterminePreviewFileType(filePath);
        var text = ReadReviewFileText(filePath, fileType);
        if (string.IsNullOrWhiteSpace(text))
        {
          continue;
        }

        var entryInfo = BuildEntryInfoFromText(
          archivePath: directoryPath,
          entryName: Path.GetFileName(filePath),
          opkFileName: Path.GetFileName(filePath),
          text: text,
          creationDate: File.GetLastWriteTime(filePath),
          sourceFilePath: filePath,
          isReviewEntry: true,
          errorCount: ExtractErrorCount(text),
          fileType: fileType);

        if (entryInfo != null)
        {
          items.Add(entryInfo);
        }
      }

      return items
        .OrderByDescending(item => item.ErrorCount)
        .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
        .ToList();
    }

    /// <summary>
    /// Формирует информацию о записи архива (с учётом манифеста и содержимого файла).
    /// </summary>
    /// <param name="archivePath">Путь к архиву.</param>
    /// <param name="entry">Запись архива.</param>
    /// <returns>Информация о записи или null.</returns>
    private async Task<ArchiveEntryInfo?> GetArchiveEntryInfoAsync(string archivePath, ZipArchiveEntry entry)
    {
      var manifest = await GetManifestCacheAsync(archivePath);
      manifest.TryGetValue(NormalizeEntryName(entry.FullName), out var creationDate);
      if (creationDate == default)
      {
        creationDate = entry.LastWriteTime.LocalDateTime;
      }

      var text = await Task.Run(() => ReadArchiveEntryTextWithManager(archivePath, NormalizeEntryName(entry.FullName)));
      return BuildEntryInfoFromText(
        archivePath,
        NormalizeEntryName(entry.FullName),
        entry.Name,
        text,
        creationDate,
        sourceFilePath: null,
        isReviewEntry: false,
        errorCount: ExtractErrorCount(text),
        fileType: FileType.OPKW);
    }

    /// <summary>
    /// Парсит текст файла и формирует объект информации о записи архива.
    /// </summary>
    /// <param name="archivePath">Путь к архиву.</param>
    /// <param name="entryName">Имя записи.</param>
    /// <param name="opkFileName">Имя файла ОПК.</param>
    /// <param name="text">Текст содержимого.</param>
    /// <param name="creationDate">Дата создания.</param>
    /// <param name="sourceFilePath">Путь к исходному файлу (для review).</param>
    /// <param name="isReviewEntry">Флаг review-записи.</param>
    /// <param name="errorCount">Количество ошибок.</param>
    /// <param name="fileType">Тип файла.</param>
    /// <returns>Информация о записи или null.</returns>
    private ArchiveEntryInfo? BuildEntryInfoFromText(
        string archivePath,
        string entryName,
        string opkFileName,
        string text,
        DateTime creationDate,
        string? sourceFilePath,
        bool isReviewEntry,
        int errorCount,
        FileType fileType)
    {
      Regex commandStartRegex = new(@"^\s*\d+\s+\S+", RegexOptions.Compiled);
      if (string.IsNullOrWhiteSpace(text))
      {
        return null;
      }

      var lines = text
        .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
        .Select(line => line.Trim())
        .ToList();

      if (lines.Count == 0)
      {
        return null;
      }

      var startIndex = lines.FindIndex(line => commandStartRegex.IsMatch(line));
      if (startIndex < 0)
      {
        return null;
      }

      var firstLine = Regex.Replace(lines[startIndex], @"^\s*\d+\s+\S+\s*", string.Empty);
      var starIndex = firstLine.IndexOf('*');

      var name = starIndex >= 0 ? firstLine[..starIndex].Trim() : firstLine.Trim();
      var nameOk = starIndex >= 0 ? firstLine[(starIndex + 1)..].Trim() : string.Empty;

      string? opk = null;
      string? ik = null;
      string? order = null;
      string? department = null;
      string? comment = null;
      List<string> kd = new();

      foreach (var line in lines.Skip(startIndex + 1))
      {
        var temp = Regex.Replace(line.ToLowerInvariant(), @"\s+", string.Empty);
        var eqIndex = line.IndexOf('=');
        var value = eqIndex >= 0 ? line[(eqIndex + 1)..].Trim() : string.Empty;

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
      }

      return new ArchiveEntryInfo(
        archivePath,
        NormalizeEntryName(entryName),
        name,
        nameOk,
        order,
        opkFileName,
        kd,
        department,
        comment,
        opk,
        ik,
        creationDate,
        sourceFilePath,
        isReviewEntry,
        errorCount,
        fileType);
    }

    /// <summary>
    /// Нормализует имя записи (замена слешей и удаление ведущих разделителей).
    /// </summary>
    /// <param name="entryName">Исходное имя записи.</param>
    /// <returns>Нормализованное имя.</returns>
    private static string NormalizeEntryName(string entryName)
    {
      return (entryName ?? string.Empty).Replace('\\', '/').TrimStart('/');
    }

    /// <summary>
    /// Проверяет, поддерживается ли файл для обработки в review.
    /// </summary>
    /// <param name="filePath">Путь к файлу.</param>
    /// <returns>true, если формат поддерживается; иначе false.</returns>
    private static bool IsSupportedReviewFilePath(string filePath)
    {
      var extension = Path.GetExtension(filePath);
      return string.Equals(extension, ".pk", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(extension, ".pkw", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Определяет тип файла по его расширению.
    /// </summary>
    /// <param name="filePath">Путь к файлу.</param>
    /// <returns>Тип файла.</returns>
    private static FileType DeterminePreviewFileType(string filePath)
    {
      return Path.GetExtension(filePath).ToLowerInvariant() switch
      {
        ".pk" => FileType.PK,
        ".pkw" => FileType.PKW,
        ".opk" => FileType.OPK,
        ".opkw" => FileType.OPKW,
        _ => FileType.None,
      };
    }

    /// <summary>
    /// Читает и нормализует текст файла на проверке с учётом кодировки.
    /// </summary>
    /// <param name="filePath">Путь к файлу.</param>
    /// <param name="fileType">Тип файла.</param>
    /// <returns>Нормализованный текст файла.</returns>
    private static string ReadReviewFileText(string filePath, FileType fileType)
    {
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

      var encoding = fileType == FileType.PKW || fileType == FileType.OPKW
        ? Encoding.UTF8
        : Encoding.GetEncoding(866);

      return CommandTranslationManager.NormalizeCommandMnemonics(
          TextSanitizer.RemoveLegacyControlChars(File.ReadAllText(filePath, encoding)))
        .Replace("\r\n", "\n")
        .Replace('\r', '\n');
    }

    /// <summary>
    /// Выполняет повторную проверку файла: пересчитывает ошибки и обновляет связанные файлы.
    /// </summary>
    /// <param name="filePath">Путь к файлу.</param>
    /// <returns>Результат повторной проверки.</returns>
    private static RecheckReviewFileResult RecheckReviewFile(string filePath)
    {
      if (string.IsNullOrWhiteSpace(filePath))
      {
        throw new ArgumentException("Не указан путь к файлу для повторной проверки.", nameof(filePath));
      }

      var fullFilePath = Path.GetFullPath(filePath);
      if (!File.Exists(fullFilePath))
      {
        throw new FileNotFoundException("Файл для повторной проверки не найден.", fullFilePath);
      }

      var fileType = DeterminePreviewFileType(fullFilePath);
      if (fileType != FileType.PK && fileType != FileType.PKW)
      {
        throw new InvalidOperationException("Повторная проверка поддерживается только для PK/PKW-файлов.");
      }

      var sourceText = ReadReviewFileText(fullFilePath, fileType);
      var normalizedSourceText = RemoveExistingReviewErrorHeader(sourceText);
      var manager = new CommandTranslationManager();
      var translation = manager.BuildTranslation(normalizedSourceText);
      var errorCount = translation.Models.Sum(model => model.Errors?.Count ?? 0);

      WriteReviewFileText(
        fullFilePath,
        errorCount > 0 ? BuildReviewErrorAnnotatedText(normalizedSourceText, errorCount) : normalizedSourceText,
        fileType);

      var opkwPath = Path.ChangeExtension(fullFilePath, ".opkw");
      if (errorCount > 0)
      {
        TryDeleteFile(opkwPath);
      }
      else
      {
        File.WriteAllText(opkwPath, translation.FormattedText, new UTF8Encoding(false));
      }

      return new RecheckReviewFileResult
      {
        FilePath = fullFilePath,
        ErrorCount = errorCount,
      };
    }

    /// <summary>
    /// Удаляет заголовок с информацией об ошибках из текста файла на проверке.
    /// </summary>
    /// <param name="text">Исходный текст.</param>
    /// <returns>Текст без заголовка ошибок.</returns>
    private static string RemoveExistingReviewErrorHeader(string text)
    {
      if (string.IsNullOrWhiteSpace(text))
      {
        return string.Empty;
      }

      var lines = text
        .Replace("\r\n", "\n")
        .Replace('\r', '\n')
        .Split('\n')
        .ToList();

      if (lines.Count == 0 || !Regex.IsMatch(lines[0], @"^\s*//=======\s*НАЙДЕНО\s+\d+\s+ОШИБ", RegexOptions.IgnoreCase))
      {
        return string.Join("\n", lines);
      }

      lines.RemoveAt(0);
      if (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[0]))
      {
        lines.RemoveAt(0);
      }

      return string.Join("\n", lines);
    }

    /// <summary>
    /// Создает текст с аннотациями об ошибках для файла на проверке.
    /// </summary>
    /// <param name="sourceText">Исходный текст.</param>
    /// <param name="errorCount">Количество ошибок.</param>
    /// <returns>Текст с аннотациями об ошибках.</returns>
    private static string BuildReviewErrorAnnotatedText(string sourceText, int errorCount)
    {
      var header = $"//=======НАЙДЕНО {errorCount} ОШИБОК";
      if (string.IsNullOrWhiteSpace(sourceText))
      {
        return header + "\r\n";
      }

      return header + "\r\n\r\n" + sourceText.Replace("\n", "\r\n");
    }

    /// <summary>
    /// Записывает текст в файл на проверке.
    /// </summary>
    /// <param name="filePath">Путь к файлу.</param>
    /// <param name="text">Текст для записи.</param>
    /// <param name="fileType">Тип файла.</param>
    private static void WriteReviewFileText(string filePath, string text, FileType fileType)
    {
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

      var encoding = fileType == FileType.PKW || fileType == FileType.OPKW
        ? Encoding.UTF8
        : Encoding.GetEncoding(866);

      var normalizedText = text.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n", "\r\n");

      if (encoding == Encoding.UTF8)
      {
        File.WriteAllText(filePath, normalizedText, new UTF8Encoding(false));
        return;
      }

      File.WriteAllText(filePath, normalizedText, encoding);
    }

    /// <summary>
    /// Извлекает количество ошибок из текста.
    /// </summary>
    /// <param name="text">Текст для извлечения количества ошибок.</param>
    /// <returns>Количество ошибок.</returns>
    private static int ExtractErrorCount(string text)
    {
      if (string.IsNullOrWhiteSpace(text))
      {
        return 0;
      }

      var firstLine = text
        .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
        .FirstOrDefault() ?? string.Empty;

      var match = Regex.Match(firstLine, @"НАЙДЕНО\s+(\d+)\s+ОШИБ", RegexOptions.IgnoreCase);
      return match.Success && int.TryParse(match.Groups[1].Value, out var errorCount)
        ? errorCount
        : 0;
    }

    /// <summary>
    /// Проверяет, имеет ли узел архива заполнитель.
    /// </summary>
    /// <param name="node">Узел архива.</param>
    /// <returns>True, если узел имеет заполнитель; в противном случае — false.</returns>
    private static bool HasPlaceholder(ArchiveTreeNode node)
    {
      return node.Children.Count == 1 && node.Children[0].Kind == ArchiveTreeNodeKind.Placeholder;
    }

    /// <summary>
    /// Пробует удалить файл.
    /// </summary>
    /// <param name="path">Путь к файлу.</param>
    private static void TryDeleteFile(string? path)
    {
      if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
      {
        return;
      }

      try
      {
        File.Delete(path);
      }
      catch
      {
      }
    }

    /// <summary>
    /// Обновляет видимость и раскладку правых панелей (список файлов и редактор).
    /// </summary>
    /// <param name="isFilesVisible">Отображать список файлов.</param>
    /// <param name="isEditorVisible">Отображать редактор.</param>
    private void UpdateRightPanels(bool isFilesVisible, bool isEditorVisible)
    {
      ViewModel.UpdateRightPanels(isEditorVisible);

      if (isEditorVisible)
      {
        FilesRowDefinition.Height = new GridLength(1, GridUnitType.Star);
        EditorRowDefinition.Height = new GridLength(1, GridUnitType.Star);

        EditorRowDefinition.MinHeight = 120;

        SplitterRowDefinition.MinHeight = 4;
      }
      else
      {
        FilesRowDefinition.Height = new GridLength(1, GridUnitType.Star);

        EditorRowDefinition.Height = new GridLength(0);
        EditorRowDefinition.MinHeight = 0;

        SplitterRowDefinition.MinHeight = 0;
      }
    }

    /// <summary>
    /// Устанавливает источник данных таблицы и обновляет текущие записи с подавлением событий выбора.
    /// </summary>
    /// <param name="entries">Список записей для отображения.</param>
    private void ApplyGridItemsSource(IReadOnlyList<ArchiveEntryInfo> entries)
    {
      _currentGridEntries = entries ?? Array.Empty<ArchiveEntryInfo>();

      _suppressGridSelection = true;
      try
      {
        ViewModel.SetFileEntries(_currentGridEntries);
      }
      finally
      {
        _suppressGridSelection = false;
      }
    }

    /// <summary>
    /// Проверяет наличие записи в буфере обмена архива.
    /// </summary>
    /// <returns>true, если буфер не пуст; иначе false.</returns>
    private bool HasArchiveClipboardEntry()
    {
      return _archiveClipboardEntry != null;
    }

    /// <summary>
    /// Сохраняет запись файла в буфер обмена архива.
    /// </summary>
    /// <param name="archivePath">Путь к архиву.</param>
    /// <param name="entryName">Имя записи.</param>
    /// <param name="displayName">Отображаемое имя.</param>
    /// <param name="operation">Тип операции (копирование или вырезание).</param>
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

    /// <summary>
    /// Очищает буфер обмена архива и обновляет состояние UI.
    /// </summary>
    /// <param name="wasConsumed">Флаг, указывающий, был ли буфер использован.</param>
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

    /// <summary>
    /// Вставляет файл из буфера обмена в указанный архив (копирование или перемещение).
    /// </summary>
    /// <param name="targetArchivePath">Путь к целевому архиву.</param>
    /// <returns>Асинхронная задача выполнения операции.</returns>
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

    /// <summary>
    /// Обновляет состояние и видимость кнопок действий в зависимости от текущего выбора.
    /// </summary>
    private void UpdateActionButtons()
    {
      UpdatePanelTitles();

      var hasArchive =
        !string.IsNullOrWhiteSpace(_lastSelectedArchivePath) &&
        (File.Exists(_lastSelectedArchivePath) || IsReviewArchivePath(_lastSelectedArchivePath));
      var isReviewArchive = hasArchive && IsReviewArchivePath(_lastSelectedArchivePath);
      var hasSelectedFile = !string.IsNullOrWhiteSpace(_lastSelectedArchivePath) && !string.IsNullOrWhiteSpace(_lastSelectedEntryName);
      var hasClipboardEntry = HasArchiveClipboardEntry();
      var canManageArchiveFiles = hasSelectedFile && !_lastSelectedIsReviewEntry;
      var canDeleteReviewFile = hasSelectedFile && _lastSelectedIsReviewEntry && !string.IsNullOrWhiteSpace(_lastSelectedReviewFilePath);

      ViewModel.UpdateActionButtons(
        _lastSelectedArchivePath,
        _lastSelectedEntryName,
        _lastSelectedReviewFilePath,
        _lastSelectedIsReviewEntry,
        hasClipboardEntry,
        IsReviewArchivePath);
    }

    /// <summary>
    /// Обновляет заголовки панелей в зависимости от текущего выбора.
    /// </summary>
    private void UpdatePanelTitles()
    {
      ViewModel.UpdatePanelTitles(_lastSelectedArchivePath, _lastSelectedEntryName);
    }

    /// <summary>
    /// Возвращает отображаемое имя архива по пути.
    /// </summary>
    /// <param name="archivePath">Путь к архиву.</param>
    /// <returns>Имя архива или null.</returns>
    private static string? GetArchiveDisplayName(string archivePath)
    {
      return string.IsNullOrWhiteSpace(archivePath)
        ? null
        : Path.GetFileName(archivePath);
    }

    /// <summary>
    /// Возвращает отображаемое имя файла по имени.
    /// </summary>
    /// <param name="entryName">Имя файла.</param>
    /// <returns>Отображаемое имя файла или null.</returns>
    private static string? GetFileDisplayName(string entryName)
    {
      return string.IsNullOrWhiteSpace(entryName)
        ? null
        : Path.GetFileName(entryName);
    }

    /// <summary>
    /// Устанавливает заголовок панели.
    /// </summary>
    /// <param name="textBlock">Элемент управления TextBlock.</param>
    /// <param name="value">Значение заголовка.</param>
    private static void SetPanelTitle(TextBlock textBlock, string? value)
    {
      var hasValue = !string.IsNullOrWhiteSpace(value);

      textBlock.Text = hasValue ? value : string.Empty;
      textBlock.ToolTip = hasValue ? value : null;
      textBlock.Visibility = hasValue ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// Проверяет, является ли указанный путь путем к архиву отзывов.
    /// </summary>
    /// <param name="path">Путь для проверки.</param>
    /// <returns>True, если путь указывает на архив отзывов; в противном случае — false.</returns>
    private bool IsReviewArchivePath(string path)
    {
      if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
      {
        return false;
      }

      var fullPath = Path.GetFullPath(path);
      return fullPath.StartsWith(_reviewArchivesFolderPath, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Отображает содержимое архива в таблице и обновляет состояние UI.
    /// </summary>
    /// <param name="archivePath">Путь к архиву.</param>
    /// <param name="clearEditor">Очистить редактор.</param>
    /// <returns>Асинхронная задача отображения.</returns>
    private async Task ShowArchiveInGridAsync(string archivePath, bool clearEditor)
    {
      if (IsReviewArchivePath(archivePath))
      {
        await ShowReviewArchiveInGridAsync(archivePath, clearEditor);
        return;
      }

      var integrityNotifications = await Task.Run(() => OpenArchiveInManager(archivePath));
      var entries = await GetArchiveEntriesAsync(archivePath);
      UpdateArchiveTreeState(archivePath, entries);

      ApplyGridItemsSource(entries);
      var baseHint = entries.Count == 0
        ? "Архив пуст."
        : "Выберите файл в архиве для просмотра..";
      ViewModel.SetFilesHint(integrityNotifications.Count == 0
        ? baseHint
        : $"{baseHint} Integrity warnings: {integrityNotifications.Count}." );

      if (clearEditor)
      {
        _lastSelectedEntryName = null;
        _suppressGridSelection = true;
        ArchiveFilesDataGrid.SelectedItem = null;
        _suppressGridSelection = false;
        ViewModel.ClearPreview();
        _lastSelectedReviewFilePath = null;
        _lastSelectedIsReviewEntry = false;
        ViewModel.SetEditorHint("Выберите файл в архиве для просмотра..");
        UpdateRightPanels(isFilesVisible: true, isEditorVisible: false);
      }
      else
      {
        var hasEditorText = ViewModel.HasPreviewContent();
        UpdateRightPanels(isFilesVisible: true, isEditorVisible: hasEditorText);
      }

      UpdateActionButtons();
    }

    /// <summary>
    /// Отображает содержимое файла архива в редакторе и обновляет состояние UI.
    /// </summary>
    /// <param name="archivePath">Путь к архиву.</param>
    /// <param name="entryName">Имя файла.</param>
    /// <param name="fromGrid">Флаг вызова из таблицы.</param>
    /// <returns>Асинхронная задача отображения.</returns>
    private async Task ShowFileAsync(string archivePath, string entryName, bool fromGrid)
    {
      if (IsReviewArchivePath(archivePath))
      {
        await ShowReviewFileAsync(archivePath, entryName, fromGrid);
        return;
      }

      await ShowArchiveInGridAsync(archivePath, clearEditor: false);
      var text = await Task.Run(() => ReadArchiveEntryTextWithManager(archivePath, entryName));
      var normalizedEntryName = NormalizeEntryName(entryName);

      ViewModel.SetPreview(text, FileType.OPKW);
      _lastSelectedArchivePath = archivePath;
      _lastSelectedEntryName = normalizedEntryName;
      _lastSelectedReviewFilePath = null;
      _lastSelectedIsReviewEntry = false;

      ViewModel.SetEditorHint("Содержимое файла доступно только для чтения.");
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

    /// <summary>
    /// Отображает содержимое архива отзывов в таблице и обновляет состояние UI.
    /// </summary>
    /// <param name="reviewDirectoryPath">Путь к директории отзывов.</param>
    /// <param name="clearEditor">Очистить редактор.</param>
    /// <returns>Асинхронная задача отображения.</returns>
    private async Task ShowReviewArchiveInGridAsync(string reviewDirectoryPath, bool clearEditor)
    {
      var entries = await GetReviewEntriesAsync(reviewDirectoryPath);
      UpdateArchiveTreeState(reviewDirectoryPath, entries);

      ApplyGridItemsSource(entries);
      ViewModel.SetFilesHint(entries.Count == 0
        ? "Архив на проверке пуст."
        : "Выберите файл на проверке для просмотра.");

      if (clearEditor)
      {
        _lastSelectedEntryName = null;
        _lastSelectedReviewFilePath = null;
        _lastSelectedIsReviewEntry = false;
        _suppressGridSelection = true;
        ArchiveFilesDataGrid.SelectedItem = null;
        _suppressGridSelection = false;
        ViewModel.ClearPreview();
        ViewModel.SetEditorHint("Select a review file to preview.");
        UpdateRightPanels(isFilesVisible: true, isEditorVisible: false);
      }
      else
      {
        var hasEditorText = ViewModel.HasPreviewContent();
        UpdateRightPanels(isFilesVisible: true, isEditorVisible: hasEditorText);
      }

      UpdateActionButtons();
    }

    /// <summary>
    /// Отображает содержимое файла из архива на проверке в редакторе.
    /// </summary>
    /// <param name="reviewDirectoryPath">Путь к архиву на проверке.</param>
    /// <param name="entryName">Имя файла.</param>
    /// <param name="fromGrid">Флаг вызова из таблицы.</param>
    /// <returns>Асинхронная задача отображения.</returns>
    private async Task ShowReviewFileAsync(string reviewDirectoryPath, string entryName, bool fromGrid)
    {
      await ShowReviewArchiveInGridAsync(reviewDirectoryPath, clearEditor: false);

      var entries = await GetReviewEntriesAsync(reviewDirectoryPath);
      var selectedEntry = entries.FirstOrDefault(item =>
        string.Equals(item.EntryName, NormalizeEntryName(entryName), StringComparison.OrdinalIgnoreCase));

      if (selectedEntry == null || string.IsNullOrWhiteSpace(selectedEntry.SourceFilePath) || !File.Exists(selectedEntry.SourceFilePath))
      {
        return;
      }

      var text = await Task.Run(() => ReadReviewFileText(selectedEntry.SourceFilePath, selectedEntry.FileType));
      ViewModel.SetPreview(text, selectedEntry.FileType);

      _lastSelectedArchivePath = reviewDirectoryPath;
      _lastSelectedEntryName = NormalizeEntryName(entryName);
      _lastSelectedReviewFilePath = selectedEntry.SourceFilePath;
      _lastSelectedIsReviewEntry = true;

      ViewModel.SetEditorHint("Содержимое файла доступно только для чтения.");
      UpdateActionButtons();
      UpdateRightPanels(true, true);

      if (!fromGrid)
      {
        await SelectGridRow(selectedEntry);
      }
    }


    /// <summary>
    /// Выбирает строку в таблице архивов.
    /// </summary>
    /// <param name="selectedRow">Выбранная строка.</param>
    /// <returns>Асинхронная задача выбора строки.</returns>
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

    /// <summary>
    /// Открывает архив по пути с переключением в UI-поток при необходимости.
    /// </summary>
    /// <param name="archivePath">Путь к архиву.</param>
    /// <returns>Асинхронная задача открытия.</returns>
    public Task OpenArchivePathAsync(string archivePath)
    {
      if (!Dispatcher.CheckAccess())
      {
        return Dispatcher.InvokeAsync(() => OpenArchivePathAsync(archivePath)).Task.Unwrap();
      }

      return OpenArchivePathCoreAsync(archivePath);
    }

    /// <summary>
    /// Открывает архив на проверке по пути с переключением в UI-поток при необходимости.
    /// </summary>
    /// <param name="reviewArchivePath">Путь к архиву на проверке.</param>
    /// <returns>Асинхронная задача открытия.</returns>
    public Task OpenReviewArchivePathAsync(string reviewArchivePath)
    {
      if (!Dispatcher.CheckAccess())
      {
        return Dispatcher.InvokeAsync(() => OpenReviewArchivePathAsync(reviewArchivePath)).Task.Unwrap();
      }

      return OpenReviewArchivePathCoreAsync(reviewArchivePath);
    }

    /// <summary>
    /// Открывает архив по пути: загружает дерево и отображает содержимое.
    /// </summary>
    /// <param name="archivePath">Путь к архиву.</param>
    /// <returns>Асинхронная задача открытия.</returns>
    private async Task OpenArchivePathCoreAsync(string archivePath)
    {
      if (string.IsNullOrWhiteSpace(archivePath))
      {
        throw new ArgumentException("Требуется указать путь к архиву.", nameof(archivePath));
      }

      var fullArchivePath = Path.GetFullPath(archivePath);
      if (!File.Exists(fullArchivePath))
      {
        throw new FileNotFoundException("Архив не найден.", fullArchivePath);
      }

      var rootNode = GetRootNode(ArchiveTreeNodeKind.Root);
      if (rootNode == null)
      {
        ResetTree();
        rootNode = GetRootNode(ArchiveTreeNodeKind.Root);
      }

      if (rootNode != null)
      {
        rootNode.IsExpanded = true;
        await LoadArchivesIntoRootAsync(rootNode);

        var archiveNode = rootNode.Children.FirstOrDefault(node =>
          node.Kind == ArchiveTreeNodeKind.Archive &&
          IsSameArchivePath(node.ArchivePath, fullArchivePath));

        if (archiveNode != null)
        {
          archiveNode.IsExpanded = true;
          await LoadArchiveFilesIntoTreeAsync(archiveNode);
        }
      }

      await OpenArchiveAsync(fullArchivePath);
    }

    /// <summary>
    /// Открывает архив на проверке по пути: загружает дерево и отображает содержимое.
    /// </summary>
    /// <param name="reviewArchivePath">Путь к архиву на проверке.</param>
    /// <returns>Асинхронная задача открытия.</returns>
    private async Task OpenReviewArchivePathCoreAsync(string reviewArchivePath)
    {
      if (string.IsNullOrWhiteSpace(reviewArchivePath))
      {
        throw new ArgumentException("Требуется указать путь к архиву на проверке.", nameof(reviewArchivePath));
      }

      var fullReviewArchivePath = Path.GetFullPath(reviewArchivePath);
      if (!Directory.Exists(fullReviewArchivePath))
      {
        throw new DirectoryNotFoundException($"Архив на проверке не найден: {fullReviewArchivePath}");
      }

      var rootNode = GetRootNode(ArchiveTreeNodeKind.ReviewRoot);
      if (rootNode == null)
      {
        ResetTree();
        rootNode = GetRootNode(ArchiveTreeNodeKind.ReviewRoot);
      }

      if (rootNode != null)
      {
        rootNode.IsExpanded = true;
        await LoadReviewArchivesIntoRootAsync(rootNode);

        var reviewNode = rootNode.Children.FirstOrDefault(node =>
          node.Kind == ArchiveTreeNodeKind.ReviewArchive &&
          IsSameArchivePath(node.ArchivePath, fullReviewArchivePath));

        if (reviewNode != null)
        {
          reviewNode.IsExpanded = true;
          await LoadReviewFilesIntoTreeAsync(reviewNode);
        }
      }

      await OpenArchiveAsync(fullReviewArchivePath);
    }

    /// <summary>
    /// Обрабатывает событие раскрытия элемента дерева.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
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

        if (node.Kind == ArchiveTreeNodeKind.ReviewRoot)
        {
          await LoadReviewArchivesIntoRootAsync(node);
          return;
        }

        if (node.Kind == ArchiveTreeNodeKind.Archive)
        {
          await LoadArchiveFilesIntoTreeAsync(node);
          return;
        }

        if (node.Kind == ArchiveTreeNodeKind.ReviewArchive)
        {
          await LoadReviewFilesIntoTreeAsync(node);
        }
      }
      catch (Exception ex)
      {
        ShowArchiveNotification("Архивы", GetUserFriendlyArchiveErrorMessage(ex), NotificationType.Error);
      }
    }

    /// <summary>
    /// Обрабатывает событие нажатия левой кнопки мыши на элементе дерева.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void TreeViewItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      var item = sender as TreeViewItem;
      var node = item?.DataContext as ArchiveTreeNode;

      LogInformation(
        $"Archive UI: PreviewMouseLeftButtonDown kind='{node?.Kind}', display='{node?.DisplayName}', button='{e.ChangedButton}', original='{e.OriginalSource?.GetType().Name}', selectedBefore='{item?.IsSelected}'.");

      if (e.OriginalSource is DependencyObject originalSource)
      {
        var toggleButton = FindVisualAncestor<ToggleButton>(originalSource);
        if (toggleButton != null)
        {
          LogInformation(
            $"Archive UI: PreviewMouseLeftButtonDown ignored expander for '{node?.DisplayName}'.");
          return;
        }

        var hitTreeViewItem = FindVisualAncestor<TreeViewItem>(originalSource);
        if (item != null && hitTreeViewItem != null && !ReferenceEquals(item, hitTreeViewItem))
        {
          LogInformation(
            $"Archive UI: PreviewMouseLeftButtonDown ignored ancestor sender='{node?.DisplayName}', hit='{(hitTreeViewItem.DataContext as ArchiveTreeNode)?.DisplayName}'.");
          return;
        }
      }

      if (item == null)
      {
        LogInformation("Archive UI: PreviewMouseLeftButtonDown sender is not TreeViewItem.");
        return;
      }

      var canToggleExpansion =
        node?.Kind == ArchiveTreeNodeKind.Root ||
        node?.Kind == ArchiveTreeNodeKind.ReviewRoot ||
        node?.Kind == ArchiveTreeNodeKind.Archive ||
        node?.Kind == ArchiveTreeNodeKind.ReviewArchive;

      if (canToggleExpansion)
      {
        item.IsExpanded = !item.IsExpanded;

        LogInformation(
          $"Archive UI: PreviewMouseLeftButtonDown toggled expansion kind='{node?.Kind}', display='{node?.DisplayName}', expandedAfter='{item.IsExpanded}'.");
      }

      item.IsSelected = true;
      item.Focus();

      LogInformation(
        $"Archive UI: PreviewMouseLeftButtonDown selected kind='{node?.Kind}', display='{node?.DisplayName}', selectedAfter='{item.IsSelected}'.");
    }

    /// <summary>
    /// Обрабатывает правый клик по узлу дерева и сохраняет контекстный узел.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события мыши.</param>
    private void TreeViewItem_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
      var item = sender as TreeViewItem;
      if (item == null)
      {
        return;
      }

      _contextMenuNode = item.DataContext as ArchiveTreeNode;
    }

    /// <summary>
    /// Возвращает текущий контекстный узел (из контекстного меню или выбранный).
    /// </summary>
    /// <returns>Узел дерева или null.</returns>
    private ArchiveTreeNode? GetContextNode()
    {
      return _contextMenuNode ?? (ArchivesTreeView.SelectedItem as ArchiveTreeNode);
    }

    /// <summary>
    /// Ищет родительский элемент заданного типа в визуальном дереве.
    /// </summary>
    /// <typeparam name="T">Тип искомого элемента.</typeparam>
    /// <param name="current">Начальный элемент.</param>
    /// <returns>Найденный элемент или null.</returns>
    private static T? FindVisualAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
      while (current != null)
      {
        if (current is T typed)
        {
          return typed;
        }

        current = VisualTreeHelper.GetParent(current);
      }

      return null;
    }

    /// <summary>
    /// Возвращает узел дерева для печати.
    /// </summary>
    /// <returns>Узел дерева или null.</returns>
    private ArchiveTreeNode GetNodeForPrint()
    {
      return _contextMenuNode ?? ArchivesTreeView.SelectedItem as ArchiveTreeNode;
    }

    /// <summary>
    /// Обрабатывает событие закрытия контекстного меню дерева архивов.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void ArchivesTreeContextMenu_Closed(object sender, RoutedEventArgs e)
    {
      _contextMenuNode = null;
      ViewModel.UpdateContextMenu(null, HasArchiveClipboardEntry());
    }
    /// <summary>
    /// Обеспечивает получение записей архива для печати (из текущего состояния или с загрузкой).
    /// </summary>
    /// <returns>Кортеж: список записей и путь к архиву.</returns>
    private async Task<(IReadOnlyList<ArchiveEntryInfo> entries, string archivePath)> EnsureEntriesForPrintAsync()
    {
      var node = GetNodeForPrint();

      if (node == null || node.Kind != ArchiveTreeNodeKind.Archive || node.ArchivePath == null)
        return (null, null);

      var archivePath = node.ArchivePath;

      if (_lastSelectedArchivePath == archivePath &&
          _currentGridEntries != null &&
          _currentGridEntries.Count > 0)
      {
        return (_currentGridEntries, archivePath);
      }

      var entries = await GetArchiveEntriesAsync(archivePath);

      return (entries, archivePath);
    }

    /// <summary>
    /// Печатает каталог архива.
    /// </summary>
    /// <returns>Задача для асинхронного выполнения.</returns>
    private async Task PrintArchiveCatalogAsync()
    {
      var (entries, archivePath) = await EnsureEntriesForPrintAsync();

      if (entries == null || entries.Count == 0)
      {
        MessageBoxCustom.Show("??? ?????? ??? ??????", "?????? ??????", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }

      var archiveName = System.IO.Path.GetFileName(archivePath);
      ArchivePrintDialogService.ShowAndPrint(
        this,
        "Каталог архива",
        (hardMarginX, hardMarginY, printableAreaWidth, printableAreaHeight) => CreatePrintDocument(
          entries,
          archiveName,
          hardMarginX,
          hardMarginY,
          printableAreaWidth,
          printableAreaHeight));
    }

    /// <summary>
    /// Создает документ для печати.
    /// </summary>
    /// <param name="entries">Список записей архива.</param>
    /// <param name="archiveName">Имя архива.</param>
    /// <param name="hardMarginX">Горизонтальный отступ.</param>
    /// <param name="hardMarginY">Вертикальный отступ.</param>
    /// <param name="printableAreaWidth">Ширина печатной области.</param>
    /// <param name="printableAreaHeight">Высота печатной области.</param>
    /// <returns>Документ для печати.</returns>
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
        PageWidth = printableAreaWidth + (hardMarginX * 2),
        PageHeight = printableAreaHeight + (hardMarginY * 2),
        ColumnWidth = double.PositiveInfinity
      };

      var availableTableWidth = Math.Max(0, printableAreaWidth - doc.PagePadding.Left - doc.PagePadding.Right);

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

    /// <summary>
    /// Создаёт ячейку таблицы для печати с заданным текстом.
    /// </summary>
    /// <param name="text">Текст ячейки.</param>
    /// <returns>Элемент Paragraph для таблицы.</returns>
    private Paragraph CreateCell(string text) =>
    new Paragraph(new Run(text ?? ""))
    {
      Margin = new Thickness(0),
      TextAlignment = TextAlignment.Left
    };

    /// <summary>
    /// Обновляет состояние и видимость пунктов контекстного меню в зависимости от выбранного узла.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void ArchivesTreeContextMenu_Opened(object sender, RoutedEventArgs e)
    {
      var node = GetContextNode();
      ViewModel.UpdateContextMenu(node, HasArchiveClipboardEntry());

      var isSupportedNode =
        node?.Kind == ArchiveTreeNodeKind.Root ||
        node?.Kind == ArchiveTreeNodeKind.Archive ||
        node?.Kind == ArchiveTreeNodeKind.File ||
        node?.Kind == ArchiveTreeNodeKind.ReviewArchive ||
        node?.Kind == ArchiveTreeNodeKind.ReviewFile;

      if (!isSupportedNode)
      {
        var contextMenu = sender as ContextMenu;
        if (contextMenu != null)
        {
          contextMenu.IsOpen = false;
        }
      }
    }

    /// <summary>
    /// Обрабатывает событие клика по пункту меню "Создать архив".
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void CreateArchiveMenuItem_Click(object sender, RoutedEventArgs e)
    {
      BeginCreateArchiveWorkflow();
    }

    /// <summary>
    /// Обрабатывает событие клика по пункту меню "Загрузить архив".
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void UploadArchiveMenuItem_Click(object sender, RoutedEventArgs e)
    {
      ArchiveTransferUiService.UploadArchive();
    }

    /// <summary>
    /// Обрабатывает событие клика по пункту меню "Скачать архивы".
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void DownloadArchivesMenuItem_Click(object sender, RoutedEventArgs e)
    {
      ArchiveTransferUiService.DownloadArchives();
    }

    /// <summary>
    /// Обрабатывает событие клика по пункту меню "Открыть архив".
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private async void OpenArchiveMenuItem_Click(object sender, RoutedEventArgs e)
    {
      var node = GetContextNode();
      if (node == null || string.IsNullOrWhiteSpace(node.ArchivePath))
      {
        return;
      }

      if (node.Kind == ArchiveTreeNodeKind.Archive || node.Kind == ArchiveTreeNodeKind.ReviewArchive)
      {
        await OpenArchiveAsync(node.ArchivePath);
      }
    }

    /// <summary>
    /// Обрабатывает событие клика по пункту меню "Сохранить архив".
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
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

    /// <summary>
    /// Обрабатывает событие клика по пункту меню "Добавить файл в архив".
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void AddFileToArchiveMenuItem_Click(object sender, RoutedEventArgs e)
    {
      var node = GetContextNode();
      if (node?.Kind != ArchiveTreeNodeKind.Archive || string.IsNullOrWhiteSpace(node.ArchivePath))
      {
        return;
      }

      AddFileToArchive(node.ArchivePath);
    }

    /// <summary>
    /// Обрабатывает событие клика по пункту меню "Удалить архив".
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private async void DeleteArchiveMenuItem_Click(object sender, RoutedEventArgs e)
    {
      var node = GetContextNode();
      if (node == null || string.IsNullOrWhiteSpace(node.ArchivePath))
      {
        return;
      }

      if (node.Kind == ArchiveTreeNodeKind.Archive)
      {
        DeleteArchive(node.ArchivePath, Path.GetFileNameWithoutExtension(node.ArchivePath));
        return;
      }

      if (node.Kind == ArchiveTreeNodeKind.ReviewArchive)
      {
        await DeleteReviewArchiveAsync(node.ArchivePath, Path.GetFileName(node.ArchivePath));
      }
    }

    /// <summary>
    /// Обрабатывает открытие файла или архива из контекстного меню.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private async void OpenArchiveFileMenuItem_Click(object sender, RoutedEventArgs e)
    {
      var node = GetContextNode();
      if (node == null || string.IsNullOrWhiteSpace(node.ArchivePath))
      {
        return;
      }

      if ((node.Kind == ArchiveTreeNodeKind.File || node.Kind == ArchiveTreeNodeKind.ReviewFile) &&
          !string.IsNullOrWhiteSpace(node.EntryName))
      {
        await OpenArchiveFileAsync(node.ArchivePath, node.EntryName);
        return;
      }

      if (node.Kind == ArchiveTreeNodeKind.ReviewArchive)
      {
        await OpenArchiveAsync(node.ArchivePath);
      }
    }

    /// <summary>
    /// Открывает файл из архива на проверке во внешнем текстовом редакторе.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void OpenInTextEditorMenuItem_Click(object sender, RoutedEventArgs e)
    {
      var node = GetContextNode();
      if (node?.Kind != ArchiveTreeNodeKind.ReviewFile || string.IsNullOrWhiteSpace(node.FilePath))
      {
        return;
      }

      FileInteractionEventAdapter.RaiseOpenFileInEditorAgain(node.FilePath);
    }

    /// <summary>
    /// Обрабатывает событие клика по пункту меню "Удалить файл из архива".
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private async void DeleteArchiveFileMenuItem_Click(object sender, RoutedEventArgs e)
    {
      var node = GetContextNode();
      if (node == null ||
          string.IsNullOrWhiteSpace(node.ArchivePath) ||
          string.IsNullOrWhiteSpace(node.EntryName))
      {
        return;
      }

      if (node.Kind == ArchiveTreeNodeKind.File)
      {
        DeleteArchiveFile(node.ArchivePath, node.EntryName, node.DisplayName);
        return;
      }

      if (node.Kind == ArchiveTreeNodeKind.ReviewFile)
      {
        await DeleteReviewFileAsync(node.ArchivePath, node.EntryName, node.DisplayName, node.FilePath);
      }
    }

    /// <summary>
    /// Обрабатывает событие клика по пункту меню "Копировать файл из архива".
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
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

    /// <summary>
    /// Обрабатывает событие клика по пункту меню "Вырезать файл из архива".
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
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

    /// <summary>
    /// Обрабатывает событие клика по пункту меню "Вставить файл в архив".
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
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

    /// <summary>
    /// Обрабатывает событие клика по кнопке "Добавить файл в архив".
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void AddFileToArchiveButton_Click(object sender, RoutedEventArgs e)
    {
      if (!string.IsNullOrWhiteSpace(_lastSelectedArchivePath))
      {
        AddFileToArchive(_lastSelectedArchivePath);
      }
    }

    /// <summary>
    /// Обрабатывает нажатие кнопки сохранения архива на диск.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void SaveArchiveToDiskButton_Click(object sender, RoutedEventArgs e)
    {
      SaveSelectedArchiveToDisk();
      ResetArchiveActionButtonFocus();
    }

    /// <summary>
    /// Обрабатывает нажатие кнопки удаления архива.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    public async Task DeleteSelectedArchiveAsync()
    {
      if (!string.IsNullOrWhiteSpace(_lastSelectedArchivePath))
      {
        if (IsReviewArchivePath(_lastSelectedArchivePath))
        {
          await DeleteReviewArchiveAsync(_lastSelectedArchivePath, Path.GetFileName(_lastSelectedArchivePath));
          return;
        }

        DeleteArchive(_lastSelectedArchivePath, Path.GetFileNameWithoutExtension(_lastSelectedArchivePath));
      }
    }

    /// <summary>
    /// Обрабатывает нажатие кнопки вставки файла в архив из буфера.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    public async Task PasteIntoSelectedArchiveAsync()
    {
      if (!string.IsNullOrWhiteSpace(_lastSelectedArchivePath))
      {
        await PasteArchiveClipboardToAsync(_lastSelectedArchivePath);
      }
    }

    /// <summary>
    /// Обрабатывает нажатие кнопки удаления файла из архива.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    public async Task DeleteSelectedFileAsync()
    {
      if (!string.IsNullOrWhiteSpace(_lastSelectedArchivePath) &&
          !string.IsNullOrWhiteSpace(_lastSelectedEntryName))
      {
        if (_lastSelectedIsReviewEntry)
        {
          await DeleteReviewFileAsync(
            _lastSelectedArchivePath,
            _lastSelectedEntryName,
            Path.GetFileName(_lastSelectedEntryName),
            _lastSelectedReviewFilePath);
          return;
        }

        DeleteArchiveFile(_lastSelectedArchivePath, _lastSelectedEntryName, Path.GetFileName(_lastSelectedEntryName));
      }
    }

    /// <summary>
    /// Обрабатывает нажатие кнопки копирования файла в архив.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    public void CopySelectedFile()
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

    /// <summary>
    /// Обрабатывает нажатие кнопки вырезания файла из архива.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    public void CutSelectedFile()
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

    /// <summary>
    /// Обрабатывает нажатие кнопки вставки файла из буфера в архив.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    public async Task PasteIntoSelectedFileArchiveAsync()
    {
      if (!string.IsNullOrWhiteSpace(_lastSelectedArchivePath))
      {
        await PasteArchiveClipboardToAsync(_lastSelectedArchivePath);
      }
    }

    /// <summary>
    /// Обрабатывает нажатие кнопки печати каталога архива.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private async void PrintArchiveCatalogButton_Click(object sender, RoutedEventArgs e)
    {
      await PrintArchiveCatalogAsync();
      ResetArchiveActionButtonFocus();
    }

    /// <summary>
    /// Сбрасывает фокус с кнопок действий над архивом.
    /// </summary>
    private void ResetArchiveActionButtonFocus()
    {
      Dispatcher.BeginInvoke(new Action(() =>
      {
        Keyboard.ClearFocus();
      }), DispatcherPriority.Background);
    }

    /// <summary>
    /// Обрабатывает выбор пункта меню печати каталога архива.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private async void PrintArchiveCatalogMenuItem_Click(object sender, RoutedEventArgs e)
    {
      await PrintArchiveCatalogAsync();
    }

    /// <summary>
    /// Начинает процесс создания архива.
    /// </summary>
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

    /// <summary>
    /// Начинает процесс открытия архива.
    /// </summary>
    /// <param name="archivePath">Путь к архиву.</param>
    private async Task OpenArchiveAsync(string archivePath)
    {
      try
      {
        LogInformation($"Archive UI: OpenArchiveAsync archive='{archivePath}'.");
        _lastSelectedArchivePath = archivePath;
        _lastSelectedEntryName = null;
        await ShowArchiveInGridAsync(archivePath, clearEditor: true);
      }
      catch (Exception ex)
      {
        ShowArchiveNotification("Открытие архива", GetUserFriendlyArchiveErrorMessage(ex), NotificationType.Error);
      }
    }

    /// <summary>
    /// Открывает файл из архива и отображает его содержимое.
    /// </summary>
    /// <param name="archivePath">Путь к архиву.</param>
    /// <param name="entryName">Имя файла внутри архива.</param>
    /// <returns>Асинхронная задача открытия файла.</returns>
    private async Task OpenArchiveFileAsync(string archivePath, string entryName)
    {
      try
      {
        LogInformation($"Archive UI: OpenArchiveFileAsync archive='{archivePath}', entry='{entryName}'.");
        _lastSelectedArchivePath = archivePath;
        _lastSelectedEntryName = entryName;
        await ShowFileAsync(archivePath, entryName, false);
      }
      catch (Exception ex)
      {
        ShowArchiveNotification("Открытие файла", GetUserFriendlyArchiveErrorMessage(ex), NotificationType.Error);
      }
    }

    /// <summary>
    /// Добавляет файл в архив.
    /// </summary>
    /// <param name="archivePath">Путь к архиву.</param>
    private void AddFileToArchive(string archivePath)
    {
      var filePath = ArchiveFileDialogService.SelectFileToAddToArchive(this);
      if (string.IsNullOrWhiteSpace(filePath))
      {
        return;
      }

      try
      {
        lock (_archiveManagerSync)
        {
          EnsureArchiveOpenedInManagerCore(archivePath);
          _archiveManager.AddFileToOpenedArchive(filePath);
        }

        ShowArchiveNotification(
          "???????????????????? ??????????",
          $"???????? '{Path.GetFileName(filePath)}' ?????????????? ???????????????? ?? ?????????? '{Path.GetFileNameWithoutExtension(archivePath)}'.",
          NotificationType.Success);
      }
      catch (Exception ex)
      {
        ShowArchiveNotification("???????????????????? ??????????", GetUserFriendlyArchiveErrorMessage(ex), NotificationType.Error);
      }
    }

    /// <summary>
    /// Сохраняет выбранный архив на диск.
    /// </summary>
    public void SaveSelectedArchiveToDisk()
    {
      if (string.IsNullOrWhiteSpace(_lastSelectedArchivePath) || !File.Exists(_lastSelectedArchivePath))
      {
        ShowArchiveNotification("???????????????????? ????????????", "???????????????? ?????????? ?????? ???????????????????? ???? ????????.", NotificationType.Warning);
        return;
      }

      var destinationPath = ArchiveFileDialogService.SelectArchiveExportPath(this, Path.GetFileName(_lastSelectedArchivePath));
      if (string.IsNullOrWhiteSpace(destinationPath))
      {
        return;
      }

      try
      {
        var savedArchivePath = ArchiveTransferService.ExportArchive(_lastSelectedArchivePath, destinationPath);
        ShowArchiveNotification(
          "???????????????????? ????????????",
          $"?????????? '{Path.GetFileName(savedArchivePath)}' ?????????????? ???????????????? ???? ????????.",
          NotificationType.Success);
      }
      catch (Exception ex)
      {
        ShowArchiveNotification("???????????????????? ????????????", GetUserFriendlyArchiveErrorMessage(ex), NotificationType.Error);
      }
    }

    /// <summary>
    /// Удаляет архив.
    /// </summary>
    /// <param name="archivePath">Путь к архиву.</param>
    /// <param name="displayName">Отображаемое имя архива.</param>
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

    /// <summary>
    /// Удаляет файл из архива с подтверждением пользователя.
    /// </summary>
    /// <param name="archivePath">Путь к архиву.</param>
    /// <param name="entryName">Имя файла внутри архива.</param>
    /// <param name="displayName">Отображаемое имя файла.</param>
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

    /// <summary>
    /// Удаляет архив на проверке.
    /// </summary>
    /// <param name="reviewArchivePath">Путь к архиву на проверке.</param>
    /// <param name="displayName">Отображаемое имя архива.</param>
    /// <returns>Асинхронная задача удаления архива.</returns>
    private async Task DeleteReviewArchiveAsync(string reviewArchivePath, string displayName)
    {
      var confirmation = Message.MessageBoxCustom.Show(
        $"Удалить архив на проверке '{displayName}'?",
        "Удаление архива",
        MessageBoxButton.YesNo,
        MessageBoxImage.Question);

      if (confirmation != MessageBoxResult.Yes)
      {
        return;
      }

      try
      {
        var fullReviewArchivePath = Path.GetFullPath(reviewArchivePath);
        Directory.Delete(fullReviewArchivePath, recursive: true);
        InvalidateArchiveCaches(fullReviewArchivePath);

        var deletedSelectedArchive = IsSameArchivePath(_lastSelectedArchivePath, fullReviewArchivePath);
        if (deletedSelectedArchive)
        {
          _lastSelectedArchivePath = null;
          _lastSelectedEntryName = null;
          _lastSelectedReviewFilePath = null;
          _lastSelectedIsReviewEntry = false;
        }

        await RefreshTreePreservingStateAsync(preservePanels: !deletedSelectedArchive);

        ShowArchiveNotification(
          "Удаление архива",
          $"Архив на проверке '{displayName}' успешно удалён.",
          NotificationType.Success);
      }
      catch (Exception ex)
      {
        ShowArchiveNotification("Удаление архива", GetUserFriendlyArchiveErrorMessage(ex), NotificationType.Error);
      }
    }

    /// <summary>
    /// Удаляет файл из архива на проверке вместе с сопутствующими файлами и обновляет состояние UI.
    /// </summary>
    /// <param name="reviewArchivePath">Путь к архиву на проверке.</param>
    /// <param name="entryName">Имя файла внутри архива.</param>
    /// <param name="displayName">Отображаемое имя файла.</param>
    /// <param name="filePath">Полный путь к исходному файлу (если известен).</param>
    /// <returns>Асинхронная задача удаления.</returns>
    private async Task DeleteReviewFileAsync(string reviewArchivePath, string entryName, string displayName, string? filePath)
    {
      var confirmation = Message.MessageBoxCustom.Show(
        $"Удалить файл '{displayName}' из архива на проверке?",
        "Удаление файла",
        MessageBoxButton.YesNo,
        MessageBoxImage.Question);

      if (confirmation != MessageBoxResult.Yes)
      {
        return;
      }

      try
      {
        var fullReviewArchivePath = Path.GetFullPath(reviewArchivePath);
        var fullFilePath = !string.IsNullOrWhiteSpace(filePath)
          ? Path.GetFullPath(filePath)
          : Path.Combine(fullReviewArchivePath, Path.GetFileName(entryName));

        if (File.Exists(fullFilePath))
        {
          File.Delete(fullFilePath);
        }

        var companionOpkwPath = Path.ChangeExtension(fullFilePath, ".opkw");
        if (!string.Equals(companionOpkwPath, fullFilePath, StringComparison.OrdinalIgnoreCase) && File.Exists(companionOpkwPath))
        {
          File.Delete(companionOpkwPath);
        }

        InvalidateArchiveCaches(fullReviewArchivePath);

        var deletedSelectedFile =
          IsSameArchivePath(_lastSelectedArchivePath, fullReviewArchivePath) &&
          string.Equals(NormalizeEntryName(_lastSelectedEntryName), NormalizeEntryName(entryName), StringComparison.OrdinalIgnoreCase);

        if (deletedSelectedFile)
        {
          _lastSelectedEntryName = null;
          _lastSelectedReviewFilePath = null;
          _lastSelectedIsReviewEntry = false;
        }

        await RefreshTreePreservingStateAsync(preservePanels: !deletedSelectedFile);

        ShowArchiveNotification(
          "Удаление файла",
          $"Файл '{displayName}' успешно удалён из архива на проверке '{Path.GetFileName(fullReviewArchivePath)}'.",
          NotificationType.Success);
      }
      catch (Exception ex)
      {
        ShowArchiveNotification("Удаление файла", GetUserFriendlyArchiveErrorMessage(ex), NotificationType.Error);
      }
    }

    /// <summary>
    /// Обрабатывает нажатие кнопки конвертации текущего содержимого в файл PKW.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    public void ConvertSelectedFileToPkw()
    {
      var fileText = GetFileContentText();
      if (string.IsNullOrWhiteSpace(fileText))
      {
        ShowArchiveNotification("Конвертация в PKW", "Нет данных для сохранения.", NotificationType.Warning);
        return;
      }

      var suggestedFileName = GetSuggestedPkwFileName();
      SavePkwFileAs(fileText, suggestedFileName);
    }

    /// <summary>
    /// Обрабатывает нажатие кнопки открытия файла в редакторе.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    public void OpenSelectedFileInEditor()
    {
      if (string.IsNullOrWhiteSpace(_lastSelectedReviewFilePath))
      {
        return;
      }

      FileInteractionEventAdapter.RaiseOpenFileInEditorAgain(_lastSelectedReviewFilePath);
    }

    /// <summary>
    /// Обрабатывает нажатие кнопки запуска файла в исполнителе.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    public async Task RunSelectedFileInExecutorAsync()
    {
      if (_lastSelectedIsReviewEntry ||
          string.IsNullOrWhiteSpace(_lastSelectedArchivePath) ||
          string.IsNullOrWhiteSpace(_lastSelectedEntryName))
      {
        return;
      }

      try
      {
        var entryName = NormalizeEntryName(_lastSelectedEntryName);
        var text = await Task.Run(() => ReadArchiveEntryTextWithManager(_lastSelectedArchivePath, entryName));
        if (string.IsNullOrWhiteSpace(text))
        {
          ShowArchiveNotification("Запуск в исполнителе", "Выбранный файл пустой.", NotificationType.Warning);
          return;
        }

        var fileType = DeterminePreviewFileType(entryName);
        var extension = GetExecutionExtension(fileType, entryName);
        var tempFilePath = CreateExecutionTempFilePath(entryName, extension);
        var encoding = (fileType == FileType.PKW || fileType == FileType.OPKW)
          ? new UTF8Encoding(false)
          : Encoding.GetEncoding(866);

        await File.WriteAllTextAsync(tempFilePath, text, encoding);
        FileInteractionEventAdapter.RaiseOpenFileInEditorAgain(tempFilePath);

        await Task.Delay(120);
        if (!TryExecuteRunCommand())
        {
          ShowArchiveNotification(
            "Запуск в исполнителе",
            "Файл открыт, но не удалось автоматически запустить исполнитель. Запустите через меню Выполнение -> Запуск.",
            NotificationType.Warning);
        }
      }
      catch (Exception ex)
      {
        ShowArchiveNotification("Запуск в исполнителе", GetUserFriendlyArchiveErrorMessage(ex), NotificationType.Error);
      }
    }

    /// <summary>
    /// Пытается выполнить команду запуска (RunCommand) из контекста главного окна.
    /// </summary>
    /// <returns>true, если команда успешно выполнена; иначе false.</returns>
    private static bool TryExecuteRunCommand()
    {
      var mainWindow = Application.Current?.MainWindow;
      if (mainWindow?.DataContext == null)
      {
        return false;
      }

      var translationProp = mainWindow.DataContext.GetType().GetProperty("Translation");
      var translationVm = translationProp?.GetValue(mainWindow.DataContext);
      if (translationVm == null)
      {
        return false;
      }

      var runCommandProp = translationVm.GetType().GetProperty("RunCommand");
      var runCommand = runCommandProp?.GetValue(translationVm) as ICommand;
      if (runCommand == null || !runCommand.CanExecute(null))
      {
        return false;
      }

      runCommand.Execute(null);
      return true;
    }

    /// <summary>
    /// Создаёт уникальный временный путь для файла исполнения на основе имени записи архива.
    /// </summary>
    /// <param name="entryName">Имя файла внутри архива.</param>
    /// <param name="extension">Расширение создаваемого файла.</param>
    /// <returns>Полный путь к временному файлу.</returns>
    private static string CreateExecutionTempFilePath(string entryName, string extension)
    {
      var tempRoot = Path.Combine(Path.GetTempPath(), "AskMkiM", "ArchiveExecution");
      Directory.CreateDirectory(tempRoot);

      var safeBase = Path.GetFileNameWithoutExtension(entryName);
      if (string.IsNullOrWhiteSpace(safeBase))
      {
        safeBase = "archive_entry";
      }

      safeBase = string.Join("_", safeBase.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
      if (string.IsNullOrWhiteSpace(safeBase))
      {
        safeBase = "archive_entry";
      }

      return Path.Combine(tempRoot, $"{safeBase}_{Guid.NewGuid():N}{extension}");
    }

    /// <summary>
    /// Получает расширение файла для исполнения на основе его типа.
    /// </summary>
    /// <param name="fileType">Тип файла.</param>
    /// <param name="entryName">Имя записи архива.</param>
    /// <returns>Расширение файла.</returns>
    private static string GetExecutionExtension(FileType fileType, string entryName)
    {
      return fileType switch
      {
        FileType.OPK => ".opk",
        FileType.OPKW => ".opkw",
        FileType.PK => ".pk",
        FileType.PKW => ".pkw",
        _ => string.IsNullOrWhiteSpace(Path.GetExtension(entryName)) ? ".opkw" : Path.GetExtension(entryName),
      };
    }

    /// <summary>
    /// Обрабатывает нажатие кнопки повторной проверки архива.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    public async Task RecheckSelectedReviewArchiveAsync()
    {
      if (string.IsNullOrWhiteSpace(_lastSelectedArchivePath) || !IsReviewArchivePath(_lastSelectedArchivePath))
      {
        return;
      }

      try
      {
        var reviewArchivePath = _lastSelectedArchivePath;
        var reviewEntries = _currentGridEntries
          .Where(entry => entry.IsReviewEntry && IsSameArchivePath(entry.ArchivePath, reviewArchivePath))
          .ToList();

        if (reviewEntries.Count == 0)
        {
          reviewEntries = (await GetReviewEntriesAsync(reviewArchivePath)).ToList();
        }

        if (reviewEntries.Count == 0)
        {
          ShowArchiveNotification("Повторная проверка архива", "В архиве на проверке нет файлов для проверки.", NotificationType.Warning);
          return;
        }

        var owner = Window.GetWindow(this);
        var previousEffect = owner?.Effect;
        ProgressWindow? progressWindow = null;
        var totalErrors = 0;
        var filesWithErrors = 0;
        try
        {
          progressWindow = new ProgressWindow
          {
            Owner = owner,
            WindowStartupLocation = owner == null
              ? WindowStartupLocation.CenterScreen
              : WindowStartupLocation.CenterOwner,
          };

          progressWindow.Configure(
            "Повторная проверка архива",
            "Подготовка проверки",
            $"Готовим повторную проверку {reviewEntries.Count} файлов.");

          if (owner != null)
          {
            owner.Effect = new BlurEffect { Radius = 8 };
          }

          progressWindow.Show();
          await WaitForProgressWindowAsync(progressWindow);

          for (var index = 0; index < reviewEntries.Count; index++)
          {
            var reviewEntry = reviewEntries[index];
            if (string.IsNullOrWhiteSpace(reviewEntry.SourceFilePath) || !File.Exists(reviewEntry.SourceFilePath))
            {
              continue;
            }

            var processed = index + 1;
            var fileName = Path.GetFileName(reviewEntry.SourceFilePath);
            progressWindow.SetProgress(processed * 100d / reviewEntries.Count);
            progressWindow.SetStage(
              $"Проверка файла {processed}/{reviewEntries.Count}",
              fileName);

            MarkReviewPathAsRecentlyMutated(reviewEntry.SourceFilePath);
            MarkReviewPathAsRecentlyMutated(Path.ChangeExtension(reviewEntry.SourceFilePath, ".opkw"));

            var result = await Task.Run(() => RecheckReviewFile(reviewEntry.SourceFilePath));
            UpdateReviewStateInPlace(reviewArchivePath, reviewEntry.EntryName, result.ErrorCount);

            totalErrors += result.ErrorCount;
            if (result.ErrorCount > 0)
            {
              filesWithErrors++;
            }
          }
        }
        finally
        {
          progressWindow?.Close();

          if (owner != null)
          {
            owner.Effect = previousEffect;
          }
        }

        if (_lastSelectedIsReviewEntry && !string.IsNullOrWhiteSpace(_lastSelectedReviewFilePath) && File.Exists(_lastSelectedReviewFilePath))
        {
          var selectedEntry = FindReviewGridEntry(reviewArchivePath, _lastSelectedEntryName);
          var fileType = selectedEntry?.FileType ?? DeterminePreviewFileType(_lastSelectedReviewFilePath);
          var text = await Task.Run(() => ReadReviewFileText(_lastSelectedReviewFilePath, fileType));
          ViewModel.SetPreview(text, fileType);
        }

        ViewModel.SetFilesHint(_currentGridEntries.Count == 0
          ? "Review archive is empty."
          : "Select a review file to preview.");
        ViewModel.SetEditorHint(_lastSelectedIsReviewEntry
          ? "File content is available in read-only mode."
          : "Select a review file to preview.");

        UpdateActionButtons();
        UpdateRightPanels(isFilesVisible: true, isEditorVisible: _lastSelectedIsReviewEntry);
        if (totalErrors > 0)
        {
          ShowArchiveNotification(
            "Повторная проверка архива",
            $"Архив проверен повторно. Найдено ошибок: {CountDisplayFormatter.Format(totalErrors)} в {CountDisplayFormatter.Format(filesWithErrors)} файлах.",
            NotificationType.Warning);
          return;
        }

        ShowArchiveNotification(
          "Повторная проверка архива",
          $"Архив проверен повторно. Ошибок не найдено. Файлов проверено: {reviewEntries.Count}.",
          NotificationType.Success);
      }
      catch (Exception ex)
      {
        ShowArchiveNotification("Повторная проверка архива", GetUserFriendlyArchiveErrorMessage(ex), NotificationType.Error);
      }
    }

    /// <summary>
    /// Ожидает завершения обновления и отрисовки окна прогресса.
    /// </summary>
    /// <param name="progressWindow">Окно прогресса.</param>
    /// <returns>Асинхронная задача ожидания.</returns>
    private static async Task WaitForProgressWindowAsync(ProgressWindow progressWindow)
    {
      await progressWindow.Dispatcher.InvokeAsync(
        progressWindow.UpdateLayout,
        DispatcherPriority.Background);

      await progressWindow.Dispatcher.InvokeAsync(
        progressWindow.UpdateLayout,
        DispatcherPriority.Render);

      await progressWindow.Dispatcher.InvokeAsync(
        () => { },
        DispatcherPriority.ContextIdle);
    }

    public async Task RecheckSelectedReviewFileAsync()
    {
      if (string.IsNullOrWhiteSpace(_lastSelectedReviewFilePath) ||
          string.IsNullOrWhiteSpace(_lastSelectedArchivePath) ||
          string.IsNullOrWhiteSpace(_lastSelectedEntryName))
      {
        return;
      }

      try
      {
        var reviewArchivePath = _lastSelectedArchivePath;
        var entryName = _lastSelectedEntryName;
        var reviewFilePath = _lastSelectedReviewFilePath;
        MarkReviewPathAsRecentlyMutated(reviewFilePath);
        MarkReviewPathAsRecentlyMutated(Path.ChangeExtension(reviewFilePath, ".opkw"));

        var selectedEntry = FindReviewGridEntry(reviewArchivePath, entryName);
        var fileType = selectedEntry?.FileType ?? DeterminePreviewFileType(reviewFilePath);

        var result = await Task.Run(() => RecheckReviewFile(reviewFilePath));
        var text = await Task.Run(() => ReadReviewFileText(reviewFilePath, fileType));

        UpdateReviewStateInPlace(reviewArchivePath, entryName, result.ErrorCount);

        ViewModel.SetPreview(text, fileType);

        _lastSelectedArchivePath = reviewArchivePath;
        _lastSelectedEntryName = NormalizeEntryName(entryName);
        _lastSelectedReviewFilePath = reviewFilePath;
        _lastSelectedIsReviewEntry = true;

        ViewModel.SetFilesHint(_currentGridEntries.Count == 0
          ? "Архив на проверке пуст."
          : "Select a review file to preview.");
        ViewModel.SetEditorHint("Содержимое файла доступно только для чтения.");
        UpdateActionButtons();
        UpdateRightPanels(isFilesVisible: true, isEditorVisible: true);

        if (result.ErrorCount > 0)
        {
          ShowArchiveNotification(
            "Повторная проверка файла",
            $"Файл проверен повторно. Найдено ошибок: {CountDisplayFormatter.Format(result.ErrorCount)}.",
            NotificationType.Warning);
          return;
        }

        ShowArchiveNotification(
          "Повторная проверка файла",
          "Файл проверен повторно. Ошибок не найдено.",
          NotificationType.Success);
      }
      catch (Exception ex)
      {
        ShowArchiveNotification("Повторная проверка файла", GetUserFriendlyArchiveErrorMessage(ex), NotificationType.Error);
      }
    }

    /// <summary>
    /// Возвращает текст текущего содержимого редактора файла.
    /// </summary>
    /// <returns>Текст содержимого или пустая строка.</returns>
    private string GetFileContentText()
    {
      return ViewModel.PreviewText ?? string.Empty;
    }

    /// <summary>
    /// Формирует рекомендуемое имя файла для сохранения в формате PKW.
    /// </summary>
    /// <returns>Имя файла без расширения.</returns>
    private string GetSuggestedPkwFileName()
    {
      var rawName = string.IsNullOrWhiteSpace(_lastSelectedEntryName)
        ? "converted_from_archive"
        : Path.GetFileNameWithoutExtension(_lastSelectedEntryName);

      return string.IsNullOrWhiteSpace(rawName)
        ? "converted_from_archive"
        : rawName;
    }

    private void SavePkwFileAs(string fileText, string suggestedFileName)
    {
      var destinationPath = ArchiveFileDialogService.SelectPkwExportPath(
        this,
        string.IsNullOrWhiteSpace(suggestedFileName) ? "converted_from_archive" : suggestedFileName);
      if (string.IsNullOrWhiteSpace(destinationPath))
      {
        return;
      }

      try
      {
        File.WriteAllText(destinationPath, fileText ?? string.Empty, new UTF8Encoding(false));
        ShowArchiveNotification(
          "PKW export",
          $"File '{Path.GetFileName(destinationPath)}' saved.",
          NotificationType.Success);
      }
      catch (Exception ex)
      {
        ShowArchiveNotification("PKW export", GetUserFriendlyArchiveErrorMessage(ex), NotificationType.Error);
      }
    }
    /// <summary>
    /// Отображает диалоговое окно создания архива.
    /// </summary>
    /// <returns>Имя созданного архива или null, если операция отменена.</returns>
    private string? ShowArchiveCreationDialog()
    {
      var dialog = ArchiveDialogHostService.CreateDialogWindow(this, "???????? ??????");
      var content = new ArchiveNameInputControl();
      content.Initialize(this, "new_archive", isFirstArchive: false);
      dialog.Content = content;

      void TryCreateArchive()
      {
        content.ClearError();

        try
        {
          string createdArchivePath;
          lock (_archiveManagerSync)
          {
            createdArchivePath = _archiveManager.CreateArchive(content.ArchiveName ?? string.Empty);
          }

          dialog.Tag = createdArchivePath;
          dialog.DialogResult = true;
        }
        catch (Exception ex)
        {
          content.ShowError(GetUserFriendlyCreateArchiveErrorMessage(ex));
        }
      }

      content.ConfirmRequested += (_, _) => TryCreateArchive();
      content.TextChanged += (_, _) => content.ClearError();

      return dialog.ShowDialog() == true
        ? dialog.Tag as string
        : null;
    }

    /// <summary>
    /// Обрабатывает изменение выбранного узла дерева архивов и открывает соответствующий архив или файл.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события изменения выбранного элемента.</param>
    private async void ArchivesTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
      var node = e.NewValue as ArchiveTreeNode;
      var previousNode = e.OldValue as ArchiveTreeNode;

      LogInformation(
        $"Archive UI: SelectedItemChanged oldKind='{previousNode?.Kind}', oldDisplay='{previousNode?.DisplayName}', newKind='{node?.Kind}', newDisplay='{node?.DisplayName}'.");

      if (node == null)
      {
        return;
      }

      try
      {
        if (node.Kind == ArchiveTreeNodeKind.Root || node.Kind == ArchiveTreeNodeKind.ReviewRoot)
        {
          lock (_archiveManagerSync)
          {
            _archiveManager.CloseArchive();
          }

          _lastSelectedArchivePath = null;
          _lastSelectedEntryName = null;
          _lastSelectedReviewFilePath = null;
          _lastSelectedIsReviewEntry = false;
          ClearFilePanels();
          return;
        }

        if ((node.Kind == ArchiveTreeNodeKind.Archive || node.Kind == ArchiveTreeNodeKind.ReviewArchive) &&
            node.ArchivePath != null)
        {
          await OpenArchiveAsync(node.ArchivePath);
          return;
        }

        if ((node.Kind == ArchiveTreeNodeKind.File || node.Kind == ArchiveTreeNodeKind.ReviewFile) &&
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

    /// <summary>
    /// Обрабатывает изменение выбранного элемента в таблице файлов архива.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события изменения выбора.</param>
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
        _lastSelectedReviewFilePath = null;
        _lastSelectedIsReviewEntry = false;
        UpdateActionButtons();
        return;
      }

      try
      {
        _lastSelectedArchivePath = selected.ArchivePath;
        _lastSelectedEntryName = selected.EntryName;
        _lastSelectedReviewFilePath = selected.SourceFilePath;
        _lastSelectedIsReviewEntry = selected.IsReviewEntry;

        string text;
        FileType fileType;
        if (selected.IsReviewEntry && !string.IsNullOrWhiteSpace(selected.SourceFilePath))
        {
          text = await Task.Run(() => ReadReviewFileText(selected.SourceFilePath, selected.FileType));
          fileType = selected.FileType;
        }
        else
        {
          text = await Task.Run(() => ReadArchiveEntryTextWithManager(selected.ArchivePath, selected.EntryName));
          fileType = FileType.OPKW;
        }

        ViewModel.SetPreview(text, fileType);

        ViewModel.SetEditorHint("Содержимое файла доступно только для чтения.");
        UpdateActionButtons();
        UpdateRightPanels(isFilesVisible: true, isEditorVisible: true);
      }
      catch (Exception ex)
      {
        ShowArchiveNotification("Архивы", GetUserFriendlyArchiveErrorMessage(ex), NotificationType.Error);
      }
    }

    /// <summary>
    /// Создаёт модальное диалоговое окно с заданным заголовком и преднастроенными параметрами отображения.
    /// </summary>
    /// <param name="title">Заголовок окна.</param>
    /// <returns>Экземпляр окна.</returns>


    /// <summary>
    /// Отображает уведомление об архиве.
    /// </summary>
    /// <param name="title">Заголовок уведомления.</param>
    /// <param name="message">Сообщение уведомления.</param>
    /// <param name="notificationType">Тип уведомления.</param>
    private void ShowArchiveNotification(string title, string message, NotificationType notificationType)
    {
      NotificationHostService.Instance.Show(title, message, notificationType);
    }

    /// <summary>
    /// Получает пользовательское сообщение об ошибке при создании архива.
    /// </summary>
    /// <param name="ex">Исключение.</param>
    /// <returns>Пользовательское сообщение об ошибке.</returns>
    private static string GetUserFriendlyCreateArchiveErrorMessage(Exception ex)
    {
      if (ex is InvalidOperationException invalidOperation &&
          (invalidOperation.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase) ||
           invalidOperation.Message.Contains("уже существует", StringComparison.OrdinalIgnoreCase)))
      {
        return "Архив с таким именем уже существует. Выберите другое имя.";
      }

      if (ex is ArgumentException argumentException)
      {
        if (!string.IsNullOrWhiteSpace(argumentException.Message) &&
            argumentException.Message.Contains("Требуется указать имя архива", StringComparison.OrdinalIgnoreCase))
        {
          return "Введите имя архива.";
        }

        if (!string.IsNullOrWhiteSpace(argumentException.Message) &&
            argumentException.Message.Contains("только недопустимые символы", StringComparison.OrdinalIgnoreCase))
        {
          return "Имя архива содержит только недопустимые символы.";
        }

        return "Имя архива содержит недопустимые символы.";
      }

      if (ex is DirectoryNotFoundException directoryNotFoundException)
      {
        return directoryNotFoundException.Message;
      }

      if (ex is UnauthorizedAccessException)
      {
        return "Нет доступа к папке архивов.";
      }

      if (ex is IOException ioException &&
          (ioException.Message.Contains("being used by another process", StringComparison.OrdinalIgnoreCase) ||
           ioException.Message.Contains("доступ к файлу", StringComparison.OrdinalIgnoreCase)))
      {
        return "Архив сейчас используется другим процессом. Повторите попытку.";
      }

      return "Не удалось создать архив.";
    }

    /// <summary>
    /// Преобразует исключение в понятное пользователю сообщение об ошибке при работе с архивами.
    /// </summary>
    /// <param name="ex">Исключение.</param>
    /// <returns>Текст ошибки для отображения пользователю.</returns>
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

    /// <summary>
    /// Определяет тип операции буфера обмена для файлов архива.
    /// </summary>
    private enum ArchiveClipboardOperation
    {
      Copy,
      Cut
    }

    /// <summary>
    /// Представляет элемент буфера обмена архива (источник, имя записи и операция).
    /// </summary>
    private sealed class ArchiveClipboardEntry
    {
      /// <summary>
      /// Инициализирует новый экземпляр класса <see cref="ArchiveClipboardEntry"/>.
      /// </summary>
      /// <param name="sourceArchivePath">Путь к исходному архиву.</param>
      /// <param name="entryName">Имя записи в архиве.</param>
      /// <param name="displayName">Отображаемое имя записи.</param>
      /// <param name="operation">Операция буфера обмена.</param>
      public ArchiveClipboardEntry(string sourceArchivePath, string entryName, string displayName, ArchiveClipboardOperation operation)
      {
        SourceArchivePath = sourceArchivePath;
        EntryName = entryName;
        DisplayName = displayName;
        Operation = operation;
      }

      /// <summary>
      /// Путь к исходному архиву.
      /// </summary>
      public string SourceArchivePath { get; }

      /// <summary>
      /// Имя записи внутри архива.
      /// </summary>
      public string EntryName { get; }

      /// <summary>
      /// Отображаемое имя файла.
      /// </summary>
      public string DisplayName { get; }

      /// <summary>
      /// Тип операции буфера обмена (копирование или вырезание).
      /// </summary>
      public ArchiveClipboardOperation Operation { get; }
    }

    /// <summary>
    /// Содержит результат повторной проверки файла на ошибки.
    /// </summary>
    private sealed class RecheckReviewFileResult
    {
      /// <summary>
      /// Путь к файлу, прошедшему повторную проверку.
      /// </summary>
      public string FilePath { get; init; } = string.Empty;

      /// <summary>
      /// Количество найденных ошибок.
      /// </summary>
      public int ErrorCount { get; init; }
    }

    /// <summary>
    /// Хранит состояние дерева архивов (раскрытые корневые узлы и архивы).
    /// </summary>
    private sealed class TreeRefreshState
    {
      /// <summary>
      /// Признак того, что корневой узел архивов раскрыт.
      /// </summary>
      public bool IsArchiveRootExpanded { get; set; }

      /// <summary>
      /// Признак того, что корневой узел архивов на проверке раскрыт.
      /// </summary>
      public bool IsReviewRootExpanded { get; set; }

      /// <summary>
      /// Набор путей раскрытых архивов.
      /// </summary>
      public HashSet<string> ExpandedArchivePaths { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

      /// <summary>
      /// Набор путей раскрытых архивов на проверке.
      /// </summary>
      public HashSet<string> ExpandedReviewArchivePaths { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
  }
}
