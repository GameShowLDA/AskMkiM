using Ask.Core.Shared.Metadata.Enums.UiEnums;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.DataBase.Engine.Static.Devices;
using Ask.UI.Features.ServiceTools.Gpt;
using Ask.UI.Features.ServiceTools.SwitchingDevice;
using MainWindowProgram.Test.Protocol;
using UI.Controls.AdminPanel;
using UI.Controls.DeviceHealthView;
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
    /// Инициализирует новый экземпляр класса <see cref="AdminServices"/>.
    /// </summary>
    /// <param name="mainWindow">Главное окно приложения.</param>
    /// <param name="multiWindow">Сервис управления многооконным интерфейсом.</param>
    public AdminServices(MainWindow mainWindow, MultiWindowService multiWindow)
    {
      _multiWindow = multiWindow;
    }

    /// <summary>
    /// Открывает элемент управления для работы с программируемой пробойной установкой (ППУ).
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    public void OpenGptServiceAsync() =>
      _multiWindow.WorkspaceService.AddControl(
        "GptManagement",
        new GPTPunchControl(GetGptAsync),
        TypeWindow.DeviceControl);

    public async Task StartConsoleTest() => await Test.ConsoleTest.TestData.PrintTestData();

    /// <summary>
    /// Открывает сервисные утилиты в отдельной вкладке рабочего пространства.
    /// </summary>
    public void OpenServiceUtilities() =>
      _multiWindow.WorkspaceService.AddControl(
        "Сервисные утилиты",
        new ServiceUtilitiesControl(GetGptAsync, GetSwitchingDeviceAsync),
        TypeWindow.Settings);

    /// <summary>
    /// Возвращает пробойную установку, настроенную для первого шасси.
    /// </summary>
    /// <returns>Найденная пробойная установка или <see langword="null"/>.</returns>
    private static async Task<IBreakdownTester?> GetGptAsync()
    {
      return (await BreakdownTesters.GetDevicesByNumberChassisAsync(1))
        .FirstOrDefault();
    }

    /// <summary>
    /// Возвращает устройство коммутации шин, настроенное для первого шасси.
    /// </summary>
    /// <returns>Найденное устройство коммутации шин или <see langword="null"/>.</returns>
    private static async Task<ISwitchingDevice?> GetSwitchingDeviceAsync()
    {
      return (await SwitchingDevices.GetDevicesByNumberChassisAsync(1))
        .FirstOrDefault();
    }

    /// <summary>
    /// Открывает административный интерфейс базы данных в отдельной вкладке рабочего пространства.
    /// </summary>
    public void OpenDatabase() =>
      _multiWindow.WorkspaceService.AddControl(
        "База данных",
        new DataBaseView(),
        TypeWindow.Settings);

    /// <summary>
    /// Открывает настройку сопротивления МКР в отдельной вкладке рабочего пространства.
    /// </summary>
    public void OpenResistance() =>
      _multiWindow.WorkspaceService.AddControl(
        "Сопротивление МКР",
        new CheckResistanceControl(),
        TypeWindow.Settings);

    public void ProtocolTest() => _multiWindow.WorkspaceService.AddControl("Тест протокола", new TestProtocol(), TypeWindow.DeviceControl);
    public void ProtocolBaseTest() => _multiWindow.WorkspaceService.AddControl("Тест теста протокола", new ProtocolTemplateEditorControl(), TypeWindow.DeviceControl);
  }
}
