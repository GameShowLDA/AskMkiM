using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ask.Support.ReleaseNotes;

/// <summary>
/// Читает и проверяет каталог JSON-описаний выпусков без запуска системы справки.
/// </summary>
public sealed class ReleaseNotesReader
{
  private static readonly JsonSerializerOptions Options = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    Converters = { new JsonStringEnumConverter<ReleaseChangeCategory>(JsonNamingPolicy.CamelCase, false) }
  };

  /// <summary>
  /// Загружает все JSON-файлы верхнего уровня каталога и сортирует выпуски от новых к старым.
  /// </summary>
  /// <param name="directoryPath">Каталог с файлами major.minor.patch.json.</param>
  /// <param name="cancellationToken">Токен отмены чтения каталога.</param>
  /// <returns>Проверенные выпуски; для пустого каталога — пустой список.</returns>
  /// <exception cref="InvalidDataException">Описание выпуска нарушает формат или правила каталога.</exception>
  /// <exception cref="DirectoryNotFoundException">Каталог выпусков отсутствует.</exception>
  public async Task<IReadOnlyList<ReleaseNotesModel>> ReadDirectoryAsync(
    string directoryPath, CancellationToken cancellationToken = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
    cancellationToken.ThrowIfCancellationRequested();
    var releases = new List<(Version Version, ReleaseNotesModel Model)>();
    var versions = new HashSet<Version>();
    foreach (string filePath in Directory.EnumerateFiles(directoryPath, "*.json").Order(StringComparer.Ordinal))
    {
      cancellationToken.ThrowIfCancellationRequested();
      ReleaseNotesModel model;
      try
      {
        await using var stream = File.OpenRead(filePath);
        model = await JsonSerializer.DeserializeAsync<ReleaseNotesModel>(stream, Options, cancellationToken)
          ?? throw new InvalidDataException($"{filePath}: описание выпуска равно null.");
      }
      catch (JsonException exception)
      {
        throw new InvalidDataException($"{filePath}: некорректное описание JSON: {exception.Message}", exception);
      }

      Version version = Validate(model, filePath);
      if (!versions.Add(version))
      {
        throw new InvalidDataException($"{filePath}: повтор версии {model.Version}.");
      }
      releases.Add((version, model));
    }

    return releases.OrderByDescending(static release => release.Version)
      .Select(static release => release.Model).ToArray();
  }

  private static Version Validate(ReleaseNotesModel model, string filePath)
  {
    void Require(bool condition, string message)
    {
      if (!condition) throw new InvalidDataException($"{filePath}: {message}");
    }

    Require(model.SchemaVersion == 1, "поддерживается только schemaVersion = 1.");
    string[] parts = model.Version?.Split('.') ?? [];
    Require(parts.Length == 3 && parts.All(static part =>
      int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out int value)
      && value >= 0 && part == value.ToString(CultureInfo.InvariantCulture)),
      "version должна иметь формат major.minor.patch без ведущих нулей.");
    var version = Version.Parse(model.Version!);
    Require(Path.GetFileNameWithoutExtension(filePath) == model.Version,
      "имя файла должно совпадать с version.");
    Require(model.ReleaseDate != default, "releaseDate должна содержать дату публикации.");
    Require(model.Changes is { Count: > 0 }, "changes должен содержать хотя бы одно изменение.");
    var ids = new HashSet<string>(StringComparer.Ordinal);
    foreach (ReleaseChangeModel change in model.Changes!)
    {
      Require(change is not null, "changes не должен содержать null.");
      Require(!string.IsNullOrWhiteSpace(change!.Id), "id изменения обязателен.");
      Require(ids.Add(change.Id), $"повтор id изменения: {change.Id}.");
      Require(Enum.IsDefined(change.Category), "неизвестная category изменения.");
      Require(!string.IsNullOrWhiteSpace(change.Title), "title изменения обязателен.");
      Require(!string.IsNullOrWhiteSpace(change.Description), "description изменения обязателен.");
    }
    return version;
  }
}
