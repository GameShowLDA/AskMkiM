namespace Ask.Support.ReleaseNotes;

/// <summary>
/// Определяет категорию изменения выпуска.
/// </summary>
public enum ReleaseChangeCategory
{
  /// <summary>Добавленная возможность.</summary>
  Added,
  /// <summary>Изменение существующего поведения.</summary>
  Changed,
  /// <summary>Исправление ошибки.</summary>
  Fixed,
  /// <summary>Удалённая возможность.</summary>
  Removed,
  /// <summary>Важная информация перед работой.</summary>
  Important,
  /// <summary>Известное ограничение выпуска.</summary>
  KnownIssue
}
