namespace TestConsole.B7783
{
  public sealed record B7783CommandResult(
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
