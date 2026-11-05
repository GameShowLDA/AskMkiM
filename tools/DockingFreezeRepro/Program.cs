using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Ask.Diagnostics.Abstractions;
using Ask.Diagnostics.Configuration;
using Ask.Diagnostics.Extensions;
using Ask.Diagnostics.Models;
using Ask.Diagnostics.Services;
using Ask.Core.Shared.DTO.Protocol;
using Ask.UI.Components.ProtocolListBox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using UI.Controls.Runner;
using UI.Windows.WpfDocking.Windows;
using UI.Windows.WpfDocking.Windows.Docking;
using UI.Windows.WpfDocking.Windows.Docking.Primitives;

internal static class Program
{
  private static readonly Stopwatch Clock = Stopwatch.StartNew();
  private static int _heartbeat;

  [STAThread]
  private static int Main(string[] args)
  {
    var mode = args.FirstOrDefault() ?? "detach";
    if (mode is not ("detach" or "collapse" or "close-runner" or "protocol-layout" or "blocking" or "blocking-worker" or "async"))
    {
      Console.Error.WriteLine("Modes: detach | collapse | close-runner | protocol-layout | blocking | blocking-worker | async");
      return 1;
    }

    var root = Path.Combine(Path.GetTempPath(), "AskMkiM.DockingFreezeRepro", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(root);
    using var trace = new StreamWriter(Path.Combine(root, "trace.log")) { AutoFlush = true };
    var original = Console.Out;
    Console.SetOut(TextWriter.Synchronized(new CombinedWriter(original, trace)));
    Log($"mode={mode}; output={root}");

    var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
    SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(application.Dispatcher));
    var exitCode = 0;
    application.Dispatcher.BeginInvoke(new Action(async () =>
    {
      try
      {
        if (mode is "detach" or "collapse" or "close-runner")
          await ReproduceDockingAsync(application, mode);
        else if (mode == "protocol-layout")
          await VerifyProtocolLayoutAsync(application);
        else
          await ReproduceDiagnosticsAsync(application, root, mode);
      }
      catch (Exception exception)
      {
        Log(exception.ToString());
        exitCode = 1;
      }
      finally
      {
        application.Shutdown();
      }
    }));
    application.Run();
    Log($"exit={exitCode}");
    return exitCode;
  }

  private static async Task VerifyProtocolLayoutAsync(Application application)
  {
    var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    _ = Task.Run(async () =>
    {
      if (await Task.WhenAny(completed.Task, Task.Delay(TimeSpan.FromSeconds(30))) != completed.Task)
      {
        Log("WATCHDOG: protocol layout did not finish in 30 seconds; terminating only this test process");
        Environment.Exit(2);
      }
    });

    Window? owner = null;
    try
    {
      application.Resources.MergedDictionaries.Add(new ResourceDictionary
      {
        Source = new Uri("/UI;component/Style.xaml", UriKind.Relative)
      });
      application.Resources.MergedDictionaries.Add(new ResourceDictionary
      {
        Source = new Uri("/UI;component/Windows/WpfDocking/Themes/Generic.xaml", UriKind.Relative)
      });
      var protocol = new ProtocolListBoxUI();
      for (var i = 0; i < 200; i++)
        await protocol.AppendLineAsync(new ShowMessageModel($"Строка {i}: проверка цепи К1/51 → К1/55; подключение к шине [A]; результат 4,2 Ом. Текст для переноса строк."));

      UIElement content = protocol;
      for (var depth = 0; depth < 4; depth++)
        content = new SplitContainer
        {
          Child1 = content,
          Child2 = new TextBlock { Text = "Другая панель" },
          Orientation = depth % 2 == 0 ? Orientation.Horizontal : Orientation.Vertical
        };

      owner = new Window
      {
        Content = content, Width = 1600, Height = 1000,
        ShowActivated = false, ShowInTaskbar = false, Opacity = 0
      };
      var watch = Stopwatch.StartNew();
      owner.Show();
      await application.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
      Log($"Initial protocol layout: {watch.ElapsedMilliseconds} ms");
      watch.Restart();
      for (var i = 0; i < 15; i++)
      {
        await protocol.AppendLineAsync(new ShowMessageModel($"Новая строка {i}: измерение сопротивления. " + new string('x', 120)));
        await application.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
      }

      var realizedCount = CountVisualItems(protocol);
      Log($"15 protocol updates: {watch.ElapsedMilliseconds} ms; realized containers={realizedCount}");
      Require(realizedCount < 200, "The protocol must virtualize offscreen rows");
    }
    finally
    {
      owner?.Close();
      completed.TrySetResult();
    }
  }

  private static int CountVisualItems(DependencyObject parent)
  {
    var count = 0;
    for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
    {
      var child = VisualTreeHelper.GetChild(parent, i);
      if (child is ListBoxItem)
        count++;
      count += CountVisualItems(child);
    }

    return count;
  }

  private static async Task ReproduceDockingAsync(Application application, string mode)
  {
    application.Resources.MergedDictionaries.Add(new ResourceDictionary
    {
      Source = new Uri("/UI;component/Style.xaml", UriKind.Relative)
    });
    application.Resources.MergedDictionaries.Add(new ResourceDictionary
    {
      Source = new Uri("/UI;component/Windows/WpfDocking/Themes/Generic.xaml", UriKind.Relative)
    });
    application.Resources[typeof(NativeFloatingWindow)] = new Style(typeof(NativeFloatingWindow),
      (Style)application.FindResource(typeof(NativeFloatingWindow)))
    {
      Setters =
      {
        new Setter(Window.ShowActivatedProperty, false),
        new Setter(Window.ShowInTaskbarProperty, false),
        new Setter(UIElement.OpacityProperty, 0d)
      }
    };

    var dock = new DockControl();
    RunControl? runner = mode == "close-runner" ? new RunControl() : null;
    var parentDock = runner == null ? dock : new DockControl();
    var parentItem = runner == null ? null : new DockItem { Title = "Runner", Content = runner };
    var host = new Grid();
    host.Children.Add(parentDock);
    var owner = new Window
    {
      Content = host, Width = 500, Height = 300,
      ShowInTaskbar = false, ShowActivated = false, Opacity = 0
    };
    application.MainWindow = owner;
    var item = new DockItem { Title = "Freeze reproduction", Content = new TextBlock { Text = "No equipment or database" } };
    try
    {
      owner.Show();
      await application.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
      if (runner != null)
      {
        parentItem!.Show(parentDock, DockPosition.Document);
        await application.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        dock = (DockControl)runner.FindName("RunnerDockManager");
        runner.ShowInspectionProtocol("Isolated inspection protocol");
        item = dock.DockItems.Single(document => document.TabText == "Итоговый протокол");
      }
      item.Show(dock, new Rect(100, 100, 350, 250));
      await application.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
      var floating = application.Windows.OfType<NativeFloatingWindow>().Single();
      floating.Opacity = 0;
      Log($"before: loaded={dock.IsLoaded}; connected={PresentationSource.FromVisual(dock) != null}; floatingVisible={floating.IsVisible}");
      Require(dock.IsLoaded && PresentationSource.FromVisual(dock) != null && floating.IsVisible,
        "Initial dock/floating window state does not match the scenario");
      if (mode == "detach")
        host.Children.Remove(dock);
      else if (mode == "close-runner")
        parentItem!.Close();
      else
        dock.Visibility = Visibility.Collapsed;
      await application.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
      Log($"after: loaded={dock.IsLoaded}; connected={PresentationSource.FromVisual(dock) != null}; floatingVisible={floating.IsVisible}; items={dock.DockItems.Count}");
      if (mode == "close-runner")
      {
        Require(!floating.IsVisible && dock.DockItems.Count == 0,
          "Closing the runner must close all floating documents and panels");
        Log("Runner close regression passed: no orphan floating windows");
        return;
      }

      Require(floating.IsVisible, "Expected the floating window to survive its container");
      Require((PresentationSource.FromVisual(dock) != null) == (mode == "collapse"),
        "Unexpected connection of the dock to PresentationSource");

      // Воспроизводит вход из NativeFloatingWindow без глобального ввода мыши.
      var beginDrag = typeof(DockManager).GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
        .Single(method => method.Name == "BeginDrag" && method.GetParameters()[2].ParameterType == typeof(MouseEventArgs));
      var reproduced = false;
      try
      {
        beginDrag.Invoke(null, [floating, floating, new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount)]);
        Log("BeginDrag returned without exception");
      }
      catch (TargetInvocationException exception) when (exception.InnerException != null)
      {
        Log($"BeginDrag exception: {exception.InnerException}");
        reproduced = exception.InnerException is InvalidOperationException
          && exception.InnerException.StackTrace?.Contains("WindowHandler.BeginDragNative", StringComparison.Ordinal) == true;
        if (mode == "collapse" || !reproduced)
          throw;
      }
      Require(reproduced == (mode != "collapse"), "Expected docking exception was not reproduced");
    }
    finally
    {
      Mouse.Capture(null);
      foreach (var dockItem in dock.DockItems.ToArray()) dockItem.Close();
      owner.Close();
    }
  }

  private static async Task ReproduceDiagnosticsAsync(Application application, string root, string mode)
  {
    var services = new ServiceCollection();
    services.AddCrashDiagnostics(options =>
    {
      options.Path = Path.Combine(root, "CrashReports");
      options.CleanupPolicy = CrashPackageCleanupPolicy.None;
      options.IncludeScreenshot = true;
      options.IncludeConfig = false;
      options.IncludeLogs = false;
    }, Log, (exception, message) => Log($"{message}: {exception}"));
    using var provider = services.BuildServiceProvider();
    var collectors = provider.GetServices<ICrashDataCollector>().Select(collector => new TracedCollector(collector));
    var service = new CrashPackageService(collectors, provider.GetRequiredService<IOptions<CrashPackageOptions>>(),
      provider.GetRequiredService<ICrashPackageLogSink>());
    using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(3));
    var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var probe = Task.Run(async () =>
    {
      await Task.Delay(250);
      _ = application.Dispatcher.BeginInvoke(new Action(() =>
      {
        Interlocked.Exchange(ref _heartbeat, 1);
        Log("UI heartbeat executed");
      }));
      await Task.Delay(1000);
      Log($"probe: heartbeat={Volatile.Read(ref _heartbeat)}; done={done.Task.IsCompleted}");
      if (await Task.WhenAny(done.Task, Task.Delay(6000)) != done.Task)
      {
        Log("WATCHDOG: diagnostic wait did not return; terminating only this reproduction process");
        Environment.Exit(2);
      }
    });
    try
    {
      SynchronizationContext.SetSynchronizationContext(new TracedSynchronizationContext(SynchronizationContext.Current!));
      var exception = new InvalidOperationException("Isolated UI crash reproduction");
      var pending = mode == "blocking-worker"
        ? Task.Run(() => service.CreateAsync(exception, cancellationToken: cancellation.Token))
        : service.CreateAsync(exception, cancellationToken: cancellation.Token);
      if (mode != "async")
      {
        Log("UI enters GetAwaiter().GetResult(), as in App.CreateCrashPackage");
        pending.GetAwaiter().GetResult();
      }
      else
      {
        Log("UI awaits diagnostic task asynchronously (control experiment)");
        await pending;
      }
    }
    catch (OperationCanceledException exception)
    {
      Log($"diagnostics canceled: {exception.GetType().Name}");
    }
    finally
    {
      done.TrySetResult();
    }
    await probe;
    Log($"diagnostics finished; heartbeat={Volatile.Read(ref _heartbeat)}");
    Require(Volatile.Read(ref _heartbeat) == 1, "UI heartbeat was not executed after diagnostics");
    if (mode == "async")
      Require(Directory.EnumerateFiles(Path.Combine(root, "CrashReports"), "*.zip").Any(), "Asynchronous control did not produce an archive");
  }

  private static void Log(string message) => Console.WriteLine($"{Clock.Elapsed.TotalMilliseconds:F0}ms [thread {Environment.CurrentManagedThreadId}] {message}");

  private static void Require(bool condition, string message)
  {
    if (!condition) throw new InvalidOperationException(message);
  }

  private sealed class TracedCollector(ICrashDataCollector inner) : ICrashDataCollector
  {
    public string Name => inner.Name;
    public int Order => inner.Order;
    public async Task CollectAsync(CrashContext context, CancellationToken cancellationToken = default)
    {
      Log($"collector {Name}: start");
      try { await inner.CollectAsync(context, cancellationToken).ConfigureAwait(false); }
      finally { Log($"collector {Name}: leave"); }
    }
  }

  private sealed class TracedSynchronizationContext(SynchronizationContext inner) : SynchronizationContext
  {
    public override void Post(SendOrPostCallback callback, object? state)
    {
      var continuation = state is Delegate action ? action.Target?.GetType().ToString() : state?.GetType().ToString();
      Log($"UI continuation queued: {continuation}");
      inner.Post(callback, state);
    }

    public override void Send(SendOrPostCallback callback, object? state) => inner.Send(callback, state);
    public override SynchronizationContext CreateCopy() => new TracedSynchronizationContext(inner.CreateCopy());
  }

  private sealed class CombinedWriter(TextWriter console, TextWriter file) : TextWriter
  {
    public override System.Text.Encoding Encoding => console.Encoding;
    public override void WriteLine(string? value)
    {
      console.WriteLine(value);
      file.WriteLine(value);
    }
  }
}
