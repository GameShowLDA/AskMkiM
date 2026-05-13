namespace Ask.UI.Features.Archive.ViewModels
{
  /// <summary>
  /// Тип узла дерева архивов.
  /// </summary>
  public enum ArchiveTreeNodeKind
  {
    /// <summary>
    /// Корневой узел архивов.
    /// </summary>
    Root,

    /// <summary>
    /// Корневой узел архивов на проверке.
    /// </summary>
    ReviewRoot,

    /// <summary>
    /// Узел архива.
    /// </summary>
    Archive,

    /// <summary>
    /// Узел архива на проверке.
    /// </summary>
    ReviewArchive,

    /// <summary>
    /// Узел файла архива.
    /// </summary>
    File,

    /// <summary>
    /// Узел файла архива на проверке.
    /// </summary>
    ReviewFile,

    /// <summary>
    /// Служебный узел-заглушка.
    /// </summary>
    Placeholder,
  }
}
