namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule.Capabilities;

/// <summary>
/// Allows a point manager to rebuild its state for a changed channel count.
/// </summary>
public interface IPointCountReconfigurable
{
  void ReconfigurePointCount(int pointCount);
}
