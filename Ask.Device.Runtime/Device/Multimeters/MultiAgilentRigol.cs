namespace Ask.Device.Runtime.Device.Multimeters;

/// <summary>
/// USB-мультиметр agilent Rigol с Agilent-совместимым SCPI-протоколом.
/// </summary>
public sealed class MultiAgilentRigol : MultiUsbSCPIMeterBase
{
  /// <summary>
  /// Создает описание USB-мультиметра agilent Rigol.
  /// </summary>
  public MultiAgilentRigol()
  {
    ConfigureUsbMeter(
      "agilent Rigol",
      "USB-SCPI мультиметр Rigol, работающий через Agilent-совместимые команды.",
      "VID_0000&PID_0000");

    ConfigureAgilentUsbCommands(supportsCapacitance: true);
  }
}
