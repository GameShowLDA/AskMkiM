using System.Windows;
using System.Windows.Controls;
using static AppManager.Config.SystemStateManager;
using static AppManager.EventAggregator;
using static AppManager.SettingsFileReader;
using static Utilities.LoggerUtility;

namespace MainWindowProgram
{
  public partial class MainWindow
  {
    /// <summary>
    /// Инициализация приложения.
    /// </summary>
    private void Initialize()
    {
      CheckStatusProgram();
      StartSettings();
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
    private async void StartSettings()
    {
      await ReadAllSettingsAsync();

      ErrorMessageEvent += messageHandler.SetErrorMessage;
      WarningMessageEvent += messageHandler.SetWarningMessage;
      InfoMessageEvent += messageHandler.SetInfoMessage;

      timer.AutoReset = true;
      timer.Enabled = true;

      LogInformation("Настройки инициализированы.");
    }

    /// <summary>
    /// Добавляет пользовательские элементы управления в интерфейс, если приложение не заблокировано.
    /// </summary>
    private async Task AddControlAsync(UserControl userControl, string name)
    {
      if (await GetIsLocked())
      {
        await Dispatcher.InvokeAsync(() =>
        {
          MessageBox.Show("В данный момент идёт работа с аппаратурой! Пожалуйста завершите выполнение!", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
        });
      }
      else
      {
        await Dispatcher.InvokeAsync(() =>
        {
          multiEditors.AddControl(name, userControl);
        });
      }
    }
  }
}
