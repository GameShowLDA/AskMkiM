using NewCore.Base.Device;

namespace NewCore.Base.Interface.Additionally
{
  public interface IAttachableDevice : IDevice
  {
    /// <summary>
    /// Номер менеджера шасси.
    /// </summary>
    public int NumberChassis { get; set; }
  }
}
