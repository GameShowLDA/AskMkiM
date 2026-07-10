using System.Text;

namespace Ask.Core.Shared.DTO.TextEditor
{
  public class TextEditorModel
  {
    public string FilePath { get; set; }
    public string FileName { get; set; }
    public string OriginalFileName { get; set; }
    public Encoding Encoding { get; set; }
    public string? SavedTextSnapshot { get; set; }
    public List<string>? SourceLines { get; set; } = new();

    public TextEditorModel(string filePath, Encoding encoding = null)
    {
      FilePath = filePath;
      Encoding = encoding ?? Encoding.UTF8;
    }

    public TextEditorModel(string filePath, string fileName, Encoding encoding = null) : this(filePath)
    {
      FilePath = filePath;
      FileName = fileName;
      Encoding = encoding ?? Encoding.UTF8;
    }
    public TextEditorModel(string filePath, string fileName, string originalFilename, Encoding encoding = null) : this(filePath, fileName, encoding)
    {
      FilePath = filePath;
      FileName = fileName;
      OriginalFileName = originalFilename;
      Encoding = encoding ?? Encoding.UTF8;
    }
  }
}
