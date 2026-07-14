namespace Ask.Device.Runtime.Device.Multimeters;

/// <summary>
/// USB-мультиметр Tektronix DMM4040.
/// </summary>
public sealed class MultiDmm4040 : MultiUsbSCPIMeterBase
{
  /// <summary>
  /// Создает описание USB-мультиметра Tektronix DMM4040.
  /// </summary>
  public MultiDmm4040()
  {
    ConfigureUsbMeter(
      "Tektronix DMM4040",
      "USB-SCPI мультиметр Tektronix DMM4040.",
      "DMM4040");

    ConfigureAgilentUsbCommands(supportsCapacitance: true);
  }
}
