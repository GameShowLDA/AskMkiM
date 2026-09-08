using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Mode
{
  public interface ILimitManager
  {
    /// <summary>
    /// Устанавливает верхний предел и проверяет, что устройство приняло заданное значение.
    /// </summary>
    /// <param name="value">
    /// Значение верхнего предела, которое требуется установить.
    /// </param>
    /// <param name="userMessageService">
    /// Необязательный сервис для отображения сообщений пользователю.
    /// Может быть <c>null</c>, если вывод сообщений не требуется.
    /// </param>
    /// <returns>
    /// Кортеж, содержащий признак успешного выполнения операции и сообщение об ошибке.
    /// </returns>
    Task<(bool Success, string Message)> SetHighLimitAsync(
        double value,
        IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Получает текущее установленное значение верхнего предела.
    /// </summary>
    /// <returns>
    /// Текущее значение верхнего предела, считанное с устройства.
    /// </returns>
    Task<double> GetHighLimitAsync();

    /// <summary>
    /// Устанавливает нижний предел и проверяет, что устройство приняло заданное значение.
    /// </summary>
    /// <param name="value">
    /// Значение нижнего предела, которое требуется установить.
    /// </param>
    /// <param name="userMessageService">
    /// Необязательный сервис для отображения сообщений пользователю.
    /// Может быть <c>null</c>, если вывод сообщений не требуется.
    /// </param>
    /// <returns>
    /// Кортеж, содержащий признак успешного выполнения операции и сообщение об ошибке.
    /// </returns>
    Task<(bool Success, string Message)> SetLowLimitAsync(
        double value,
        IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Получает текущее установленное значение нижнего предела.
    /// </summary>
    /// <returns>
    /// Текущее значение нижнего предела, считанное с устройства.
    /// </returns>
    Task<double> GetLowLimitAsync();
  }
}
