using System.Windows.Media;

namespace ConsoleUI.ConsoleLogic
{
  public class ConsoleTextManager
  {
    private static readonly Lazy<ConsoleTextManager> _instance = new(() => new ConsoleTextManager());
    public static ConsoleTextManager Instance => _instance.Value;

    public const int MaxBufferSize = 5000;

    private readonly List<LogEntry> _buffer = new();
    private readonly List<Action<LogEntry>> _subscribers = new();

    private readonly object _lock = new();

    public void Append(string text)
    {
      var entry = new LogEntry
      {
        Text = text,
        Color = ParseColor(text)
      };

      List<Action<LogEntry>> subscribers;
      lock (_lock)
      {
        _buffer.Add(entry);
        TrimBufferIfNeeded();
        subscribers = _subscribers.ToList();
      }

      foreach (var sub in subscribers)
        sub(entry);
    }

    public void Subscribe(Action<LogEntry> callback)
    {
      List<LogEntry> snapshot;
      lock (_lock)
      {
        _subscribers.Add(callback);
        snapshot = _buffer.ToList();
      }

      foreach (var entry in snapshot)
        callback(entry); // при подписке отдаём всё ранее накопленное
    }

    public void Unsubscribe(Action<LogEntry> callback)
    {
      lock (_lock)
        _subscribers.Remove(callback);
    }

    public void Clear()
    {
      lock (_lock)
        _buffer.Clear();

      ConsoleVisibilityController.ClearConsole();
    }

    private Brush ParseColor(string text)
    {
      if (string.IsNullOrWhiteSpace(text))
        return Brushes.LightGray;

      var normalized = text.ToUpperInvariant();

      if (HasLevelToken(normalized, "ERROR") || HasLevelToken(normalized, "EXCEPTION"))
        return Brushes.OrangeRed;
      if (HasLevelToken(normalized, "WARN") || HasLevelToken(normalized, "WARNING"))
        return Brushes.Goldenrod;
      if (HasLevelToken(normalized, "DEBUG"))
        return Brushes.Gray;
      if (HasLevelToken(normalized, "INFO"))
        return Brushes.White;

      return Brushes.LightGray;
    }

    private static bool HasLevelToken(string text, string token)
    {
      return text.Contains($"[{token}]") ||
             text.Contains($"|{token}") ||
             text.Contains($"{token}:") ||
             text.Contains($" {token} ") ||
             text.Contains($" {token}]") ||
             text.Contains($"[{token} ");
    }

    private void TrimBufferIfNeeded()
    {
      if (_buffer.Count <= MaxBufferSize)
        return;

      var removeCount = _buffer.Count - MaxBufferSize;
      _buffer.RemoveRange(0, removeCount);
    }
  }
}
