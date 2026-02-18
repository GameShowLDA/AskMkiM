using Ask.Core.Shared.Metadata.Enums.UiEnums;
using MainWindowProgram.Test.Protocol;
using System.Windows;
using UI.Controls.AdminPanel;
using UI.Controls.DeviceHealthView;
using UI.Controls.GPT;
using UI.Controls.Settings.Protocol;
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
    public void OpenGptServiceAsync() => _multiWindow.WorkspaceService.AddControl("GptManagement", new GPTPunchControl(), TypeWindow.DeviceControl);

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

    public async Task StartConsoleTest() => await Test.ConsoleTest.TestData.PrintTestData();

    public void AdminPanel() => _multiWindow.WorkspaceService.AddControl("Панель администратора", new AdminPanelControl(), TypeWindow.Settings);
    public void ProtocolTest() => _multiWindow.WorkspaceService.AddControl("Тест протокола", new TestProtocol(), TypeWindow.DeviceControl);
    public void ProtocolBaseTest() => _multiWindow.WorkspaceService.AddControl("Тест теста протокола", new ProtocolTemplateEditorControl(), TypeWindow.DeviceControl);
  }
}
