using AppConfiguration.Execution;
using AppConfiguration.MeasurementError;
using AppConfiguration.Protocol;
using System.Windows;
using static AppConfiguration.Base.EventAggregator;
using static Utilities.LoggerUtility;
using AppConfiguration.Theme;
using DataBaseConfiguration;

namespace MainWindowProgram.Engine
{
  internal class ApplicationInitializer
  {
    private readonly MessageHandler messageHandler;

    /// <summary>
    /// Конструктор инициализатора приложения.
    /// </summary>
    /// <param name="messageHandler">Обработчик сообщений, используемый для отображения ошибок, предупреждений и информации.</param>
    public ApplicationInitializer(MessageHandler messageHandler)
    {
      this.messageHandler = messageHandler;
    }

    /// <summary>
    /// Запускает полную процедуру инициализации приложения.
    /// </summary>
    public async Task InitializeAsync()
    {
      CheckStatusProgram();
      await StartSettingsAsync();
    }

    /// <summary>
    /// Проверяет, запущен ли уже экземпляр приложения, и предотвращает запуск нескольких экземпляров.
    /// </summary>
    private void CheckStatusProgram()
    {
      bool isNewInstance;
      var mutex = new Mutex(true, "AxionHolding", out isNewInstance);

      if (!isNewInstance)
      {
        MessageBox.Show("Вы не можете запускать несколько экземпляров от Axion Holding. Это реализовано, чтобы избежать перегрузку оборудования АСК-МКИ-М!",
            "ВНИМАНИЕ!", MessageBoxButton.OK, MessageBoxImage.Information);
        LogWarning("Попытка запустить несколько экземпляров.");
        Application.Current.Shutdown();
      }
    }

    /// <summary>
    /// Выполняет асинхронную настройку приложения, загружает настройки темы и регистрирует обработчики событий для сообщений.
    /// </summary>
    private async Task StartSettingsAsync()
    {
      try
      {
        var executionTask = ExecutionSettingsManager.ReadExecutionModeAsync();
        var protocolTask = ProtocolSettingsManager.ReadProtocolModeAsync();
        var db = DataBaseConfig.InitializeDB();

        await Task.WhenAll(executionTask, protocolTask, db);
        await ThemeSettingsManager.ReadThemeModeAsync();
      }
      catch (Exception ex)
      {
        LogException(ex);
      }

      ErrorMessageEvent += messageHandler.SetErrorMessage;
      WarningMessageEvent += messageHandler.SetWarningMessage;
      InfoMessageEvent += messageHandler.SetInfoMessage;
      LogInformation("Настройки инициализированы.");
    }
  }
}
