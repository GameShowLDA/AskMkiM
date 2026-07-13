using System.Collections.ObjectModel;
using UI.Components.Invoke;
using UserControl = System.Windows.Controls.UserControl;

namespace UI.Components
{
  public sealed class EditorWorkspaceSession
  {
    public ObservableCollection<OpenFileButton> OpenPages { get; }

    public ObservableCollection<UserControl> UserControls { get; }

    public Dictionary<string, string> FilePaths { get; }

    public bool IsEmpty => OpenPages.Count == 0 && UserControls.Count == 0;

    public EditorWorkspaceSession(
      ObservableCollection<OpenFileButton> openPages,
      ObservableCollection<UserControl> userControls,
      Dictionary<string, string> filePaths)
    {
      OpenPages = openPages;
      UserControls = userControls;
      FilePaths = filePaths;
    }

    public static EditorWorkspaceSession CreateEmpty() =>
      new(new ObservableCollection<OpenFileButton>(), new ObservableCollection<UserControl>(), new Dictionary<string, string>());
  }
}
