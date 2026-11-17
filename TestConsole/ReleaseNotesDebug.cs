using Ask.Support.ReleaseNotes;

namespace TestConsole;

/// <summary>
/// Выводит проверенный каталог выпусков в консоль без запуска справки или оборудования.
/// </summary>
internal static class ReleaseNotesDebug
{
  /// <summary>
  /// Читает заданный каталог или историю выпусков из каталога приложения.
  /// </summary>
  /// <param name="directoryPath">Каталог JSON; пустое значение выбирает AppHelp/releases рядом с приложением.</param>
  /// <returns>Код 0 при успешном чтении или 1 при ошибке данных либо доступа к файлам.</returns>
  public static async Task<int> RunAsync(string? directoryPath)
  {
    string path = string.IsNullOrWhiteSpace(directoryPath)
      ? Path.Combine(AppContext.BaseDirectory, "AppHelp", "releases")
      : directoryPath;
    Console.WriteLine($"Каталог: {path}");
    try
    {
      var releases = await new ReleaseNotesReader().ReadDirectoryAsync(path);
      Console.WriteLine($"Прочитано выпусков: {releases.Count}");
      foreach (var release in releases)
      {
        Console.WriteLine($"\nВерсия {release.Version} · {release.ReleaseDate:yyyy-MM-dd}");
        if (!string.IsNullOrWhiteSpace(release.Title)) Console.WriteLine(release.Title);
        if (!string.IsNullOrWhiteSpace(release.Summary)) Console.WriteLine(release.Summary);
        foreach (var change in release.Changes)
        {
          Console.WriteLine($"  [{change.Category}] {change.Id}: {change.Title}");
          Console.WriteLine($"    {change.Description}");
          if (change.HelpKey is not null) Console.WriteLine($"    Справка: {change.HelpKey}");
        }
      }
      return 0;
    }
    catch (Exception exception) when (exception is InvalidDataException or IOException or UnauthorizedAccessException)
    {
      Console.Error.WriteLine(exception.Message);
      return 1;
    }
  }
}
