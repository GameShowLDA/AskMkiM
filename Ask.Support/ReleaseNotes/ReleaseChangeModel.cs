namespace Ask.Support.ReleaseNotes;

/// <summary>
/// Содержит описание одного пользовательского изменения.
/// </summary>
public sealed class ReleaseChangeModel
{
  /// <summary>Уникальный идентификатор изменения внутри выпуска.</summary>
  public required string Id { get; init; }

  /// <summary>Категория изменения.</summary>
  public required ReleaseChangeCategory Category { get; init; }

  /// <summary>Краткий заголовок изменения.</summary>
  public required string Title { get; init; }

  /// <summary>Описание изменения обычным текстом.</summary>
  public required string Description { get; init; }

  /// <summary>Необязательный ключ связанной страницы справки.</summary>
  public string? HelpKey { get; init; }
}
