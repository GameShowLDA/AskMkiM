namespace Ask.Device.Runtime.Device.Multimeters;

/// <summary>
/// USB-мультиметр Agilent 34450A.
/// </summary>
public sealed class MultiAgilent34450A : MultiUsbSCPIMeterBase
{
  /// <summary>
  /// Создает описание USB-мультиметра Agilent 34450A.
  /// </summary>
  public MultiAgilent34450A()
  {
    ConfigureUsbMeter(
      "Agilent 34450A",
      "USB-SCPI мультиметр Agilent 34450A.",
      "VID_0000&PID_0000");

    ConfigureAgilentUsbCommands(supportsCapacitance: true);
  }
}
