using System.Windows;
using UI.Components.Invoke;
using UserControl = System.Windows.Controls.UserControl;

namespace UI.Components
{
  public sealed class MultiWindowWorkspaceSession
  {
    public EditorWorkspaceSession EditorSession { get; }

    public List<OpenFileButton> SearchOpenPages { get; }

    public List<UserControl> SearchUserControls { get; }

    public string SearchResultsText { get; }

    public GridLength SearchResultsRowHeight { get; }

    public Visibility SearchResultsVisibility { get; }

    public Visibility SearchDataGridVisibility { get; }

    public Visibility SplitterVisibility { get; }

    public bool IsEmpty => EditorSession.IsEmpty && SearchOpenPages.Count == 0 && SearchUserControls.Count == 0;

    public MultiWindowWorkspaceSession(
      EditorWorkspaceSession editorSession,
      List<OpenFileButton> searchOpenPages,
      List<UserControl> searchUserControls,
      string searchResultsText,
      GridLength searchResultsRowHeight,
      Visibility searchResultsVisibility,
      Visibility searchDataGridVisibility,
      Visibility splitterVisibility)
    {
      EditorSession = editorSession;
      SearchOpenPages = searchOpenPages;
      SearchUserControls = searchUserControls;
      SearchResultsText = searchResultsText;
      SearchResultsRowHeight = searchResultsRowHeight;
      SearchResultsVisibility = searchResultsVisibility;
      SearchDataGridVisibility = searchDataGridVisibility;
      SplitterVisibility = splitterVisibility;
    }

    public static MultiWindowWorkspaceSession CreateEmpty() =>
      new(
        EditorWorkspaceSession.CreateEmpty(),
        new List<OpenFileButton>(),
        new List<UserControl>(),
        string.Empty,
        new GridLength(0),
        Visibility.Collapsed,
        Visibility.Collapsed,
        Visibility.Collapsed);
  }
}
