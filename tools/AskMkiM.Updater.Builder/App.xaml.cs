using System.IO;
using System.Windows;

namespace AskMkiM.Updater.Builder;

/// <summary>
/// Запускает конструктор автономных обновлений AskMkiM.
/// </summary>
public partial class App : Application
{
  /// <inheritdoc />
  protected override async void OnStartup(StartupEventArgs e)
  {
    base.OnStartup(e);
    if (e.Args.Length == 4 && string.Equals(e.Args[0], "--build", StringComparison.OrdinalIgnoreCase))
    {
      ShutdownMode = ShutdownMode.OnExplicitShutdown;
      try
      {
        await new Core.UpdatePackageBuilder().BuildAsync(e.Args[1], e.Args[2], e.Args[3]);
        Shutdown(0);
      }
      catch (Exception ex)
      {
        try
        {
          File.WriteAllText(e.Args[3] + ".error.log", ex.ToString());
        }
        catch
        {
          // Код завершения остаётся основным способом сообщить об ошибке автоматизации.
        }

        Shutdown(2);
      }

      return;
    }

    new MainWindow().Show();
  }
}
