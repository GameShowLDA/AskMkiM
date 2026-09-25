namespace Ask.Core.Shared.Metadata.Static.Delays
{
  /// <summary>
  /// Содержит задержки, используемые при работе с модулем коммутации реле.
  /// </summary>
  public class ModuleRelayControlDelays
  {
    /// <summary>
    /// Создаёт набор задержек модуля коммутации реле.
    /// </summary>
    /// <param name="preCommandDelay">Задержка перед отправкой команды в миллисекундах.</param>
    /// <param name="postCommandDelay">Задержка после отправки команды в миллисекундах.</param>
    public ModuleRelayControlDelays(int preCommandDelay = 20, int postCommandDelay = 20)
    {
      PreCommandDelay = new DelayModel()
      {
        Name = "Задержка перед отправкой команды МКР",
        Delay = preCommandDelay,
      };
      PostCommandDelay = new DelayModel()
      {
        Name = "Задержка после отправки команды МКР",
        Delay = postCommandDelay,
      };
    }

    /// <summary>
    /// Задержка перед отправкой команды МКР.
    /// </summary>
    public readonly DelayModel PreCommandDelay;

    /// <summary>
    /// Задержка после отправки команды МКР.
    /// </summary>
    public readonly DelayModel PostCommandDelay;
  }
}
