namespace Ask.Device.Runtime.Device.Multimeters;

/// <summary>
/// COM-мультиметр Agilent, совместимый со старым типом устройства Agilent (COM) из MKI.
/// </summary>
public sealed class MultiAgilentCom : MultiComSCPIMeterBase
{
  /// <summary>
  /// Инициализирует Agilent-совместимый COM-мультиметр.
  /// </summary>
  public MultiAgilentCom()
  {
    ConfigureComMeter(
      "agilent COM",
      "COM-SCPI мультиметр Agilent, работающий через Agilent-совместимые команды.");

    ConfigureAgilentComCommands(supportsCapacitance: true);
  }
}
