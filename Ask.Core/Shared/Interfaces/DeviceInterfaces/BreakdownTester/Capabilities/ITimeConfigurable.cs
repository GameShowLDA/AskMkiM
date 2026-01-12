using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities
{
  public interface ITimeConfigurable
  {
    /// <summary>
    /// Устанавливает время теста и проверяет, что устройство приняло значение.
    /// </summary>
    /// <param name="value">
    /// Значение времени в секундах, которое требуется задать устройству.
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
    Task<(bool Success, string Message)> SetTestTimeAsync(double value, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Получает текущее установленное время теста с устройства.
    /// </summary>
    /// <returns>
    /// Время теста в секундах, считанное с устройства.  
    /// В случае ошибок может возвращаться 0 или иное значение по умолчанию,
    /// в зависимости от реализации.
    /// </returns>
    Task<double> GetTestTimeAsync();

    /// <summary>
    /// Устанавливает время нарастания напряжения (Ramp Time) и проверяет, что устройство приняло значение.
    /// </summary>
    /// <param name="value">
    /// Время нарастания в секундах (обычно в диапазоне 0.1 – 999.9).
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
    Task<(bool Success, string Message)> SetRampTimeAsync(double value, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Получает текущее установленное время нарастания напряжения (Ramp Time).
    /// </summary>
    /// <returns>
    /// Значение времени нарастания напряжения в секундах.  
    /// В случае ошибок может возвращаться 0 или иное значение по умолчанию,
    /// в зависимости от реализации.
    /// </returns>
    Task<double> GetRampTimeAsync();
  }
}
