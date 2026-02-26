using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities
{
  /// <summary>
  /// Интерфейс для измерения ёмкости.
  /// </summary>
  public interface ICapacitanceMeasurement
  {
    /// <summary>
    /// Устанавливает режим измерения ёмкости.
    /// </summary>
    Task<bool> SetCapacitanceModeAsync(IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Выполняет измерение ёмкости.
    /// </summary>
    /// <param name="param">Ожиданемео знчение.</param>
    Task<double> MeasureCapacitanceAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1, IUserInteractionService? userMessageService = null);
  }
}
