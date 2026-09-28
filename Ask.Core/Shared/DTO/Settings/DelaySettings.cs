namespace Ask.Core.Shared.DTO.Settings;

/// <summary>
/// Содержит фиксированные задержки оборудования в миллисекундах.
/// </summary>
public sealed class DelaySettings
{
  /// <summary>
  /// Задержки пробойной установки.
  /// </summary>
  public BreakdownTesterDelaySettings BreakdownTester { get; set; } = new();

  /// <summary>
  /// Задержки модуля коммутации реле.
  /// </summary>
  public ModuleRelayControlDelaySettings ModuleRelayControl { get; set; } = new();
}

/// <summary>
/// Содержит задержки пробойной установки в миллисекундах.
/// </summary>
public sealed class BreakdownTesterDelaySettings
{
  /// <summary>
  /// Задержка после испытания напряжения.
  /// </summary>
  public int PostTestDelay { get; set; } = 100;
}

/// <summary>
/// Содержит задержки модуля коммутации реле в миллисекундах.
/// </summary>
public sealed class ModuleRelayControlDelaySettings
{
  /// <summary>
  /// Задержка перед отправкой команды МКР.
  /// </summary>
  public int PreCommandDelay { get; set; } = 20;

  /// <summary>
  /// Задержка после отправки команды МКР.
  /// </summary>
  public int PostCommandDelay { get; set; } = 20;
}
