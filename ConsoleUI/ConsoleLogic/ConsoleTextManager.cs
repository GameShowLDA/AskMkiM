using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using ConsoleUI.ConsoleUI;

namespace ConsoleUI.ConsoleLogic
{
  public class ConsoleTextManager
  {
    private static readonly Lazy<ConsoleTextManager> _instance = new(() => new ConsoleTextManager());
    public static ConsoleTextManager Instance => _instance.Value;

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

      lock (_lock)
      {
        _buffer.Add(entry);
      }

      Application.Current.Dispatcher.BeginInvoke(() =>
      {
        foreach (var sub in _subscribers)
          sub(entry);
      });
    }

    public void Subscribe(Action<LogEntry> callback)
    {
      lock (_lock)
      {
        _subscribers.Add(callback);

        foreach (var entry in _buffer)
          callback(entry); // при подписке отдаём всё ранее накопленное
      }
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
      var normalized = text.ToUpperInvariant();
      if (normalized.Contains("|ERROR")) return Brushes.OrangeRed;
      if (normalized.Contains("|WARN")) return Brushes.Goldenrod;
      if (normalized.Contains("|DEBUG")) return Brushes.Gray;
      if (normalized.Contains("|INFO")) return Brushes.White;
      return Brushes.LightGray;
    }
  }
}
