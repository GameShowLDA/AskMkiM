using System.IO;
using System.Runtime.ExceptionServices;
using System.Windows.Controls;
using Ask.UI.ThirdParty.Docking.Integration.ExecutionWorkspace;
using AvalonDock;
using AvalonDock.Controls;

namespace Ask.UI.UnitTests.Controls.ExecutionWorkspace;

public sealed class ExecutionWorkspaceControlTests
{
  [Fact]
  public void Splitters_UseExplicitWorkspaceStyle() => RunInSta(() =>
  {
    var control = new ExecutionWorkspaceControl();
    var root = Assert.IsType<Grid>(control.Content);
    var dockingManager = Assert.IsType<DockingManager>(Assert.Single(root.Children));

    Assert.NotNull(dockingManager.GridSplitterHorizontalStyle);
    Assert.Same(dockingManager.GridSplitterHorizontalStyle, dockingManager.GridSplitterVerticalStyle);
    Assert.Equal(typeof(LayoutGridResizerControl), dockingManager.GridSplitterHorizontalStyle.TargetType);
  });

  [Fact]
  public void Layout_RoundTripsWithAllContents() => RunInSta(() =>
  {
    var path = Path.Combine(Path.GetTempPath(), $"askmkim-layout-{Guid.NewGuid():N}.json");
    try
    {
      var control = new ExecutionWorkspaceControl();
      foreach (var pane in Enum.GetValues<ExecutionWorkspacePane>())
        control.SetContent(pane, new Border { Tag = pane });

      control.SaveLayout(path);
      control.ResetLayout();

      Assert.True(control.TryRestoreLayout(path));
      var json = File.ReadAllText(path);
      Assert.All(new[] { "runner.protocol", "runner.errors", "runner.equipment", "runner.translation", "runner.source", "runner.result" },
        id => Assert.Contains(id, json, StringComparison.Ordinal));
    }
    finally
    {
      File.Delete(path);
      File.Delete(FloatingWindowPlacementStore.GetCompanionPath(path));
    }
  });

  [Fact]
  public void InvalidLayout_ReturnsFalseAndRestoresDefaultLayout() => RunInSta(() =>
  {
    var path = Path.Combine(Path.GetTempPath(), $"askmkim-layout-{Guid.NewGuid():N}.json");
    try
    {
      File.WriteAllText(path, "not json");
      var control = new ExecutionWorkspaceControl();

      Assert.False(control.TryRestoreLayout(path));

      var recoveredPath = path + ".recovered";
      control.SaveLayout(recoveredPath);
      Assert.Contains("runner.protocol", File.ReadAllText(recoveredPath), StringComparison.Ordinal);
      File.Delete(recoveredPath);
      File.Delete(FloatingWindowPlacementStore.GetCompanionPath(recoveredPath));
    }
    finally
    {
      File.Delete(path);
    }
  });

  [Fact]
  public void HiddenPaneWithoutContent_DoesNotInvalidateSavedLayout() => RunInSta(() =>
  {
    var path = Path.Combine(Path.GetTempPath(), $"askmkim-layout-{Guid.NewGuid():N}.json");
    try
    {
      var control = new ExecutionWorkspaceControl();
      foreach (var pane in Enum.GetValues<ExecutionWorkspacePane>())
        control.SetContent(pane, new Border { Tag = pane });
      control.HidePane(ExecutionWorkspacePane.Result);
      control.SaveLayout(path);

      control.SetContent(ExecutionWorkspacePane.Result, null);
      control.ResetLayout();

      Assert.True(control.TryRestoreLayout(path));
      Assert.False(control.IsPaneVisible(ExecutionWorkspacePane.Result));
    }
    finally
    {
      File.Delete(path);
      File.Delete(FloatingWindowPlacementStore.GetCompanionPath(path));
    }
  });

  [Fact]
  public void FloatingPlacement_RestoresProportionsOnDifferentMonitorSize()
  {
    var placement = new FloatingPlacement
    {
      ContentId = "runner.protocol",
      Left = 0.1,
      Top = 0.2,
      Width = 0.5,
      Height = 0.4,
    };
    var monitor = new DisplayMonitor(0, "test", new System.Windows.Rect(0, 0, 2560, 1440), 1, 1, true);

    var result = FloatingWindowPlacementStore.RestoreBounds(placement, monitor);

    Assert.Equal(new System.Windows.Rect(256, 288, 1280, 576), result);
  }

  [Fact]
  public void FloatingPlacement_UsesLogicalUnitsForScaledMonitor()
  {
    var placement = new FloatingPlacement
    {
      ContentId = "runner.protocol",
      Left = 0.25,
      Top = 0.25,
      Width = 0.5,
      Height = 0.5,
    };
    var monitor = new DisplayMonitor(0, "test", new System.Windows.Rect(0, 0, 3000, 2000), 1.5, 1.5, true);

    var result = FloatingWindowPlacementStore.RestoreBounds(placement, monitor);

    Assert.Equal(new System.Windows.Rect(500, 333.3333333333333, 1000, 666.6666666666666), result);
  }

  private static void RunInSta(Action action)
  {
    Exception? failure = null;
    var thread = new Thread(() =>
    {
      try { action(); }
      catch (Exception exception) { failure = exception; }
    }) { IsBackground = true };
    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    Assert.True(thread.Join(TimeSpan.FromSeconds(20)), "ExecutionWorkspace test timed out");
    if (failure != null)
      ExceptionDispatchInfo.Capture(failure).Throw();
  }
}
