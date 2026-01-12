using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities
{
  /// <summary>
  /// Интерфейс для режимов, поддерживающих установку и получение параметра напряжения.
  /// </summary>
  public interface IVoltageConfigurable
  {
    /// <summary>
    /// Устанавливает напряжение и проверяет, что устройство приняло значение.
    /// </summary>
    /// <param name="value">
    /// Значение напряжения в вольтах, которое требуется задать устройству.
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
    Task<(bool Success, string Message)> SetVoltageAsync(double value, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Получает установленное значение напряжения с устройства.
    /// </summary>
    /// <returns>
    /// Напряжение в киловольтах, считанное с устройства.  
    /// В случае ошибок может возвращаться 0 или иное значение по умолчанию,
    /// в зависимости от реализации.
    /// </returns>
    Task<double> GetVoltageAsync();
  }
}
