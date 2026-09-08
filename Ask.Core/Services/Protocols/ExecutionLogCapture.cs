using Ask.Core.Shared.DTO.Protocol;
using Ask.LogLib;

namespace Ask.Core.Services.Protocols;

/// <summary>Запись журнала с позицией относительно сообщений протокола.</summary>
public sealed record ExecutionLogEntry(int BeforeMessage, DateTimeOffset Timestamp, string Level, string Text);

/// <summary>Собирает журнал приложения во время одного запуска, включая финальный сброс оборудования.</summary>
public sealed class ExecutionLogCapture : IDisposable
{
  private readonly object _gate = new();
  private readonly List<(ShowMessageModel? After, DateTimeOffset Time, string Level, string Text)> _entries = new();
  private readonly List<ShowMessageModel> _messages = new();
  private bool _active;

  public void Start()
  {
    Dispose();
    lock (_gate)
    {
      _entries.Clear();
      _messages.Clear();
      _active = true;
      LoggerUtility.LogMessageWritten += OnLog;
      LoggerUtility.ExceptionLogged += OnException;
    }
  }

  public void RecordMessage(ShowMessageModel message)
  {
    lock (_gate)
      if (_active) _messages.Add(message);
  }

  public IReadOnlyList<ExecutionLogEntry> Snapshot(IReadOnlyList<ShowMessageModel> messages)
  {
    lock (_gate)
    {
      var positions = new Dictionary<ShowMessageModel, int>(ReferenceEqualityComparer.Instance);
      for (int i = 0; i < messages.Count; i++) positions[messages[i]] = i + 1;
      var anchors = new Dictionary<ShowMessageModel, int>(ReferenceEqualityComparer.Instance);
      int position = 0;
      foreach (var message in _messages)
      {
        if (positions.TryGetValue(message, out int index)) position = index;
        anchors[message] = position;
      }
      return _entries.Select(e => new ExecutionLogEntry(
        e.After != null && anchors.TryGetValue(e.After, out int index) ? index : 0,
        e.Time, e.Level, e.Text)).ToArray();
    }
  }

  private void OnLog(object? sender, ApplicationLogMessageEventArgs e) => Add(e.Timestamp, e.Level.ToString(), e.Message);
  private void OnException(object? sender, LoggedExceptionEventArgs e) => Add(DateTimeOffset.Now, "Exception", e.Exception.ToString());

  private void Add(DateTimeOffset time, string level, string text)
  {
    lock (_gate)
      if (_active) _entries.Add((_messages.LastOrDefault(), time, level, text));
  }

  public void Dispose()
  {
    lock (_gate)
    {
      _active = false;
      LoggerUtility.LogMessageWritten -= OnLog;
      LoggerUtility.ExceptionLogged -= OnException;
    }
  }
}
