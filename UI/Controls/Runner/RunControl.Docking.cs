using Ask.UI.Controls.ErrorList;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using UI.Windows.WpfDocking.Windows;
using UI.Windows.WpfDocking.Windows.Docking;

namespace UI.Controls.Runner
{
  public partial class RunControl
  {
    private static readonly DependencyPropertyDescriptor ErrorSplitDistanceDescriptor =
      DependencyPropertyDescriptor.FromProperty(DockPaneSplit.SplitterDistanceProperty, typeof(DockPaneSplit));

    private readonly DispatcherTimer _errorResizeTimer = new DispatcherTimer
    {
      Interval = TimeSpan.FromMilliseconds(250),
    };

    private bool _dockLayoutInitialized;
    private bool _closingDockLayout;
    private bool _applyingErrorHeight;
    private bool _errorHeightCustomized;
    private DockPaneSplit? _errorPaneSplit;
    private DockItem? _runnerOwner;

    private void InitializeDocking()
    {
      ProtocolPane.Closing += RunnerPane_Closing;
      ErrorsPane.Closing += RunnerPane_Closing;
      RunnerDockManager.DockItemStateChanged += RunnerDockManager_DockItemStateChanged;
      _errorResizeTimer.Tick += ErrorResizeTimer_Tick;
      Unloaded += RunControl_Unloaded;
    }

    private void InitializeDockLayout()
    {
      if (_closingDockLayout)
        return;

      if (!_dockLayoutInitialized)
      {
        _dockLayoutInitialized = true;
        ProtocolPane.Show(RunnerDockManager, DockPosition.Document);
        ErrorsPane.Show(ProtocolPane.FirstPane, Dock.Bottom,
          new SplitterDistance(ErrorListLayoutSettings.GetInitialHeight()));
      }

      UpdateErrorPaneSplit();
      for (DependencyObject? parent = VisualTreeHelper.GetParent(this); parent != null;
        parent = VisualTreeHelper.GetParent(parent))
      {
        if (parent is DockItem owner && owner.Content == this)
        {
          if (_runnerOwner != owner)
          {
            if (_runnerOwner != null)
              _runnerOwner.StateChanged -= RunnerOwner_StateChanged;

            _runnerOwner = owner;
            _runnerOwner.StateChanged += RunnerOwner_StateChanged;
          }

          break;
        }
      }
    }

    private void RunnerPane_Closing(object? sender, CancelEventArgs e)
    {
      e.Cancel = !_closingDockLayout;
    }

    private void RunnerOwner_StateChanged(object? sender, DockItemStateEventArgs e)
    {
      if (e.StateChangeMethod != DockItemStateChangeMethod.Close)
        return;

      _closingDockLayout = true;
      _errorResizeTimer.Stop();
      DetachErrorPaneSplit();
      if (_runnerOwner != null)
      {
        _runnerOwner.StateChanged -= RunnerOwner_StateChanged;
        _runnerOwner = null;
      }

      // Закрытие вкладки исполнителя должно также удалить все его вложенные floating windows.
      foreach (var item in RunnerDockManager.DockItems.ToArray())
        item.Close();
    }

    private void ShowRunnerDocument(DockItem document, DockItemShowMethod showMethod = DockItemShowMethod.Activate)
    {
      if (_closingDockLayout)
        return;

      InitializeDockLayout();
      var anchor = RunnerDockManager.DockItems.FirstOrDefault(item =>
        item != document && item != ProtocolPane && item != ErrorsPane && item.FirstPane != null);
      if (anchor != null)
      {
        document.Show(anchor.FirstPane, anchor.FirstPane.Items.Count, showMethod);
      }
      else
      {
        DockPaneNode leftArea = ProtocolPane.FirstPane.Parent ?? (DockPaneNode)ProtocolPane.FirstPane;
        document.Show(leftArea, false, Dock.Right, new SplitterDistance(1, SplitterUnitType.Star), false, showMethod);
      }
    }

    private void SelectRunnerDocument(DockItem document)
    {
      if (_closingDockLayout)
        return;

      if (document.DockPosition == DockPosition.Unknown)
      {
        // До Loaded транслированную программу выберет ShowDocuments после создания панелей.
        if (RunnerDockManager.IsLoaded)
          ShowRunnerDocument(document, DockItemShowMethod.Select);
        return;
      }

      document.Show(DockItemShowMethod.Select);
    }

    private void RunControl_Unloaded(object sender, RoutedEventArgs e)
    {
      _errorResizeTimer.Stop();
      DetachErrorPaneSplit();
    }

    private void RunnerDockManager_DockItemStateChanged(object? sender, DockItemStateEventArgs e)
    {
      if (!IsLoaded || _closingDockLayout)
        return;

      UpdateErrorPaneSplit();
      Dispatcher.BeginInvoke(new Action(ErrorListBoxVertical.RefreshLayoutFromHost), DispatcherPriority.Loaded);
    }

    private void UpdateErrorPaneSplit()
    {
      var pane = ErrorsPane.FirstPane;
      var split = pane?.Items.Count == 1 && pane.Parent?.Orientation == Orientation.Vertical
        ? pane.Parent
        : null;
      if (_errorPaneSplit == split)
        return;

      _errorResizeTimer.Stop();
      DetachErrorPaneSplit();
      _errorPaneSplit = split;
      if (_errorPaneSplit != null)
        ErrorSplitDistanceDescriptor.AddValueChanged(_errorPaneSplit, ErrorPaneSplitDistanceChanged);
    }

    private void DetachErrorPaneSplit()
    {
      if (_errorPaneSplit != null)
        ErrorSplitDistanceDescriptor.RemoveValueChanged(_errorPaneSplit, ErrorPaneSplitDistanceChanged);

      _errorPaneSplit = null;
    }

    private void ErrorPaneSplitDistanceChanged(object? sender, EventArgs e)
    {
      if (_applyingErrorHeight)
        return;

      _errorHeightCustomized = true;
      _errorResizeTimer.Stop();
      _errorResizeTimer.Start();
    }

    private void ErrorResizeTimer_Tick(object? sender, EventArgs e)
    {
      _errorResizeTimer.Stop();
      ErrorListLayoutSettings.SaveHeight(ErrorListBoxVertical.ActualHeight);
      ErrorListBoxVertical.RefreshLayoutFromHost();
    }

    private void ErrorListBoxVertical_DesiredHeightChanged(double height)
    {
      if (_errorHeightCustomized || _errorResizeTimer.IsEnabled || _errorPaneSplit == null)
        return;

      var sizedPane = _errorPaneSplit.IsSplitterTopLeft ? _errorPaneSplit.Child1 : _errorPaneSplit.Child2;
      if (sizedPane != ErrorsPane.FirstPane)
        return;

      _applyingErrorHeight = true;
      try
      {
        _errorPaneSplit.SplitterDistance = new SplitterDistance(ErrorListLayoutSettings.ClampHeight(height));
      }
      finally
      {
        _applyingErrorHeight = false;
      }
    }
  }
}
