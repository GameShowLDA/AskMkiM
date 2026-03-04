using System.Windows.Controls;

namespace Ask.Core.Shared.Metadata.View.EditorHost
{
  public interface IRunView
  {
    string FileName { get; }

    string OpkFilePath { get; set; }
    UserControl View { get; }
  }
}
