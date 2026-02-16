namespace Ask.Core.Shared.Interfaces.ExecutionInterfaces
{
  /// <summary>
  /// Global command info that can be shared between execution and hotkey handlers.
  /// </summary>
  public interface IExecutionCommandInfo
  {
    /// <summary>
    /// Command number.
    /// </summary>
    string CommandNumber { get; }

    /// <summary>
    /// Command mnemonic.
    /// </summary>
    string Mnemonic { get; }

    /// <summary>
    /// Original command body text.
    /// </summary>
    string CommandBody { get; }
  }
}
