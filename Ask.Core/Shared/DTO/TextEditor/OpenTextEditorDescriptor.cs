namespace Ask.Core.Shared.DTO.TextEditor
{
  public sealed class OpenTextEditorDescriptor
  {
    public OpenTextEditorDescriptor(string displayName, string filePath, string textContent = "")
    {
      DisplayName = string.IsNullOrWhiteSpace(displayName)
        ? "Без имени"
        : displayName;
      FilePath = filePath ?? string.Empty;
      TextContent = textContent ?? string.Empty;
    }

    public string DisplayName { get; }

    public string FilePath { get; }

    public string TextContent { get; }

    public string IdentityKey => string.IsNullOrWhiteSpace(FilePath)
      ? DisplayName
      : FilePath;
  }
}
