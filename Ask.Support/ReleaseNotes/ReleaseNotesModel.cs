namespace Ask.Support.ReleaseNotes;

/// <summary>
/// Содержит описание опубликованного выпуска приложения.
/// </summary>
public sealed class ReleaseNotesModel
{
  /// <summary>Версия формата описания выпуска.</summary>
  public required int SchemaVersion { get; init; }

  /// <summary>Номер выпуска в формате major.minor.patch.</summary>
  public required string Version { get; init; }

  /// <summary>Дата публикации выпуска.</summary>
  public required DateOnly ReleaseDate { get; init; }

  /// <summary>Необязательное название выпуска.</summary>
  public string? Title { get; init; }

  /// <summary>Необязательное краткое описание выпуска.</summary>
  public string? Summary { get; init; }

  /// <summary>Изменения выпуска в редакционном порядке.</summary>
  public required IReadOnlyList<ReleaseChangeModel> Changes { get; init; }

  /// <summary>Дополнительные изменения для разработчиков, видимые только роли ROOT.</summary>
  public IReadOnlyList<ReleaseChangeModel> RootChanges { get; init; } = [];
}
