namespace Ask.Core.Shared.Metadata.View.EditorHost
{
  /// <summary>
  /// Управляет жизненным циклом документов в редакторе:
  /// создание, открытие, сохранение и вывод.
  /// Не управляет состоянием вкладок и активным редактором.
  /// </summary>
  public interface IEditorDocumentService
  {
    /// <summary>
    /// Создаёт новый пустой документ и открывает его в редакторе.
    /// </summary>
    void CreateNewFile();

    /// <summary>
    /// Открывает документ по указанному пути.
    /// </summary>
    /// <param name="filePath">Абсолютный путь к файлу.</param>
    void OpenFile(string filePath);

    /// <summary>
    /// Сохраняет текущий активный документ.
    /// </summary>
    void SaveFile();

    /// <summary>
    /// Сохраняет текущий документ под новым именем.
    /// </summary>
    void SaveFileAs();

    /// <summary>
    /// Выводит текущий документ на печать.
    /// </summary>
    void PrintFile();

    /// <summary>
    /// Открывает каталог текущего документа в файловом менеджере ОС.
    /// </summary>
    void OpenFolder();
  }
}
