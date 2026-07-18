namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule.Capabilities;

/// <summary>
/// Предоставляет изменение количества точек модуля коммутации реле.
/// </summary>
public interface IPointCountReconfigurable
{
  /// <summary>
  /// Изменяет количество точек модуля коммутации реле.
  /// </summary>
  /// <param name="pointCount">Количество точек.</param>
  void ReconfigurePointCount(int pointCount);
}
