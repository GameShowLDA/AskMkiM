namespace Ask.Core.Shared.DTO.TextEditor
{
  /// <summary>
  /// Краткое описание файла, открытого в основном текстовом редакторе.
  /// </summary>
  public sealed class OpenTextEditorDescriptor
  {
    public OpenTextEditorDescriptor(string displayName, string filePath)
    {
      DisplayName = string.IsNullOrWhiteSpace(displayName)
        ? "Без имени"
        : displayName;
      FilePath = filePath ?? string.Empty;
    }

    public string DisplayName { get; }

    public string FilePath { get; }

    public string IdentityKey => string.IsNullOrWhiteSpace(FilePath)
      ? DisplayName
      : FilePath;
  }
}
