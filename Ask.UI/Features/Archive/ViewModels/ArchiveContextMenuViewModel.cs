using Ask.UI.Shared.ViewModels;
using System.Windows;

namespace Ask.UI.Features.Archive.ViewModels
{
  /// <summary>
  /// ViewModel контекстного меню для операций с архивами.
  /// </summary>
  public sealed class ArchiveContextMenuViewModel : ObservableObject
  {
    private Visibility _createArchiveVisibility = Visibility.Collapsed;
    private Visibility _uploadArchiveVisibility = Visibility.Collapsed;
    private Visibility _downloadArchivesVisibility = Visibility.Collapsed;
    private Visibility _openArchiveVisibility = Visibility.Collapsed;
    private Visibility _saveArchiveVisibility = Visibility.Collapsed;
    private Visibility _printArchiveCatalogVisibility = Visibility.Collapsed;
    private Visibility _deleteArchiveVisibility = Visibility.Collapsed;
    private Visibility _addFileToArchiveVisibility = Visibility.Collapsed;
    private Visibility _openArchiveFileVisibility = Visibility.Collapsed;
    private Visibility _openInTextEditorVisibility = Visibility.Collapsed;
    private Visibility _copyArchiveFileVisibility = Visibility.Collapsed;
    private Visibility _cutArchiveFileVisibility = Visibility.Collapsed;
    private Visibility _pasteArchiveFileVisibility = Visibility.Collapsed;
    private Visibility _deleteArchiveFileVisibility = Visibility.Collapsed;

    /// <summary> Видимость команды создания архива. </summary>
    public Visibility CreateArchiveVisibility { get => _createArchiveVisibility; private set => SetProperty(ref _createArchiveVisibility, value); }

    /// <summary> Видимость команды загрузки архива. </summary>
    public Visibility UploadArchiveVisibility { get => _uploadArchiveVisibility; private set => SetProperty(ref _uploadArchiveVisibility, value); }

    /// <summary> Видимость команды скачивания архивов. </summary>
    public Visibility DownloadArchivesVisibility { get => _downloadArchivesVisibility; private set => SetProperty(ref _downloadArchivesVisibility, value); }

    /// <summary> Видимость команды открытия архива. </summary>
    public Visibility OpenArchiveVisibility { get => _openArchiveVisibility; private set => SetProperty(ref _openArchiveVisibility, value); }

    /// <summary> Видимость команды сохранения архива. </summary>
    public Visibility SaveArchiveVisibility { get => _saveArchiveVisibility; private set => SetProperty(ref _saveArchiveVisibility, value); }

    /// <summary> Видимость команды печати каталога архива. </summary>
    public Visibility PrintArchiveCatalogVisibility { get => _printArchiveCatalogVisibility; private set => SetProperty(ref _printArchiveCatalogVisibility, value); }

    /// <summary> Видимость команды удаления архива. </summary>
    public Visibility DeleteArchiveVisibility { get => _deleteArchiveVisibility; private set => SetProperty(ref _deleteArchiveVisibility, value); }

    /// <summary> Видимость команды добавления файла в архив. </summary>
    public Visibility AddFileToArchiveVisibility { get => _addFileToArchiveVisibility; private set => SetProperty(ref _addFileToArchiveVisibility, value); }

    /// <summary> Видимость команды открытия файла архива. </summary>
    public Visibility OpenArchiveFileVisibility { get => _openArchiveFileVisibility; private set => SetProperty(ref _openArchiveFileVisibility, value); }

    /// <summary> Видимость команды открытия файла в текстовом редакторе. </summary>
    public Visibility OpenInTextEditorVisibility { get => _openInTextEditorVisibility; private set => SetProperty(ref _openInTextEditorVisibility, value); }

    /// <summary> Видимость команды копирования файла архива. </summary>
    public Visibility CopyArchiveFileVisibility { get => _copyArchiveFileVisibility; private set => SetProperty(ref _copyArchiveFileVisibility, value); }

    /// <summary> Видимость команды вырезания файла архива. </summary>
    public Visibility CutArchiveFileVisibility { get => _cutArchiveFileVisibility; private set => SetProperty(ref _cutArchiveFileVisibility, value); }

    /// <summary> Видимость команды вставки файла в архив. </summary>
    public Visibility PasteArchiveFileVisibility { get => _pasteArchiveFileVisibility; private set => SetProperty(ref _pasteArchiveFileVisibility, value); }

    /// <summary> Видимость команды удаления файла архива. </summary>
    public Visibility DeleteArchiveFileVisibility { get => _deleteArchiveFileVisibility; private set => SetProperty(ref _deleteArchiveFileVisibility, value); }

    /// <summary>
    /// Признак наличия доступных элементов контекстного меню.
    /// </summary>
    public bool HasAvailableItems { get; private set; }

    /// <summary>
    /// Обновляет состояние и видимость элементов контекстного меню.
    /// </summary>
    /// <param name="node">Выбранный узел дерева архивов.</param>
    /// <param name="hasClipboardEntry">
    /// Признак наличия файла в буфере обмена архивов.
    /// </param>
    public void Update(ArchiveTreeNode? node, bool hasClipboardEntry)
    {
      var isRoot = node?.Kind == ArchiveTreeNodeKind.Root;
      var isArchive = node?.Kind == ArchiveTreeNodeKind.Archive;
      var isFile = node?.Kind == ArchiveTreeNodeKind.File;
      var isReviewFile = node?.Kind == ArchiveTreeNodeKind.ReviewFile;
      var isReviewArchive = node?.Kind == ArchiveTreeNodeKind.ReviewArchive;

      CreateArchiveVisibility = ToVisibility(isRoot);
      UploadArchiveVisibility = ToVisibility(isRoot);
      DownloadArchivesVisibility = ToVisibility(isRoot);
      OpenArchiveVisibility = ToVisibility(isArchive || isReviewArchive);
      SaveArchiveVisibility = ToVisibility(isArchive);
      PrintArchiveCatalogVisibility = ToVisibility(isArchive);
      DeleteArchiveVisibility = ToVisibility(isArchive || isReviewArchive);
      AddFileToArchiveVisibility = ToVisibility(isArchive);
      OpenArchiveFileVisibility = ToVisibility(isFile || isReviewFile || isReviewArchive);
      OpenInTextEditorVisibility = ToVisibility(isReviewFile);
      CopyArchiveFileVisibility = ToVisibility(isFile);
      CutArchiveFileVisibility = ToVisibility(isFile);
      PasteArchiveFileVisibility = ToVisibility((isFile || isArchive) && hasClipboardEntry);
      DeleteArchiveFileVisibility = ToVisibility(isFile || isReviewFile);

      HasAvailableItems = isRoot || isArchive || isFile || isReviewArchive || isReviewFile;
      RaisePropertyChanged(nameof(HasAvailableItems));
    }

    /// <summary>
    /// Преобразует логическое значение в значение видимости.
    /// </summary>
    /// <param name="value">Логическое значение.</param>
    /// <returns>
    /// Visibility.Visible, если значение true;
    /// иначе Visibility.Collapsed.
    /// </returns>
    private static Visibility ToVisibility(bool value) => value ? Visibility.Visible : Visibility.Collapsed;
  }
}
