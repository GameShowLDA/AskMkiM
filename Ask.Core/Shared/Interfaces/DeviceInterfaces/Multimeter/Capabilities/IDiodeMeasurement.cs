using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities
{
  /// <summary>
  /// Интерфейс для работы мультиметра в режиме проверки диода.
  /// </summary>
  public interface IDiodeMeasurement
  {
    /// <summary>
    /// Устанавливает режим проверки диода.
    /// </summary>
    Task<bool> SetDiodeModeAsync(IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Проверяет диод и возвращает измеренное падение напряжения.
    /// </summary>
    /// <param name="param">Ожидаемое значение.</param>
    Task<double> CheckDiodeAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1, IUserInteractionService? userMessageService = null);
  }
}
