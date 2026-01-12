using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities
{
  /// <summary>
  /// Интерфейс для режимов, поддерживающих установку и получение текущего режима работы устройства.
  /// </summary>
  public interface IModeConfigurable
  {
    /// <summary>
    /// Устанавливает режим работы устройства.
    /// </summary>
    /// <param name="userMessageService">
    /// (Необязательно) Сервис для отображения сообщений пользователю.  
    /// Может быть <c>null</c>, если сообщения выводить не требуется.
    /// </param>
    /// <returns>
    /// Кортеж:
    /// <list type="bullet">
    ///   <item><description><c>bool Success</c> — признак успешного выполнения операции.</description></item>
    ///   <item><description><c>string Message</c> — сообщение об ошибке, если установка не удалась.</description></item>
    /// </list>
    /// </returns>
    Task<(bool Success, string Message)> SetModeAsync(IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Получает текущий активный режим работы устройства.
    /// </summary>
    /// <returns>
    /// Кортеж:
    /// <list type="bullet">
    ///   <item><description><c>bool Success</c> — признак успешного выполнения операции.</description></item>
    ///   <item><description><c>string Message</c> — строковое представление текущего режима.</description></item>
    /// </list>
    /// </returns>
    Task<(bool Success, string Message)> GetModeAsync();
  }
}
