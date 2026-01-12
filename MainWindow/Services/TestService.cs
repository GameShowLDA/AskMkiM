using UI.Controls.ExecutorControls.TestsControls;
using UI.Controls.ExecutorControls.TestsControls.MethodExecutor.CI;
using UI.Controls.ExecutorControls.TestsControls.MethodExecutor.PI;
using UI.Controls.ExecutorControls.TestsControls.NodeMethod.CI;
using UI.Controls.ExecutorControls.TestsControls.NodeMethod.PI;
using static UI.Components.Invoke.OpenFileButton;

namespace MainWindowProgram.Services
{
  /// <summary>
  /// Реализация сервиса для добавления элементов управления для тестов.
  /// </summary>
  public class TestService
  {
    /// <summary>
    /// Сервис управления многооконным интерфейсом.
    /// </summary>
    private readonly MultiWindowService _multiWindow;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="TestService"/>.
    /// </summary>
    /// <param name="multiWindow">Сервис управления многооконным интерфейсом.</param>
    public TestService(MultiWindowService multiWindow)
    {
      _multiWindow = multiWindow;
    }

    /// <summary>
    /// Добавляет элемент управления для теста методом узла СИ в multiEditors.
    /// </summary>
    public void AddCiNodeMethodControlAsync() =>
      _multiWindow.AddControl("Метод узла СИ", new CiNodeMethodControl(), TypeWindow.DeviceControl);

    /// <summary>
    /// Добавляет элемент управления для теста методом узла ПИ(DCW) в multiEditors.
    /// </summary>
    public void AddPiDCWNodeMethodControlAsync() =>
      _multiWindow.AddControl("Метод узла ПИ(DCW)", new PiDCWNodeMethodControl(), TypeWindow.DeviceControl);

    /// <summary>
    /// Добавляет элемент управления для теста методом узла ПИ(ACW) в multiEditors.
    /// </summary>
    public void AddPiACWNodeMethodControlAsync() =>
      _multiWindow.AddControl("Метод узла ПИ(ACW)", new PiACWNodeMethodControl(), TypeWindow.DeviceControl);

    /// <summary>
    /// Добавляет элемент управления для теста групповым методом СИ в multiEditors.
    /// </summary>
    public void AddCiMethodExecutorControlAsync() =>
       _multiWindow.AddControl("Групповой метод СИ", new CiMethodExecutor(), TypeWindow.DeviceControl);

    /// <summary>
    /// Добавляет элемент управления для теста групповым методом ПИ(ACW) в multiEditors.
    /// </summary>
    public void AddPiACWMethodExecutorControlAsync() =>
       _multiWindow.AddControl("Групповой метод ПИ(ACW)", new PiACWMethodExecutorControl(), TypeWindow.DeviceControl);

    /// <summary>
    /// Добавляет элемент управления для теста групповым методом ПИ(DCW) в multiEditors.
    /// </summary>
    public void AddPiDCWMethodExecutorControlAsync() =>
       _multiWindow.AddControl("Групповой метод ПИ(DCW)", new PiDCWMethodExecutorControl(), TypeWindow.DeviceControl);

    /// <summary>
    /// Добавляет элемент управления для перекрёстного теста МКР в multiEditors.
    /// </summary>
    public void AddCrossTestMkrExecutorControlAsync() =>
       _multiWindow.AddControl("Перекрёстный тест МКР", new CrossConnectionControl(), TypeWindow.DeviceControl);

    /// <summary>
    /// Добавляет элемент управления для контроля сопротивления контактов реле коммутатора в multiEditors.
    /// </summary>
    public async Task AddRelayContactResistExecutorControlAsync() =>
      _multiWindow.AddControl("Контроль сопротивления контактов реле коммутатора", new RkommConnectionControl(), TypeWindow.DeviceControl);
  }
}
