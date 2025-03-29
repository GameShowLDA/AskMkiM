using System.Windows;
using System.Windows.Input;
using AppConfiguration.Base;
using AppConfiguration.Execution;
using AppConfiguration.MeasurementError;
using AppConfiguration.Protocol;
using AppConfiguration.Theme;
using static Utilities.LoggerUtility;

namespace TestWPF
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    public MainWindow()
    {
      Task.Run(async () =>
      {
        try
        {
          await StartConfigAsync();
        }
        catch (InvalidOperationException exception)
        {
          LogError($"Ошибка загрузки темы программы: {exception}");
          return;
        }
        catch (Exception ex)
        {
          LogError($"Ошибка выполнения программы: {ex}");
        }
      }).Wait();

      InitializeComponent();
    }

    private async Task StartConfigAsync()
    {
      try
      {
        var executionTask = ExecutionSettingsManager.ReadExecutionModeAsync();
        var protocolTask = ProtocolSettingsManager.ReadProtocolModeAsync();
        var measurementErrorTask = MeasurementErrorSettingsManager.ReadMeasurementErrorMode();
        var db = DataBaseConfiguration.Configurations.DataBaseConfig.InitializeDB();

        await Task.WhenAll(executionTask, protocolTask, measurementErrorTask, db);
        await ThemeSettingsManager.ReadThemeModeAsync();
      }
      catch (Exception ex)
      {
        var stackTrace = new System.Diagnostics.StackTrace();
        var callingFrame = stackTrace.GetFrame(1);
        var method = callingFrame.GetMethod();
        var className = method.DeclaringType.FullName;
        var methodName = method.Name;

        LogError($"Ошибка в методе {className}.{methodName}: {ex.Message}");
      }
    }


    private void Button_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      new UI.Controls.Search.SearchWindow().ShowDialog();
    }
  }
}