namespace Ask.Core.Services.Calendar;

/// <summary>Содержит общую заметку календаря, привязанную к дате.</summary>
public sealed record CalendarNote
{
  public Guid Id { get; init; }
  public DateTime Date { get; init; }
  /// <summary>Признак выполнения дела.</summary>
  public bool IsCompleted { get; init; }
  public string Title { get; init; } = string.Empty;
  public string Text { get; init; } = string.Empty;
  [System.Text.Json.Serialization.JsonIgnore]
  public string Caption => string.IsNullOrWhiteSpace(Title) ? "Заметка" : Title;
}
