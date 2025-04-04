using System.Windows;
using System.Windows.Controls;
using static AppConfiguration.SystemState.SystemStateManager;
using static AppConfiguration.Base.EventAggregator;
using static Utilities.LoggerUtility;
using AppConfiguration.Execution;
using AppConfiguration.MeasurementError;
using AppConfiguration.Protocol;
using AppConfiguration.Theme;
using static Utilities.LoggerUtility;
using static UI.Components.Invoke.OpenFileButton;

namespace MainWindowProgram
{
  public partial class MainWindow
  {
    /// <summary>
    /// Инициализация приложения.
    /// </summary>
    private async Task Initialize()
    {
      CheckStatusProgram();
      await StartSettings();
      RegisterHotkeys();
    }

    /// <summary>
    /// Проверяет, запущен ли уже экземпляр приложения, и предотвращает запуск нескольких экземпляров.
    /// </summary>
    private void CheckStatusProgram()
    {
      bool isNewInstance;
      var _mutex = new Mutex(true, "AxionHolding", out isNewInstance);

      if (!isNewInstance)
      {
        MessageBox.Show("Вы не можете запускать несколько экземпляров от Axion Holding. Это реализовано, чтобы избежать перегрузку оборудования АСК-МКИ-М!", "ВНИМАНИЕ!", MessageBoxButton.OK, MessageBoxImage.Information);
        LogWarning("Попытка запустить несколько экземпляров.");
        Application.Current.Shutdown();
        return;
      }
    }

    /// <summary>
    /// Выполняет асинхронную настройку приложения, загружает настройки темы и регистрирует обработчики событий для сообщений.
    /// </summary>
    private async Task StartSettings()
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

      ErrorMessageEvent += messageHandler.SetErrorMessage;
      WarningMessageEvent += messageHandler.SetWarningMessage;
      InfoMessageEvent += messageHandler.SetInfoMessage;
      LogInformation("Настройки инициализированы.");
    }

  }
}
