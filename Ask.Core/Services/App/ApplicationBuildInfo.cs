using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace Ask.Core.Services.App;

/// <summary>
/// Предоставляет встроенные сведения о версии и происхождении запущенной сборки.
/// </summary>
public sealed record ApplicationBuildInfo
{
  private static readonly Lazy<ApplicationBuildInfo> CurrentValue = new(Create);

  /// <summary>Сведения о запущенной сборке приложения.</summary>
  public static ApplicationBuildInfo Current => CurrentValue.Value;

  /// <summary>Функциональная версия приложения.</summary>
  public required string Version { get; init; }

  /// <summary>Полный идентификатор сборки.</summary>
  public required string BuildIdentifier { get; init; }

  /// <summary>Время сборки в формате UTC.</summary>
  public required string BuildTimestampUtc { get; init; }

  /// <summary>Календарная дата сборки для отображения пользователю.</summary>
  public string BuildDate => DateTime.TryParseExact(
    BuildTimestampUtc,
    "yyyyMMdd.HHmmss",
    CultureInfo.InvariantCulture,
    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
    out DateTime timestamp)
      ? timestamp.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)
      : "Неизвестно";

  /// <summary>Полный хеш Git-коммита.</summary>
  public required string GitCommit { get; init; }

  /// <summary>Сокращённый хеш Git-коммита.</summary>
  public required string GitCommitShort { get; init; }

  /// <summary>Признак сборки из рабочей копии с незакоммиченными изменениями.</summary>
  public bool IsDirty { get; init; }

  /// <summary>Последние коммиты, доступные на момент сборки приложения.</summary>
  public required IReadOnlyList<BuildCommitInfo> RecentCommits { get; init; }

  /// <summary>Путь к запущенному исполняемому файлу.</summary>
  public required string ExecutablePath { get; init; }

  /// <summary>Время изменения запущенного исполняемого файла в формате UTC.</summary>
  public required string ExecutableModifiedUtc { get; init; }

  /// <summary>SHA-256 запущенного исполняемого файла.</summary>
  public required string ExecutableSha256 { get; init; }

  /// <summary>Идентификатор модуля запущенной entry assembly.</summary>
  public required string ModuleVersionId { get; init; }

  /// <summary>Формирует однострочное диагностическое представление сведений о сборке.</summary>
  /// <returns>Строка с версией, ревизией, состоянием Git и путём к EXE.</returns>
  public string ToDiagnosticString() =>
    $"Build={BuildIdentifier}; Commit={GitCommit}; Dirty={IsDirty}; "
    + $"MVID={ModuleVersionId}; EXE={ExecutablePath}; EXE_SHA256={ExecutableSha256}";

  /// <summary>
  /// Записывает манифест запущенной сборки рядом с исполняемым файлом.
  /// </summary>
  /// <returns>Полный путь к записанному манифесту.</returns>
  public async Task<string> WriteManifestAsync()
  {
    string path = Path.Combine(AppContext.BaseDirectory, "build-manifest.json");
    await File.WriteAllTextAsync(path, JsonSerializer.Serialize(this, new JsonSerializerOptions
    {
      WriteIndented = true
    }));
    return path;
  }

  private static ApplicationBuildInfo Create()
  {
    var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
    var metadataAttributes = assembly.GetCustomAttributes<AssemblyMetadataAttribute>().ToArray();
    var metadata = metadataAttributes
      .GroupBy(static attribute => attribute.Key, StringComparer.OrdinalIgnoreCase)
      .ToDictionary(static group => group.Key, static group => group.Last().Value ?? string.Empty,
        StringComparer.OrdinalIgnoreCase);
    string executablePath = Environment.ProcessPath ?? assembly.Location;
    string version = assembly.GetName().Version is { } assemblyVersion
      ? $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}"
      : "unknown";
    string informationalVersion = assembly
      .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? version;

    return new ApplicationBuildInfo
    {
      Version = version,
      BuildIdentifier = GetMetadata(metadata, "BuildIdentity", informationalVersion),
      BuildTimestampUtc = GetMetadata(metadata, "BuildTimestampUtc", "unknown"),
      GitCommit = GetMetadata(metadata, "GitCommit", "unknown"),
      GitCommitShort = GetMetadata(metadata, "GitCommitShort", "unknown"),
      IsDirty = bool.TryParse(GetMetadata(metadata, "GitDirty", "false"), out bool dirty) && dirty,
      RecentCommits = metadataAttributes
        .Where(static attribute => string.Equals(
          attribute.Key,
          "GitHistoryEntry",
          StringComparison.OrdinalIgnoreCase))
        .Select(static attribute => BuildCommitInfo.TryParse(attribute.Value))
        .Where(static commit => commit is not null)
        .Cast<BuildCommitInfo>()
        .ToArray(),
      ExecutablePath = executablePath,
      ExecutableModifiedUtc = GetExecutableModifiedUtc(executablePath),
      ExecutableSha256 = GetExecutableSha256(executablePath),
      ModuleVersionId = assembly.ManifestModule.ModuleVersionId.ToString("D")
    };
  }

  private static string GetMetadata(
    IReadOnlyDictionary<string, string> metadata,
    string key,
    string fallback) => metadata.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value)
      ? value
      : fallback;

  private static string GetExecutableModifiedUtc(string path)
  {
    try
    {
      return File.GetLastWriteTimeUtc(path).ToString("O");
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
    {
      return "unknown";
    }
  }

  private static string GetExecutableSha256(string path)
  {
    try
    {
      using var stream = File.OpenRead(path);
      return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
    {
      return "unknown";
    }
  }
}

/// <summary>
/// Содержит краткие сведения о Git-коммите, встроенные в приложение при сборке.
/// </summary>
public sealed record BuildCommitInfo
{
  /// <summary>Сокращённый хеш коммита.</summary>
  public required string Hash { get; init; }

  /// <summary>Дата создания коммита.</summary>
  public required string Date { get; init; }

  /// <summary>Заголовок коммита.</summary>
  public required string Subject { get; init; }

  internal static BuildCommitInfo? TryParse(string? value)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      return null;
    }

    string[] parts = value.Split(',', 3, StringSplitOptions.TrimEntries);
    return parts.Length == 3
      ? new BuildCommitInfo { Hash = parts[0], Date = parts[1], Subject = parts[2] }
      : null;
  }
}
