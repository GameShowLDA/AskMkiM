using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace Ask.Core.Shared.Interfaces.UiInterfaces
{
  /// <summary>
  /// Предоставляет безопасный доступ к значениям полей ввода,
  /// независимо от конкретной реализации пользовательского интерфейса.
  /// </summary>
  public interface IInputFieldAccessor
  {
    /// <summary>
    /// Возвращает основные значения полей ввода:
    /// первую точку, вторую точку и электрический параметр.
    /// </summary>
    (string First, string Second, string Parameter) GetValues();

    /// <summary>
    /// Возвращает значение времени выполнения операции.
    /// </summary>
    string GetTime();

    /// <summary>
    /// Возвращает значение времени нарастания (ramp).
    /// </summary>
    string GetTimeRamp();

    /// <summary>
    /// Возвращает значение напряжения.
    /// </summary>
    string GetVoltage();

    /// <summary>
    /// Возвращает активную шину (bus), выбранную пользователем.
    /// </summary>
    BusPoint GetBus();

    /// <summary>
    /// Возвращает активную пару шин (SwitchingBusNew), выбранную пользователем.
    /// </summary>
    SwitchingBusNew GetPairBus();
  }
}
