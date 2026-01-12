using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces
{
  /// <summary>
  /// Определяет интерфейс для компонентов, требующих инициализации,
  /// подключения и отключения.
  /// </summary>
  public interface IConnectable
  {
    /// <summary>
    /// Событие, которое вызывается при изменении состояния питания.
    /// </summary>
    event Action IsReset;

    /// <summary>
    /// Выполняет инициализацию компонента, подготавливая его к подключению.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию. Возвращает <c>true</c>, если инициализация прошла успешно; иначе — <c>false</c>.</returns>
    Task<(bool Connect, string Answer)> InitializeAsync(IUserInteractionService userMessageService = null);

    /// <summary>
    /// Выполняет подключение к компоненту.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию. Возвращает <c>true</c>, если подключение прошло успешно; иначе — <c>false</c>.</returns>
    Task<(bool Connect, string Answer)> ConnectAsync(IUserInteractionService userMessageService = null);

    /// <summary>
    /// Выполняет отключение от компонента.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию. Возвращает <c>true</c>, если отключение прошло успешно; иначе — <c>false</c>.</returns>
    Task<bool> DisconnectAsync(IUserInteractionService userMessageService = null);

    /// <summary>
    /// Выполняет сброс устройства.
    /// </summary>
    /// <returns></returns>
    Task<bool> ResetAsync(IUserInteractionService userMessageService = null);
  }
}
