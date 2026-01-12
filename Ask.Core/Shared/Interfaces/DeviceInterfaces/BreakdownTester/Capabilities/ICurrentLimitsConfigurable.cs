using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities
{
  /// <summary>
  /// Интерфейс для режимов, поддерживающих установку и получение пределов тока (верхнего и нижнего).
  /// </summary>
  public interface ICurrentLimitsConfigurable
  {
    /// <summary>
    /// Устанавливает верхний предел тока и проверяет, что устройство приняло значение.
    /// </summary>
    /// <param name="value">
    /// Значение верхнего предела в миллиамперах (мА), которое требуется задать устройству.
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
    Task<(bool Success, string Message)> SetHighCurrentLimitAsync(double value, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Получает текущее установленное значение верхнего предела тока.
    /// </summary>
    /// <returns>
    /// Ток в миллиамперах (мА), считанный с устройства.  
    /// В случае ошибок может возвращаться 0 или иное значение по умолчанию,
    /// в зависимости от реализации.
    /// </returns>
    Task<double> GetHighCurrentLimitAsync();

    /// <summary>
    /// Устанавливает нижний предел тока и проверяет, что устройство приняло значение.
    /// </summary>
    /// <param name="value">
    /// Значение нижнего предела в миллиамперах (мА), которое требуется задать устройству.
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
    Task<(bool Success, string Message)> SetLowCurrentLimitAsync(double value, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Получает текущее установленное значение нижнего предела тока.
    /// </summary>
    /// <returns>
    /// Ток в миллиамперах (мА), считанный с устройства.  
    /// В случае ошибок может возвращаться 0 или иное значение по умолчанию,
    /// в зависимости от реализации.
    /// </returns>
    Task<double> GetLowCurrentLimitAsync();
  }
}
