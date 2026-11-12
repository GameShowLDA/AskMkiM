namespace AskMkiM.Updater.Core.Models;

/// <summary>
/// Описывает содержимое и версию автономного пакета обновления.
/// </summary>
public sealed class UpdateManifest
{
  /// <summary>Идентификатор формата пакета.</summary>
  public int FormatVersion { get; init; } = 1;

  /// <summary>Имя обновляемого продукта.</summary>
  public string Product { get; init; } = "AskMkiM";

  /// <summary>Версия приложения.</summary>
  public string Version { get; init; } = string.Empty;

  /// <summary>Уникальный идентификатор сборки.</summary>
  public string BuildIdentifier { get; init; } = string.Empty;

  /// <summary>Время формирования пакета в UTC.</summary>
  public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;

  /// <summary>Список управляемых программных файлов.</summary>
  public IReadOnlyList<UpdateFileEntry> Files { get; init; } = Array.Empty<UpdateFileEntry>();
}
