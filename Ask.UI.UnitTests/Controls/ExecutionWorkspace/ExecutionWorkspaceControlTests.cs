using System.IO;
using System.Runtime.ExceptionServices;
using System.Windows.Controls;
using Ask.UI.ThirdParty.Docking.Integration.ExecutionWorkspace;

namespace Ask.UI.UnitTests.Controls.ExecutionWorkspace;

public sealed class ExecutionWorkspaceControlTests
{
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
    }
    finally
    {
      File.Delete(path);
    }
  });

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
