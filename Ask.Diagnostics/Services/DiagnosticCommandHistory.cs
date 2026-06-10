namespace Ask.Diagnostics.Services
{
  public static class DiagnosticCommandHistory
  {
    private static Action<string>? _append;

    public static void Configure(Action<string>? append)
    {
      Volatile.Write(ref _append, append);
    }

    public static void RecordCommand(string? deviceName, string command)
    {
      if (string.IsNullOrWhiteSpace(command))
      {
        return;
      }

      Add(Format(deviceName, ">>", command));
    }

    public static void RecordResponse(string? deviceName, string response)
    {
      if (string.IsNullOrWhiteSpace(response))
      {
        return;
      }

      Add(Format(deviceName, "<<", response));
    }

    private static void Add(string value)
    {
      try
      {
        Volatile.Read(ref _append)?.Invoke(value);
      }
      catch
      {
      }
    }

    private static string Format(string? deviceName, string direction, string value)
    {
      var prefix = string.IsNullOrWhiteSpace(deviceName) ? string.Empty : $"[{deviceName}] ";
      return $"{prefix}{direction} {value.Trim()}";
    }
  }
}
