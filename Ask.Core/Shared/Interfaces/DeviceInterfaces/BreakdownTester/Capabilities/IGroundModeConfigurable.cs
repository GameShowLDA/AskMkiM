using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities
{
  /// <summary>
  /// Интерфейс для режимов, поддерживающих включение и выключение земли.
  /// </summary>
  public interface IGroundModeConfigurable
  {
    /// <summary>
    /// Включает или выключает землю и проверяет,
    /// что устройство приняло новое состояние.
    /// </summary>
    /// <param name="state">Новое состояние земли: <c>true</c> — ON, <c>false</c> — OFF.</param>
    /// <param name="userMessageService">
    /// (Необязательно) Сервис для отображения сообщений пользователю.
    /// Может быть <c>null</c>, если вывод сообщений не требуется.
    /// </param>
    /// <returns>
    /// Кортеж:
    /// <list type="bullet">
    ///   <item><description><c>bool Success</c> — признак успешного выполнения операции.</description></item>
    ///   <item><description><c>string Message</c> — сообщение об ошибке, если установка не удалась.</description></item>
    /// </list>
    /// </returns>
    Task<(bool Success, string Message)> SetGroundModeAsync(bool state, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Получает текущее состояние земли с устройства.
    /// </summary>
    /// <returns><c>true</c>, если земля включена; иначе <c>false</c>.</returns>
    Task<bool> GetGroundModeAsync();
  }
}
