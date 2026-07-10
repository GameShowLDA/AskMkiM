namespace TestConsole.Keysight
{
  public sealed record KeysightCommandResult(
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
