namespace Ask.Core.Shared.Metadata.Static.Delays
{
  /// <summary>
  /// Содержит задержки, используемые при работе с пробойной установкой.
  /// </summary>
  public class BreakdownTesterDelays
  {
    /// <summary>
    /// Задержка после испытания напряжения.
    /// </summary>
    public readonly DelayModel PostTestDelay = new DelayModel() { Name = "Задержка после испытания напряжения", Delay = 100 };
  }
}
