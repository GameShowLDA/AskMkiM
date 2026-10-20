using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.UI.Features.TextSearch.Views;
using Ask.UI.Infrastructure.UI.Overlay.Drawer.Runtime;

namespace MainWindowProgram;

public partial class MainWindow
{
  private SearchPanelWindow? _searchPanel;

  internal void RefreshSearchTarget() => _searchPanel?.SynchronizeTarget();

  internal void ShowSearchPanel()
  {
    if (_isUserSwitchInProgress || DrawerHostService.Instance.ShouldBlockGlobalInput)
      return;

    if (_searchPanel == null)
    {
      _searchPanel = new SearchPanelWindow(this, TopPanel)
      {
        TargetProvider = MultiWindow.GetActiveSearchTarget,
        EditorFindAllAsync = MultiWindow.FindAllForPanelAsync,
        CanReplaceEditor = () => MultiWindow.GetActiveTextEditor()?.TextEditor is { IsReadOnly: false },
        ClearEditorHighlights = MultiWindow.OnSearchWindowClosing,
        ClearReplacementHighlights = MultiWindow.ClearReplacementHighlights,
        EditorSearchAsync = (query, allDocuments, action) => MultiWindow.SearchData(
          query.Text, query.WholeWord, query.MatchCase, allDocuments ? 1 : 0,
          action == TextSearchAction.Previous ? "FindPrevious" : "FindNext"),
        EditorReplaceAsync = (query, replacement, allDocuments, replaceAll) => MultiWindow.ReplaceData(
          replacement, query.Text, query.WholeWord, query.MatchCase, allDocuments ? 1 : 0,
          replaceAll ? "FindAll" : "FindNext")
      };
      _searchPanel.Closed += (_, _) => _searchPanel = null;
    }

    _searchPanel.SetSearchText(MultiWindow.GetActiveTextEditor()?.TextArea.Selection.GetText());
    _searchPanel.ShowPanel();
  }
}
