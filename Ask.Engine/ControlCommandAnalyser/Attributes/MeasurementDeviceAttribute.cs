using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace Ask.Engine.ControlCommandAnalyser.Attributes
{
  /// <summary>
  /// Атрибут, указывающий, какой измерительный прибор используется командой.
  /// Если атрибут отсутствует — считается MeasurementDevice.None.
  /// </summary>
  [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
  public class MeasurementDeviceAttribute : Attribute
  {
    /// <summary>
    /// Тип прибора, используемый командой.
    /// </summary>
    public MeasurementDevice Device { get; }

    public MeasurementDeviceAttribute(MeasurementDevice device)
    {
      Device = device;
    }
  }
}
