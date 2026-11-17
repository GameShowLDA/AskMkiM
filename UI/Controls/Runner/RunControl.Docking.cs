using Ask.UI.ThirdParty.Docking.Integration.ExecutionWorkspace;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using UI.Windows.WpfDocking.Windows.Docking;

namespace UI.Controls.Runner;

public partial class RunControl
{
  private bool _dockLayoutInitialized;
  private bool _closingDockLayout;
  private DockItem? _runnerOwner;
  private readonly Dictionary<DockItem, EventHandler> _workspaceTitleHandlers = [];
  private static readonly DependencyPropertyDescriptor TabTextDescriptor =
    DependencyPropertyDescriptor.FromProperty(DockItem.TabTextProperty, typeof(DockItem));

  private void InitializeDocking()
  {
    ProtocolPane.Closing += RunnerPane_Closing;
    ErrorsPane.Closing += RunnerPane_Closing;
    RunnerWorkspace.Loaded += (_, _) =>
    {
      InitializeDockLayout();
      ResumeRunnerFloatingWindows();
      Dispatcher.BeginInvoke(new Action(ShowPendingInspectionProtocol), DispatcherPriority.Loaded);
    };
    Unloaded += RunControl_Unloaded;
  }

  private void InitializeDockLayout()
  {
    if (_closingDockLayout || !RunnerWorkspace.IsLoaded)
      return;

    _dockLayoutInitialized = true;
    var hostWindow = Window.GetWindow(this);
    if (_layoutHostWindow != hostWindow)
    {
      DetachLayoutHost();
      _layoutHostWindow = hostWindow;
      if (_layoutHostWindow != null)
        _layoutHostWindow.Closing += LayoutHost_Closing;
    }

    for (DependencyObject? parent = VisualTreeHelper.GetParent(this); parent != null;
      parent = VisualTreeHelper.GetParent(parent))
    {
      if (parent is not DockItem owner || owner.Content != this)
        continue;

      if (_runnerOwner != owner)
      {
        if (_runnerOwner != null)
        {
          _runnerOwner.StateChanged -= RunnerOwner_StateChanged;
          _runnerOwner.Closing -= RunnerOwner_Closing;
        }

        _runnerOwner = owner;
        _runnerOwner.StateChanged += RunnerOwner_StateChanged;
        _runnerOwner.Closing += RunnerOwner_Closing;
      }

      break;
    }
  }

  private void RunnerPane_Closing(object? sender, CancelEventArgs e) =>
    e.Cancel = !_closingDockLayout && !_restoringRunnerLayout;

  private void RunnerOwner_Closing(object? sender, CancelEventArgs e)
  {
    if (!e.Cancel)
      SaveRunnerLayout();
  }

  private void RunnerOwner_StateChanged(object? sender, DockItemStateEventArgs e)
  {
    if (e.StateChangeMethod != DockItemStateChangeMethod.Close)
      return;

    _closingDockLayout = true;
    SaveRunnerLayout(allowClosing: true);
    UnsubscribeRunnerEvents();
    DetachLayoutHost();
    RunnerWorkspace.CloseFloatingWindows();
    if (_runnerOwner != null)
    {
      _runnerOwner.StateChanged -= RunnerOwner_StateChanged;
      _runnerOwner.Closing -= RunnerOwner_Closing;
      _runnerOwner = null;
    }

    foreach (var item in _runnerDocuments.ToArray())
    {
      StopTrackingWorkspaceTitle(item);
      item.Close();
    }
  }

  private void ShowRunnerDocument(DockItem document, DockItemShowMethod showMethod = DockItemShowMethod.Activate)
  {
    if (_closingDockLayout || !TryGetWorkspacePane(document, out var pane))
      return;

    RunnerWorkspace.SetContent(pane, document);
    RunnerWorkspace.SetTitle(pane, document.TabText ?? document.Title ?? string.Empty);
    TrackWorkspaceTitle(document);
    RunnerWorkspace.ShowPane(pane, showMethod is not DockItemShowMethod.Deselect);
  }

  private void TrackWorkspaceTitle(DockItem document)
  {
    if (_workspaceTitleHandlers.ContainsKey(document))
      return;

    EventHandler handler = (_, _) =>
    {
      if (TryGetWorkspacePane(document, out var pane))
        RunnerWorkspace.SetTitle(pane, document.TabText ?? document.Title ?? string.Empty);
    };
    _workspaceTitleHandlers.Add(document, handler);
    TabTextDescriptor.AddValueChanged(document, handler);
  }

  private void StopTrackingWorkspaceTitle(DockItem document)
  {
    if (!_workspaceTitleHandlers.Remove(document, out var handler))
      return;

    TabTextDescriptor.RemoveValueChanged(document, handler);
  }

  private void SelectRunnerDocument(DockItem document)
  {
    if (!_closingDockLayout && TryGetWorkspacePane(document, out var pane))
      RunnerWorkspace.ActivatePane(pane);
  }

  private bool TryGetWorkspacePane(DockItem document, out ExecutionWorkspacePane pane)
  {
    pane = document switch
    {
      _ when document == ProtocolPane => ExecutionWorkspacePane.Protocol,
      _ when document == ErrorsPane => ExecutionWorkspacePane.Errors,
      _ when document == SourceDocument => ExecutionWorkspacePane.Source,
      _ when document == _inspectionProtocolDockItem => ExecutionWorkspacePane.Result,
      { Content: TranslatorEditor } => ExecutionWorkspacePane.Translation,
      { Content: DevicesStatus } => ExecutionWorkspacePane.Equipment,
      _ => default,
    };
    return document == ProtocolPane || document == ErrorsPane || document == SourceDocument
      || document == _inspectionProtocolDockItem || document.Content is TranslatorEditor or DevicesStatus;
  }

  private void RunControl_Unloaded(object sender, RoutedEventArgs e)
  {
    DetachLayoutHost();
  }
}
