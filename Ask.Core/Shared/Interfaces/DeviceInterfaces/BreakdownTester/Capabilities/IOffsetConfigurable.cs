using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities
{
  /// <summary>
  /// Интерфейс для режимов, поддерживающих установку и получение параметра смещения измерений (Offset).
  /// </summary>
  public interface IOffsetConfigurable
  {
    /// <summary>
    /// Устанавливает смещение и проверяет, что устройство приняло значение.
    /// </summary>
    /// <param name="value">
    /// Значение смещения в миллиамперах (мА), которое требуется задать устройству.
    /// </param>
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
    Task<(bool Success, string Message)> SetOffsetAsync(double value, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Получает текущее установленное смещение (Offset) с устройства.
    /// </summary>
    /// <returns>
    /// Смещение в миллиамперах (мА), считанное с устройства.  
    /// В случае ошибок может возвращаться 0 или иное значение по умолчанию,
    /// в зависимости от реализации.
    /// </returns>
    Task<double> GetOffsetAsync();
  }
}
