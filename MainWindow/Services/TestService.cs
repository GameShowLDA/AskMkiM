using MainWindowProgram.Infrastructure;
using Mode.TestSuite.CrossTestMkr;
using Mode.TestSuite.Metrology.MethodExecutor.CI;
using Mode.TestSuite.Metrology.MethodExecutor.PI;
using Mode.TestSuite.Metrology.NodeMethod.CI;
using Mode.TestSuite.Metrology.NodeMethod.PI;
using System.Windows.Input;
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
    public async Task AddCiNodeMethodControlAsync() =>
      _multiWindow.AddControlAsync("Метод узла СИ", new CiNodeMethodControl(), TypeWindow.DeviceControl);

    /// <summary>
    /// Добавляет элемент управления для теста методом узла ПИ(DCW) в multiEditors.
    /// </summary>
    public async Task AddPiDCWNodeMethodControlAsync() =>
      _multiWindow.AddControlAsync("Метод узла ПИ(DCW)", new PiDCWNodeMethodControl(), TypeWindow.DeviceControl);

    /// <summary>
    /// Добавляет элемент управления для теста методом узла ПИ(ACW) в multiEditors.
    /// </summary>
    public async Task AddPiACWNodeMethodControlAsync() =>
      _multiWindow.AddControlAsync("Метод узла ПИ(ACW)", new PiACWNodeMethodControl(), TypeWindow.DeviceControl);

    /// <summary>
    /// Добавляет элемент управления для теста групповым методом СИ в multiEditors.
    /// </summary>
    public async Task AddCiMethodExecutorControlAsync() =>
       _multiWindow.AddControlAsync("Групповой метод СИ", new CiMethodExecutor(), TypeWindow.DeviceControl);

    /// <summary>
    /// Добавляет элемент управления для теста групповым методом ПИ(ACW) в multiEditors.
    /// </summary>
    public async Task AddPiACWMethodExecutorControlAsync() =>
       _multiWindow.AddControlAsync("Групповой метод ПИ(ACW)", new PiACWMethodExecutorControl(), TypeWindow.DeviceControl);

    /// <summary>
    /// Добавляет элемент управления для теста групповым методом ПИ(DCW) в multiEditors.
    /// </summary>
    public async Task AddPiDCWMethodExecutorControlAsync() =>
       _multiWindow.AddControlAsync("Групповой метод ПИ(DCW)", new PiDCWMethodExecutorControl(), TypeWindow.DeviceControl);

    /// <summary>
    /// Добавляет элемент управления для перекрёстного теста МКР в multiEditors.
    /// </summary>
    public async Task AddCrossTestMkrExecutorControlAsync() =>
       _multiWindow.AddControlAsync("Перекрёстный тест МКР", new CrossTestMkrControl(), TypeWindow.DeviceControl);
  }
}
