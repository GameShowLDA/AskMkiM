using System.IO;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.TextEditor;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.Core.Shared.Metadata.Enums.RoleEnums;
using UI.Controls.Runner;
using UI.Controls.TextEditorControl;
using UI.Windows.WpfDocking.Windows;
using UI.Windows.WpfDocking.Windows.Docking;
using UI.Windows.WpfDocking.Windows.Docking.Primitives;

internal static partial class Program
{
  private static async Task VerifyRunnerLayoutAsync(Application application, string directory)
  {
    foreach (var resource in new[] { "/UI;component/Style.xaml", "/UI;component/Windows/WpfDocking/Themes/Generic.xaml" })
      application.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(resource, UriKind.Relative) });
    application.Resources[typeof(NativeFloatingWindow)] = new Style(typeof(NativeFloatingWindow),
      (Style)application.FindResource(typeof(NativeFloatingWindow)))
    {
      Setters = { new Setter(Window.ShowActivatedProperty, false), new Setter(Window.ShowInTaskbarProperty, false),
        new Setter(UIElement.OpacityProperty, 0d) },
    };
    var root = new DockControl();
    var window = new Window { Content = root, Width = 1200, Height = 800, ShowActivated = false, ShowInTaskbar = false, Opacity = 0 };
    application.MainWindow = window;
    var pathField = typeof(RunControl).GetField("_runnerLayoutPath", BindingFlags.Instance | BindingFlags.NonPublic)!;
    var store = typeof(RunControl).Assembly.GetType("UI.Controls.Runner.RunnerLayoutStore")!;
    var getPath = store.GetMethod("GetCurrentPath", BindingFlags.Static | BindingFlags.NonPublic)!;
    RoleAuthorizationConfig.SetCurrentRole(RoleType.Adjuster, "Adjuster");
    var firstPath = (string)getPath.Invoke(null, null)!;
    RoleAuthorizationConfig.SetCurrentRole(RoleType.Developer, "Developer");
    var secondPath = (string)getPath.Invoke(null, null)!;
    Require(firstPath != secondPath, "Accounts must have distinct layout files");
    var path = Path.Combine(directory, Path.GetFileName(firstPath));

    async Task<(RunControl Runner, DockItem Owner, DockControl Dock)> OpenAsync(string layoutPath)
    {
      var runner = new RunControl { FileName = "layout.asktrace" };
      pathField.SetValue(runner, layoutPath);
      runner.SetLeftEditor(new TextEditorUI(FileType.OPKW, new TextEditorModel("test.pkw", "test.opkw")));
      runner.SetSourceEditor(new TextEditorUI(FileType.PKW, new TextEditorModel("test.pkw", "test.pkw")));
      var owner = new DockItem { Title = "Runner", Content = runner, HideOnPerformClose = false };
      owner.Show(root, DockPosition.Document);
      await application.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
      return (runner, owner, (DockControl)runner.FindName("RunnerDockManager"));
    }

    try
    {
      window.Show();
      var initial = await OpenAsync(path);
      var program = initial.Dock.DockItems.Single(item => item.Content is UI.Controls.TranslatorEditor);
      initial.Runner.SourceDocument!.Show(program.FirstPane, false, Dock.Bottom,
        new SplitterDistance(180), true, DockItemShowMethod.Activate);
      initial.Runner.ShowInspectionProtocol("First result");
      var result = initial.Dock.DockItems.Single(item => item.TabText == "Итоговый протокол");
      result.Show(initial.Dock, new Rect(90000, 90000, 420, 250));
      await application.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
      initial.Owner.Close();
      Require(File.Exists(path), "Closing must persist the account layout");
      window.Width = 950;
      var restored = await OpenAsync(path);
      var source = restored.Runner.SourceDocument!;
      var restoredProgram = restored.Dock.DockItems.Single(item => item.Content is UI.Controls.TranslatorEditor);
      Require(source.FirstPane.Parent == restoredProgram.FirstPane.Parent
        && source.FirstPane.Parent.SplitterDistance.IsStar, "Split must be restored as a proportion");
      Require(source.FirstPane.Parent.Child2 == source.FirstPane
        && source.FirstPane.Parent.IsSplitterTopLeft,
        "Restoration must preserve the bottom source and the size of the top program");
      var restoredRatio = source.FirstPane.Parent.SplitterDistance.Value;
      window.Height = 420;
      var hidden = restored.Dock.DockItems.Single(item => item.TabText == "Итоговый протокол");
      Require(hidden.IsHidden, "The result pane must stay hidden until a protocol arrives");
      restored.Runner.ShowInspectionProtocol("Second result");
      await application.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
      Require(hidden.DockPosition == DockPosition.Floating, "The result must return to its floating pane");
      var floating = application.Windows.OfType<NativeFloatingWindow>().Single();
      Require(floating.Left < 90000 && floating.Top < 90000, "Off-screen floating bounds must be fitted to an available monitor");
      typeof(RunControl).GetMethod("SuspendRunnerFloatingWindows", BindingFlags.Instance | BindingFlags.NonPublic)!
        .Invoke(restored.Runner, null);
      await application.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
      Require(hidden.IsHidden && !floating.IsVisible, "Suspending a workspace must hide its floating windows");
      typeof(RunControl).GetMethod("ResumeRunnerFloatingWindows", BindingFlags.Instance | BindingFlags.NonPublic)!
        .Invoke(restored.Runner, null);
      Require(hidden.DockPosition == DockPosition.Floating, "Returning to the account must resume its floating windows");
      restored.Runner.ClearInspectionProtocol();
      restored.Owner.Close();

      var legacy = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
      legacy.Remove("Version");
      foreach (var action in legacy["Actions"]!.AsArray().Where(action => action!["Kind"]!.GetValue<string>() == "split"))
      {
        var side = (Dock)action!["Side"]!.GetValue<int>();
        bool targetFirst = side is Dock.Right or Dock.Bottom;
        bool sizedFirst = targetFirst == action["SizeForTarget"]!.GetValue<bool>();
        bool horizontal = side is Dock.Left or Dock.Right;
        action["Side"] = (int)(sizedFirst
          ? (horizontal ? Dock.Left : Dock.Top)
          : (horizontal ? Dock.Right : Dock.Bottom));
        action["SizeForTarget"] = targetFirst;
      }
      File.WriteAllText(path, legacy.ToJsonString());
      var reopened = await OpenAsync(path);
      var reopenedSource = reopened.Runner.SourceDocument!.FirstPane;
      Require(reopenedSource.Parent.Child2 == reopenedSource && reopenedSource.Parent.IsSplitterTopLeft,
        "Legacy files must restore the original side and sized panel");
      Require(Math.Abs(reopenedSource.Parent.SplitterDistance.Value - restoredRatio) < 0.0001,
        "Saving in a smaller window must preserve the configured split ratio");
      reopened.Runner.ShowInspectionProtocol("Third result");
      Require(reopened.Dock.DockItems.Single(item => item.TabText == "Итоговый протокол")
        .DockPosition == DockPosition.Floating, "Closing without a result must preserve its last position");
      reopened.Owner.Close();

      var other = await OpenAsync(Path.Combine(directory, Path.GetFileName(secondPath)));
      Require(other.Dock.DockItems.Count == 5, "A second account must start with its own default layout");
      other.Owner.Close();
      File.WriteAllText(path, "{ broken json");
      var fallback = await OpenAsync(path);
      Require(fallback.Dock.DockItems.Count == 5, "A damaged file must fall back to the default layout");
      fallback.Owner.Close();
      Log("Runner layout regression passed: account isolation, proportional splits, floating result restoration");
    }
    finally
    {
      window.Close();
    }
  }
}
