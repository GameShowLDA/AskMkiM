using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities
{
  /// <summary>
  /// Интерфейс для проверки проводимости (прозвонки).
  /// </summary>
  public interface IContinuityMeasurement
  {
    /// <summary>
    /// Устанавливает режим прозвонки.
    /// </summary>
    Task<bool> SetContinuityModeAsync(IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Проверяет наличие проводимости.
    /// </summary>
    Task<bool> CheckContinuityAsync(bool expectedOutcome, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Проверяет наличие проводимости.
    /// </summary>
    Task<double> CheckContinuityAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1, IUserInteractionService? userMessageService = null);
  }
}
