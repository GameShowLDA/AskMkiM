using Ask.Core.Shared.Interfaces.EventInterfaces;
using System.Collections.Concurrent;

namespace Ask.Core.Services.EventCore.Services
{
  /// <summary>
  /// Статический агрегатор событий приложения.
  /// Предоставляет централизованный механизм публикации и подписки на события любого типа.
  /// </summary>
  public static class EventAggregator
  {
    private static readonly ConcurrentDictionary<Type, List<Delegate>> _subscribers = new();

    public static void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent
    {
      var list = _subscribers.GetOrAdd(typeof(TEvent), _ => new List<Delegate>());
      lock (list) list.Add(handler);
    }

    public static void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent
    {
      if (_subscribers.TryGetValue(typeof(TEvent), out var list))
      {
        lock (list) list.Remove(handler);
      }
    }

    public static void Publish<TEvent>(TEvent eventData) where TEvent : IEvent
    {
      if (_subscribers.TryGetValue(typeof(TEvent), out var list))
      {
        foreach (var subscriber in list.ToArray())
        {
          try
          {
            if (subscriber is Action<TEvent> action)

              action.Invoke(eventData);
          }
          catch (Exception ex)
          {
            Console.WriteLine($"Ошибка при обработке события {typeof(TEvent).Name}: {ex.Message}");
          }
        }
      }
    }

    public static void ClearAll() => _subscribers.Clear();
  }
}
