namespace TestConsole.ModuleRelayControlTests
{
  public sealed record ModuleRelayControlCommandResult(
    string Command,
    string Response,
    TimeSpan Elapsed,
    bool Success,
    bool TimedOut,
    Exception? Error = null)
  {
    public string ErrorMessage => Error?.Message ?? string.Empty;
  }
}
