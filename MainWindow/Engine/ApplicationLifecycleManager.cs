using MainWindowProgram.Events;
using MainWindowProgram.Services;
using MainWindowProgram.ViewModels;

namespace MainWindowProgram.Engine
{
  /// <summary>
  /// Отвечает за инициализацию и связывание всех событий жизненного цикла приложения.
  /// </summary>
  /// <remarks>
  /// Класс <see cref="ApplicationLifecycleManager"/> выполняет центральную настройку событий приложения, 
  /// создавая и объединяя специализированные биндеры событий:
  /// <list type="bullet">
  /// <item>
  /// <description><see cref="SystemEventsBinder"/> — обрабатывает системные события, связанные с запуском и завершением работы приложения, изменением системных состояний и др.</description>
  /// </item>
  /// <item>
  /// <description><see cref="UiEventsBinder"/> — управляет событиями пользовательского интерфейса, включая взаимодействие с главным окном, многооконным редактором и строкой состояния.</description>
  /// </item>
  /// <item>
  /// <description><see cref="StateEventsBinder"/> — отвечает за события состояния устройств и служб, включая работу с USB-сервисом и внутренними состояниями приложения.</description>
  /// </item>
  /// </list>
  /// После инициализации всех биндерах вызывается метод <see cref="ApplicationEventsBinder.BindAll"/>, 
  /// который выполняет привязку событий к соответствующим обработчикам.
  /// </remarks>
  internal class ApplicationLifecycleManager
  {
    /// <summary>
    /// Экземпляр агрегатора событий приложения, объединяющий все группы биндеров.
    /// </summary>
    internal static ApplicationEventsBinder ApplicationEvents;

    /// <summary>
    /// Инициализирует обработчики событий приложения и связывает их с соответствующими биндер-классами.
    /// </summary>
    /// <param name="window">Главное окно приложения, к которому привязываются события пользовательского интерфейса.</param>
    /// <param name="usb">Сервис управления подключениями USB-устройств, используемый для обработки аппаратных событий.</param>
    /// <param name="statusBarViewModel">Модель представления строки состояния, используемая для отображения текущего состояния в UI.</param>
    /// <remarks>
    /// Метод создаёт экземпляр <see cref="ApplicationEventsBinder"/>, в который передаются все основные биндеры событий:
    /// <see cref="SystemEventsBinder"/>, <see cref="UiEventsBinder"/>, <see cref="StateEventsBinder"/>.
    /// После создания агрегатора вызывается метод <see cref="ApplicationEventsBinder.BindAll"/>, 
    /// обеспечивающий привязку всех событий к их обработчикам.
    /// </remarks>
    public void Initialize(MainWindow window, UsbServices usb, TextEditorStatusViewModel statusBarViewModel)
    {
      ApplicationEvents = new ApplicationEventsBinder(
        new SystemEventsBinder(),
        new UiEventsBinder(window, window.MultiWindow, statusBarViewModel),
        new StateEventsBinder(window, usb)
      );

      ApplicationEvents.BindAll();
    }
  }
}
