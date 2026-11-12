using System.Windows;

namespace AskMkiM.Updater.Stub;

/// <summary>
/// Запускает автономный установщик обновления AskMkiM.
/// </summary>
public partial class App : Application
{
  /// <inheritdoc />
  protected override async void OnStartup(StartupEventArgs e)
  {
    base.OnStartup(e);
    if (e.Args.Length == 1 && string.Equals(e.Args[0], "--verify-package", StringComparison.OrdinalIgnoreCase))
    {
      ShutdownMode = ShutdownMode.OnExplicitShutdown;
      try
      {
        string executablePath = Environment.ProcessPath
          ?? throw new InvalidOperationException("Не удалось определить путь к обновлению.");
        using Core.OpenedUpdatePackage package = await Core.UpdatePackageReader.OpenAsync(executablePath);
        Shutdown(0);
      }
      catch
      {
        Shutdown(2);
      }

      return;
    }

    new MainWindow().Show();
  }
}
