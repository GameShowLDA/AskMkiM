namespace Ask.Device.Runtime.Device.Multimeters;

/// <summary>
/// USB-мультиметр Tektronix DMM4050.
/// </summary>
public sealed class MultiDmm4050 : MultiUsbSCPIMeterBase
{
  /// <summary>
  /// Создает описание USB-мультиметра Tektronix DMM4050.
  /// </summary>
  public MultiDmm4050()
  {
    ConfigureUsbMeter(
      "Tektronix DMM4050",
      "USB-SCPI мультиметр Tektronix DMM4050.",
      "DMM4050");

    ConfigureAgilentUsbCommands(supportsCapacitance: true);
  }
}
