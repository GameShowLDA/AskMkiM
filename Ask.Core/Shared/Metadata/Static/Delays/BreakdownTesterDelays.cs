namespace Ask.Core.Shared.Metadata.Static.Delays
{
  /// <summary>
  /// Содержит задержки, используемые при работе с пробойной установкой.
  /// </summary>
  public class BreakdownTesterDelays
  {
    /// <summary>
    /// Создаёт набор задержек пробойной установки.
    /// </summary>
    /// <param name="postTestDelay">Задержка после испытания напряжения в миллисекундах.</param>
    public BreakdownTesterDelays(int postTestDelay = 100)
    {
      PostTestDelay = new DelayModel()
      {
        Name = "Задержка после испытания напряжения",
        Delay = postTestDelay,
      };
    }

    /// <summary>
    /// Задержка после испытания напряжения.
    /// </summary>
    public readonly DelayModel PostTestDelay;
  }
}
