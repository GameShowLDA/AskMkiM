using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.UI.Features.Archive.Contracts;
using Ask.UI.Shared.Commands;
using Ask.UI.Shared.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Ask.UI.Features.Archive.ViewModels
{
  public sealed class ArchiveViewModel : ObservableObject
  {
    #region Private поля.
    private IArchiveViewActions? _actions;
    private IReadOnlyList<ArchiveEntryInfo> _fileEntries = Array.Empty<ArchiveEntryInfo>();
    private ArchiveEntryInfo? _selectedFileEntry;
    private string _filesHint = "Выберите архив для просмотра файлов.";
    private string _editorHint = "Выберите файл в архиве для просмотра.";
    private string _selectedArchiveName = string.Empty;
    private string _selectedArchiveFileName = string.Empty;
    private string _previewText = string.Empty;
    private FileType _previewFileType = FileType.OPKW;
    private Visibility _archiveFilesGridVisibility = Visibility.Collapsed;
    private Visibility _editorPanelVisibility = Visibility.Collapsed;
    private Visibility _rightSplitterVisibility = Visibility.Collapsed;
    private Visibility _archiveActionsVisibility = Visibility.Collapsed;
    private Visibility _recheckReviewArchiveVisibility = Visibility.Collapsed;
    private Visibility _saveArchiveVisibility = Visibility.Collapsed;
    private Visibility _convertArchiveToApkVisibility = Visibility.Collapsed;
    private Visibility _printArchiveCatalogVisibility = Visibility.Collapsed;
    private Visibility _addFileToArchiveVisibility = Visibility.Collapsed;
    private Visibility _pasteIntoArchiveVisibility = Visibility.Collapsed;
    private Visibility _deleteArchiveVisibility = Visibility.Collapsed;
    private Visibility _deleteArchiveFileVisibility = Visibility.Collapsed;
    private Visibility _copyArchiveFileVisibility = Visibility.Collapsed;
    private Visibility _cutArchiveFileVisibility = Visibility.Collapsed;
    private Visibility _pasteArchiveFileVisibility = Visibility.Collapsed;
    private Visibility _convertToPkwVisibility = Visibility.Collapsed;
    private Visibility _runInExecutorVisibility = Visibility.Collapsed;
    private Visibility _recheckReviewFileVisibility = Visibility.Collapsed;
    private Visibility _openInEditorVisibility = Visibility.Collapsed;
    private Visibility _selectedArchiveNameVisibility = Visibility.Collapsed;
    private Visibility _selectedArchiveFileNameVisibility = Visibility.Collapsed;
    #endregion

    #region Св-ва.

    /// <summary>
    /// Коллекция узлов дерева архивов.
    /// </summary>
    public ObservableCollection<ArchiveTreeNode> TreeNodes { get; } = new();

    /// <summary>
    /// ViewModel контекстного меню архивов.
    /// </summary>
    public ArchiveContextMenuViewModel ContextMenu { get; } = new();

    /// <summary>
    /// Коллекция файлов текущего архива.
    /// </summary>
    public IReadOnlyList<ArchiveEntryInfo> FileEntries
    {
      get => _fileEntries;
      private set => SetProperty(ref _fileEntries, value);
    }

    /// <summary>
    /// Текущий выбранный файл архива.
    /// </summary>
    public ArchiveEntryInfo? SelectedFileEntry
    {
      get => _selectedFileEntry;
      set => SetProperty(ref _selectedFileEntry, value);
    }

    /// <summary>
    /// Подсказка для области списка файлов.
    /// </summary>
    public string FilesHint { get => _filesHint; private set => SetProperty(ref _filesHint, value); }

    /// <summary>
    /// Подсказка для области редактора.
    /// </summary>
    public string EditorHint { get => _editorHint; private set => SetProperty(ref _editorHint, value); }

    /// <summary>
    /// Имя выбранного архива.
    /// </summary>
    public string SelectedArchiveName { get => _selectedArchiveName; private set => SetProperty(ref _selectedArchiveName, value); }

    /// <summary>
    /// Имя выбранного файла архива.
    /// </summary>
    public string SelectedArchiveFileName { get => _selectedArchiveFileName; private set => SetProperty(ref _selectedArchiveFileName, value); }

    /// <summary>
    /// Текст предварительного просмотра файла.
    /// </summary>
    public string PreviewText { get => _previewText; private set => SetProperty(ref _previewText, value); }

    /// <summary>
    /// Тип файла предварительного просмотра.
    /// </summary>
    public FileType PreviewFileType { get => _previewFileType; private set => SetProperty(ref _previewFileType, value); } 

    #endregion

    #region Visibility.

    /// <summary>
    /// Видимость таблицы файлов архива.
    /// </summary>
    public Visibility ArchiveFilesGridVisibility { get => _archiveFilesGridVisibility; private set => SetProperty(ref _archiveFilesGridVisibility, value); }

    /// <summary>
    /// Видимость панели редактора.
    /// </summary>
    public Visibility EditorPanelVisibility { get => _editorPanelVisibility; private set => SetProperty(ref _editorPanelVisibility, value); }

    /// <summary>
    /// Видимость разделителя правой панели.
    /// </summary>
    public Visibility RightSplitterVisibility { get => _rightSplitterVisibility; private set => SetProperty(ref _rightSplitterVisibility, value); }

    /// <summary>
    /// Видимость панели действий с архивом.
    /// </summary>
    public Visibility ArchiveActionsVisibility { get => _archiveActionsVisibility; private set => SetProperty(ref _archiveActionsVisibility, value); }

    /// <summary>
    /// Видимость команды повторной проверки архива.
    /// </summary>
    public Visibility RecheckReviewArchiveVisibility { get => _recheckReviewArchiveVisibility; private set => SetProperty(ref _recheckReviewArchiveVisibility, value); }

    /// <summary>
    /// Видимость команды сохранения архива.
    /// </summary>
    public Visibility SaveArchiveVisibility { get => _saveArchiveVisibility; private set => SetProperty(ref _saveArchiveVisibility, value); }

    /// <summary>
    /// Видимость команды конвертации выбранного архива в legacy APK.
    /// </summary>
    public Visibility ConvertArchiveToApkVisibility { get => _convertArchiveToApkVisibility; private set => SetProperty(ref _convertArchiveToApkVisibility, value); }

    /// <summary>
    /// Видимость команды печати каталога архива.
    /// </summary>
    public Visibility PrintArchiveCatalogVisibility { get => _printArchiveCatalogVisibility; private set => SetProperty(ref _printArchiveCatalogVisibility, value); }

    /// <summary>
    /// Видимость команды добавления файла в архив.
    /// </summary>
    public Visibility AddFileToArchiveVisibility { get => _addFileToArchiveVisibility; private set => SetProperty(ref _addFileToArchiveVisibility, value); }

    /// <summary>
    /// Видимость команды вставки файла в архив.
    /// </summary>
    public Visibility PasteIntoArchiveVisibility { get => _pasteIntoArchiveVisibility; private set => SetProperty(ref _pasteIntoArchiveVisibility, value); }

    /// <summary>
    /// Видимость команды удаления архива.
    /// </summary>
    public Visibility DeleteArchiveVisibility { get => _deleteArchiveVisibility; private set => SetProperty(ref _deleteArchiveVisibility, value); }

    /// <summary>
    /// Видимость команды удаления файла архива.
    /// </summary>
    public Visibility DeleteArchiveFileVisibility { get => _deleteArchiveFileVisibility; private set => SetProperty(ref _deleteArchiveFileVisibility, value); }

    /// <summary>
    /// Видимость команды копирования файла архива.
    /// </summary>
    public Visibility CopyArchiveFileVisibility { get => _copyArchiveFileVisibility; private set => SetProperty(ref _copyArchiveFileVisibility, value); }

    /// <summary>
    /// Видимость команды вырезания файла архива.
    /// </summary>
    public Visibility CutArchiveFileVisibility { get => _cutArchiveFileVisibility; private set => SetProperty(ref _cutArchiveFileVisibility, value); }

    /// <summary>
    /// Видимость команды вставки файла архива.
    /// </summary>
    public Visibility PasteArchiveFileVisibility { get => _pasteArchiveFileVisibility; private set => SetProperty(ref _pasteArchiveFileVisibility, value); }

    /// <summary>
    /// Видимость команды конвертации в PKW.
    /// </summary>
    public Visibility ConvertToPkwVisibility { get => _convertToPkwVisibility; private set => SetProperty(ref _convertToPkwVisibility, value); }

    /// <summary>
    /// Видимость команды запуска в исполнителе.
    /// </summary>
    public Visibility RunInExecutorVisibility { get => _runInExecutorVisibility; private set => SetProperty(ref _runInExecutorVisibility, value); }

    /// <summary>
    /// Видимость команды повторной проверки файла.
    /// </summary>
    public Visibility RecheckReviewFileVisibility { get => _recheckReviewFileVisibility; private set => SetProperty(ref _recheckReviewFileVisibility, value); }

    /// <summary>
    /// Видимость команды открытия файла в редакторе.
    /// </summary>
    public Visibility OpenInEditorVisibility { get => _openInEditorVisibility; private set => SetProperty(ref _openInEditorVisibility, value); }

    /// <summary>
    /// Видимость отображения имени выбранного архива.
    /// </summary>
    public Visibility SelectedArchiveNameVisibility { get => _selectedArchiveNameVisibility; private set => SetProperty(ref _selectedArchiveNameVisibility, value); }

    /// <summary>
    /// Видимость отображения имени выбранного файла архива.
    /// </summary>
    public Visibility SelectedArchiveFileNameVisibility { get => _selectedArchiveFileNameVisibility; private set => SetProperty(ref _selectedArchiveFileNameVisibility, value); }

    #endregion

    #region ICommand.

    /// <summary> Команда создания архива. </summary>
    public ICommand CreateArchiveCommand { get; }

    /// <summary> Команда загрузки архива. </summary>
    public ICommand UploadArchiveCommand { get; }

    /// <summary> Команда скачивания архивов. </summary>
    public ICommand DownloadArchivesCommand { get; }

    /// <summary> Команда открытия архива из контекстного меню. </summary>
    public ICommand OpenContextArchiveCommand { get; }

    /// <summary> Команда сохранения архива из контекстного меню. </summary>
    public ICommand SaveContextArchiveCommand { get; }

    /// <summary> Команда печати каталога архива из контекстного меню. </summary>
    public ICommand PrintContextArchiveCatalogCommand { get; }

    /// <summary> Команда добавления файла в архив из контекстного меню. </summary>
    public ICommand AddFileToContextArchiveCommand { get; }

    /// <summary> Команда удаления архива из контекстного меню. </summary>
    public ICommand DeleteContextArchiveCommand { get; }

    /// <summary> Команда открытия файла из контекстного меню. </summary>
    public ICommand OpenContextFileCommand { get; }

    /// <summary> Команда открытия файла в редакторе из контекстного меню. </summary>
    public ICommand OpenContextFileInEditorCommand { get; }

    /// <summary> Команда копирования файла из контекстного меню. </summary>
    public ICommand CopyContextFileCommand { get; }

    /// <summary> Команда вырезания файла из контекстного меню. </summary>
    public ICommand CutContextFileCommand { get; }

    /// <summary> Команда вставки файла из контекстного меню. </summary>
    public ICommand PasteContextFileCommand { get; }

    /// <summary> Команда удаления файла из контекстного меню. </summary>
    public ICommand DeleteContextFileCommand { get; }

    /// <summary> Команда повторной проверки выбранного архива. </summary>
    public ICommand RecheckSelectedReviewArchiveCommand { get; }

    /// <summary> Команда сохранения выбранного архива. </summary>
    public ICommand SaveSelectedArchiveCommand { get; }

    /// <summary> Команда конвертации выбранного архива в legacy APK. </summary>
    public ICommand ConvertSelectedArchiveToApkCommand { get; }

    /// <summary> Команда добавления файла в выбранный архив. </summary>
    public ICommand AddFileToSelectedArchiveCommand { get; }

    /// <summary> Команда удаления выбранного архива. </summary>
    public ICommand DeleteSelectedArchiveCommand { get; }

    /// <summary> Команда вставки файла в выбранный архив. </summary>
    public ICommand PasteIntoSelectedArchiveCommand { get; }

    /// <summary> Команда удаления выбранного файла. </summary>
    public ICommand DeleteSelectedFileCommand { get; }

    /// <summary> Команда копирования выбранного файла. </summary>
    public ICommand CopySelectedFileCommand { get; }

    /// <summary> Команда вырезания выбранного файла. </summary>
    public ICommand CutSelectedFileCommand { get; }

    /// <summary> Команда вставки файла в архив выбранного файла. </summary>
    public ICommand PasteIntoSelectedFileArchiveCommand { get; }

    /// <summary> Команда конвертации выбранного файла в PKW. </summary>
    public ICommand ConvertSelectedFileToPkwCommand { get; }

    /// <summary> Команда открытия выбранного файла в редакторе. </summary>
    public ICommand OpenSelectedFileInEditorCommand { get; }

    /// <summary> Команда запуска выбранного файла в исполнителе. </summary>
    public ICommand RunSelectedFileInExecutorCommand { get; }

    /// <summary> Команда повторной проверки выбранного файла. </summary>
    public ICommand RecheckSelectedReviewFileCommand { get; }
    #endregion

    /// <summary>
    /// Инициализирует ViewModel управления архивами.
    /// </summary>
    public ArchiveViewModel()
    {
      CreateArchiveCommand = new RelayCommand(() => _actions?.CreateArchive());
      UploadArchiveCommand = new RelayCommand(() => _actions?.UploadArchive());
      DownloadArchivesCommand = new RelayCommand(() => _actions?.DownloadArchives());
      OpenContextArchiveCommand = new AsyncRelayCommand(() => _actions?.OpenContextArchiveAsync() ?? Task.CompletedTask);
      SaveContextArchiveCommand = new RelayCommand(() => _actions?.SaveContextArchive());
      PrintContextArchiveCatalogCommand = new AsyncRelayCommand(() => _actions?.PrintContextArchiveCatalogAsync() ?? Task.CompletedTask);
      AddFileToContextArchiveCommand = new RelayCommand(() => _actions?.AddFileToContextArchive());
      DeleteContextArchiveCommand = new AsyncRelayCommand(() => _actions?.DeleteContextArchiveAsync() ?? Task.CompletedTask);
      OpenContextFileCommand = new AsyncRelayCommand(() => _actions?.OpenContextFileAsync() ?? Task.CompletedTask);
      OpenContextFileInEditorCommand = new RelayCommand(() => _actions?.OpenContextFileInEditor());
      CopyContextFileCommand = new RelayCommand(() => _actions?.CopyContextFile());
      CutContextFileCommand = new RelayCommand(() => _actions?.CutContextFile());
      PasteContextFileCommand = new AsyncRelayCommand(() => _actions?.PasteContextFileAsync() ?? Task.CompletedTask);
      DeleteContextFileCommand = new AsyncRelayCommand(() => _actions?.DeleteContextFileAsync() ?? Task.CompletedTask);
      RecheckSelectedReviewArchiveCommand = new AsyncRelayCommand(() => _actions?.RecheckSelectedReviewArchiveAsync() ?? Task.CompletedTask);
      SaveSelectedArchiveCommand = new RelayCommand(() => _actions?.SaveSelectedArchiveToDisk());
      ConvertSelectedArchiveToApkCommand = new AsyncRelayCommand(() => _actions?.ConvertSelectedArchiveToApkAsync() ?? Task.CompletedTask);
      AddFileToSelectedArchiveCommand = new RelayCommand(() => _actions?.AddFileToSelectedArchive());
      DeleteSelectedArchiveCommand = new AsyncRelayCommand(() => _actions?.DeleteSelectedArchiveAsync() ?? Task.CompletedTask);
      PasteIntoSelectedArchiveCommand = new AsyncRelayCommand(() => _actions?.PasteIntoSelectedArchiveAsync() ?? Task.CompletedTask);
      DeleteSelectedFileCommand = new AsyncRelayCommand(() => _actions?.DeleteSelectedFileAsync() ?? Task.CompletedTask);
      CopySelectedFileCommand = new RelayCommand(() => _actions?.CopySelectedFile());
      CutSelectedFileCommand = new RelayCommand(() => _actions?.CutSelectedFile());
      PasteIntoSelectedFileArchiveCommand = new AsyncRelayCommand(() => _actions?.PasteIntoSelectedFileArchiveAsync() ?? Task.CompletedTask);
      ConvertSelectedFileToPkwCommand = new RelayCommand(() => _actions?.ConvertSelectedFileToPkw());
      OpenSelectedFileInEditorCommand = new RelayCommand(() => _actions?.OpenSelectedFileInEditor());
      RunSelectedFileInExecutorCommand = new AsyncRelayCommand(() => _actions?.RunSelectedFileInExecutorAsync() ?? Task.CompletedTask);
      RecheckSelectedReviewFileCommand = new AsyncRelayCommand(() => _actions?.RecheckSelectedReviewFileAsync() ?? Task.CompletedTask);
    }

    /// <summary>
    /// Подключает обработчики UI-действий архива.
    /// </summary>
    /// <param name="actions">Реализация действий представления архива.</param>
    public void AttachActions(IArchiveViewActions actions) => _actions = actions;

    /// <summary>
    /// Устанавливает корневые узлы дерева архивов.
    /// </summary>
    /// <param name="nodes">Коллекция корневых узлов.</param>
    public void SetTreeRoots(IEnumerable<ArchiveTreeNode> nodes)
    {
      TreeNodes.Clear();
      foreach (var node in nodes)
      {
        TreeNodes.Add(node);
      }
    }

    /// <summary>
    /// Устанавливает список файлов архива.
    /// </summary>
    /// <param name="entries">Коллекция файлов архива.</param>
    public void SetFileEntries(IReadOnlyList<ArchiveEntryInfo> entries) => FileEntries = entries;

    /// <summary>
    /// Устанавливает текст подсказки для области файлов.
    /// </summary>
    /// <param name="value">Текст подсказки.</param>
    public void SetFilesHint(string value) => FilesHint = value;

    /// <summary>
    /// Устанавливает текст подсказки для области редактора.
    /// </summary>
    /// <param name="value">Текст подсказки.</param>
    public void SetEditorHint(string value) => EditorHint = value;

    /// <summary>
    /// Устанавливает содержимое предварительного просмотра файла.
    /// </summary>
    /// <param name="text">Текст предварительного просмотра.</param>
    /// <param name="fileType">Тип файла.</param>
    public void SetPreview(string text, FileType fileType) { PreviewText = text; PreviewFileType = fileType; }

    /// <summary>
    /// Очищает область предварительного просмотра.
    /// </summary>
    public void ClearPreview() { PreviewText = string.Empty; PreviewFileType = FileType.OPKW; }

    /// <summary>
    /// Проверяет наличие содержимого предварительного просмотра.
    /// </summary>
    /// <returns>
    /// true, если предварительный просмотр содержит текст;
    /// иначе false.
    /// </returns>
    public bool HasPreviewContent() => !string.IsNullOrWhiteSpace(PreviewText);

    /// <summary>
    /// Обновляет заголовки панелей выбранного архива и файла.
    /// </summary>
    /// <param name="archivePath">Путь к выбранному архиву.</param>
    /// <param name="entryName">Имя выбранного файла архива.</param>
    public void UpdatePanelTitles(string? archivePath, string? entryName)
    {
      SelectedArchiveName = string.IsNullOrWhiteSpace(archivePath) ? string.Empty : Path.GetFileName(archivePath);
      SelectedArchiveFileName = string.IsNullOrWhiteSpace(entryName) ? string.Empty : Path.GetFileName(entryName);
      SelectedArchiveNameVisibility = ToVisibility(!string.IsNullOrWhiteSpace(SelectedArchiveName));
      SelectedArchiveFileNameVisibility = ToVisibility(!string.IsNullOrWhiteSpace(SelectedArchiveFileName));
    }

    /// <summary>
    /// Обновляет состояние правых панелей интерфейса.
    /// </summary>
    /// <param name="isEditorVisible">
    /// Признак отображения панели редактора.
    /// </param>
    public void UpdateRightPanels(bool isEditorVisible)
    {
      ArchiveFilesGridVisibility = Visibility.Visible;
      EditorPanelVisibility = ToVisibility(isEditorVisible);
      RightSplitterVisibility = ToVisibility(isEditorVisible);
    }

    /// <summary>
    /// Обновляет состояние кнопок и действий интерфейса архива.
    /// </summary>
    /// <param name="archivePath">Путь к выбранному архиву.</param>
    /// <param name="entryName">Имя выбранного файла архива.</param>
    /// <param name="reviewFilePath">Путь к файлу проверки.</param>
    /// <param name="isReviewEntry">
    /// Признак того, что выбран файл проверки.
    /// </param>
    /// <param name="hasClipboardEntry">
    /// Признак наличия файла в буфере обмена архивов.
    /// </param>
    /// <param name="isReviewArchivePath">
    /// Делегат проверки принадлежности архива к архивам проверки.
    /// </param>
    public void UpdateActionButtons(string? archivePath, string? entryName, string? reviewFilePath, bool isReviewEntry, bool hasClipboardEntry, Func<string, bool> isReviewArchivePath)
    {
      UpdatePanelTitles(archivePath, entryName);

      var hasArchive = !string.IsNullOrWhiteSpace(archivePath) && (File.Exists(archivePath) || isReviewArchivePath(archivePath));
      var isReviewArchive = hasArchive && isReviewArchivePath(archivePath!);
      var hasSelectedFile = !string.IsNullOrWhiteSpace(archivePath) && !string.IsNullOrWhiteSpace(entryName);
      var canManageArchiveFiles = hasSelectedFile && !isReviewEntry;
      var canDeleteReviewFile = hasSelectedFile && isReviewEntry && !string.IsNullOrWhiteSpace(reviewFilePath);

      ArchiveActionsVisibility = ToVisibility(hasArchive);
      RecheckReviewArchiveVisibility = ToVisibility(isReviewArchive);
      SaveArchiveVisibility = ToVisibility(hasArchive && !isReviewArchive);
      ConvertArchiveToApkVisibility = ToVisibility(hasArchive && !isReviewArchive);
      PrintArchiveCatalogVisibility = ToVisibility(hasArchive && !isReviewArchive);
      AddFileToArchiveVisibility = ToVisibility(hasArchive && !isReviewArchive);
      PasteIntoArchiveVisibility = ToVisibility(hasArchive && hasClipboardEntry);
      DeleteArchiveVisibility = ToVisibility(hasArchive);
      DeleteArchiveFileVisibility = ToVisibility(canManageArchiveFiles || canDeleteReviewFile);
      CopyArchiveFileVisibility = ToVisibility(canManageArchiveFiles);
      CutArchiveFileVisibility = ToVisibility(canManageArchiveFiles);
      PasteArchiveFileVisibility = ToVisibility(canManageArchiveFiles && hasClipboardEntry);
      ConvertToPkwVisibility = ToVisibility(!isReviewEntry);
      RunInExecutorVisibility = ToVisibility(canManageArchiveFiles);
      RecheckReviewFileVisibility = ToVisibility(isReviewEntry && !string.IsNullOrWhiteSpace(reviewFilePath));
      OpenInEditorVisibility = ToVisibility(isReviewEntry && !string.IsNullOrWhiteSpace(reviewFilePath));
    }

    /// <summary>
    /// Обновляет состояние контекстного меню дерева архивов.
    /// </summary>
    /// <param name="node">Выбранный узел дерева архивов.</param>
    /// <param name="hasClipboardEntry">
    /// Признак наличия файла в буфере обмена архивов.
    /// </param>
    public void UpdateContextMenu(ArchiveTreeNode? node, bool hasClipboardEntry)
    {
      ContextMenu.Update(node, hasClipboardEntry);
    }

    /// <summary>
    /// Преобразует логическое значение в значение видимости.
    /// </summary>
    /// <param name="visible">Признак отображения элемента.</param>
    /// <returns>
    /// Visibility.Visible, если значение true;
    /// иначе Visibility.Collapsed.
    /// </returns>
    private static Visibility ToVisibility(bool visible) => visible ? Visibility.Visible : Visibility.Collapsed;
  }
}
