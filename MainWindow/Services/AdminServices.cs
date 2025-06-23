using System.Windows;
using MainWindowProgram.Test.ChoiceDevice;
using MainWindowProgram.Test.Protocol;
using Mode.SelfControl.DeviceCheck;
using Mode.Settings.LoggerMessage;
using Mode.Settings.SendCommand;
using UI.Controls.GPT;
using UI.Controls.MeasurementError;
using static UI.Components.Invoke.OpenFileButton;

namespace MainWindowProgram.Services
{
  /// <summary>
  /// Реализация административных сервисов, предоставляющих доступ к управлению ППУ, логами, отправке команд и работе с USB.
  /// </summary>
  public class AdminServices
  {
    /// <summary>
    /// Сервис для управления многооконным интерфейсом.
    /// </summary>
    private readonly MultiWindowService _multiWindow;

    /// <summary>
    /// Ссылка на главное окно приложения.
    /// </summary>
    private readonly MainWindow _mainWindow;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="AdminServices"/>.
    /// </summary>
    /// <param name="mainWindow">Главное окно приложения.</param>
    /// <param name="multiWindow">Сервис управления многооконным интерфейсом.</param>
    public AdminServices(MainWindow mainWindow, MultiWindowService multiWindow)
    {
      _multiWindow = multiWindow;
      _mainWindow = mainWindow;
    }

    /// <summary>
    /// Открывает элемент управления для работы с программируемой пробойной установкой (ППУ).
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    public async Task OpenGptServiceAsync() =>
        await _multiWindow.AddControlAsync("GptManagement", new GPTPunchControl(), TypeWindow.DeviceControl);

    /// <summary>
    /// Открывает элемент управления для просмотра и анализа логов приложения.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    public async Task OpenLogger() =>
        await _multiWindow.AddControlAsync("Logger", new LoggerControl(), TypeWindow.DeviceControl);

    /// <summary>
    /// Открывает элемент управления для отправки произвольной команды на устройство.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    public async Task OpenSendCommand() =>
        await _multiWindow.AddControlAsync("Send Command", new SendCommandControl(), TypeWindow.DeviceControl);

    /// <summary>
    /// Открывает элемент управления для работы с USB-устройствами (например, флешками).
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    public async Task OpenUsbServiceAsync()
    {
      _mainWindow.Effect = new System.Windows.Media.Effects.BlurEffect();

      await Application.Current.Dispatcher.InvokeAsync(() =>
      {
        var keyManagementWindow = new KeyManagementWindow();
        keyManagementWindow.ShowDialog();
      });

      _mainWindow.Effect = null;
    }


    /// <summary>
    /// Открывает пользовательский элемент управления с настройками погрешностей выполнения режимов.
    /// </summary>
    /// <returns>Задача, представляющая операцию открытия интерфейса погрешностей.</returns>
    public async Task OpenErrorSync() =>
      await _multiWindow.AddControlAsync("Погрешности измерений", new MeasurementErrorControl(), TypeWindow.Settings);

    public async Task StartConsoleTest() =>
     await Test.ConsoleTest.TestData.PrintTestData();

    public async Task ProtocolTest()
    { 
      await _multiWindow.AddControlAsync("Тест протокола", new TestProtocol(), TypeWindow.DeviceControl);
    }

    public async Task ChoiceTest()
    {
      await _multiWindow.AddControlAsync("Тест выбора", new TestChoiceDevice(), TypeWindow.DeviceControl);
    }

    public async Task SelfCheckTest()
    {
      await _multiWindow.AddControlAsync("Самоконтроль", new ModuleSelfControl(), TypeWindow.DeviceControl);
    }
  }
}
