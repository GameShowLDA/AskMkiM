using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis.Capabilities

{
  /// <summary>
  /// Интерфейс для управления питанием шасси.
  /// </summary>
  public interface IPowerManagerChassis
  {
    /// <summary>
    /// Отключает питание шасси.
    /// </summary>
    /// <returns>Асинхронная задача.</returns>
    Task StopPowerAsync(IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Включает питание шасси.
    /// </summary>
    /// <returns>Асинхронная задача.</returns>
    Task StartPowerAsync(IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Асинхронно проверяет наличие и корректность питания системы.
    /// </summary>
    /// <param name="userMessageService">
    /// Сервис пользовательского взаимодействия, используемый для отображения сообщений (опционально).
    /// </param>
    /// <returns>
    /// <see langword="true"/>, если питание присутствует и соответствует требованиям;  
    /// <see langword="false"/> — если питание отсутствует или некорректно.
    /// </returns>
    Task<bool> VerifyPowerAsync(IUserInteractionService? userMessageService = null);
  }
}
