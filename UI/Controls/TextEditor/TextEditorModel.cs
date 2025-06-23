using System.Text;

namespace UI.Controls.TextEditor
{
  public class TextEditorModel
  {
    public string FilePath { get; set; }
    public string FileName { get; set; }
    public Encoding Encoding { get; set; }

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
  }
}
