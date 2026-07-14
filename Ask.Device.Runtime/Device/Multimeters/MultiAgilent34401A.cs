namespace Ask.Device.Runtime.Device.Multimeters;

/// <summary>
/// USB-мультиметр Agilent 34401A.
/// </summary>
public sealed class MultiAgilent34401A : MultiUsbSCPIMeterBase
{
  /// <summary>
  /// Создает описание USB-мультиметра Agilent 34401A.
  /// </summary>
  public MultiAgilent34401A()
  {
    ConfigureUsbMeter(
      "Agilent 34401A",
      "USB-SCPI мультиметр Agilent 34401A.",
      "34401A");

    ConfigureAgilentUsbCommands(supportsCapacitance: false);
  }
}
