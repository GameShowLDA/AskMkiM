using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSupply.Capabilities
{
  /// <summary>
  /// Предоставляет управление включением и выключением выхода источника питания.
  /// </summary>
  public interface IOutputManager
  {
    /// <summary>
    /// Включает или выключает выход источника питания.
    /// </summary>
    /// <param name="enabled"><see langword="true"/> для включения выхода; <see langword="false"/> для выключения.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <param name="userMessageService">Сервис взаимодействия с пользователем.</param>
    /// <returns>Задача изменения состояния выхода.</returns>
    Task SetOutputStateAsync(bool enabled, CancellationToken cancellationToken = default, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Читает состояние выхода источника питания.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <param name="userMessageService">Сервис взаимодействия с пользователем.</param>
    /// <returns><see langword="true"/>, если выход включён; иначе — <see langword="false"/>.</returns>
    Task<bool> GetOutputStateAsync(CancellationToken cancellationToken = default, IUserInteractionService? userMessageService = null);
  }
}
