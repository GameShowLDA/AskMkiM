using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities
{
  /// <summary>
  /// Интерфейс для режимов, поддерживающих установку и получение частоты испытаний.
  /// </summary>
  public interface IFrequencyConfigurable
  {
    /// <summary>
    /// Устанавливает частоту испытаний и проверяет,
    /// что устройство приняло значение.
    /// </summary>
    /// <param name="frequency">
    /// Частота в герцах, допустимые значения: 50 или 60 Гц.
    /// </param>
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
    Task<(bool Success, string Message)> SetFrequencyAsync(int frequency, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Получает установленное значение частоты испытаний с устройства.
    /// </summary>
    /// <returns>
    /// Частота в герцах.  
    /// В случае ошибок может возвращаться 0,
    /// в зависимости от реализации.
    /// </returns>
    Task<int> GetFrequencyAsync();
  }
}
