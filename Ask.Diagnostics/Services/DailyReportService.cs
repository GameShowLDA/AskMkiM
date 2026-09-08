using System.Globalization;
using System.IO.Compression;
using System.Text.Json;

namespace Ask.Diagnostics.Services;

/// <summary>Собирает сохранённые протоколы, журналы и отчёты о сбоях за выбранный локальный день.</summary>
public sealed class DailyReportService
{
  public async Task<IReadOnlyList<string>> CreateAsync(DateTime date, string destination,
    string historyRoot, string logsRoot, string crashRoot)
  {
    var issues = new List<string>();
    var files = new List<(string Source, string Entry)>();
    string day = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    Collect(Path.Combine(historyRoot, day), "Protocols", files, issues);
    Collect(Path.Combine(logsRoot, day), "Logs", files, issues);
    if (Directory.Exists(crashRoot))
    {
      string prefix = date.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + "_";
      foreach (string path in Directory.EnumerateFileSystemEntries(crashRoot).OrderBy(p => p))
      {
        if (!Path.GetFileName(path).StartsWith(prefix, StringComparison.Ordinal)) continue;
        if (Directory.Exists(path)) Collect(path, "CrashReports/" + Path.GetFileName(path), files, issues);
        else files.Add((path, "CrashReports/" + Path.GetFileName(path)));
      }
    }
    else issues.Add("Каталог crash reports отсутствует.");

    files.RemoveAll(f => string.Equals(Path.GetFullPath(f.Source), Path.GetFullPath(destination),
      StringComparison.OrdinalIgnoreCase));

    string temporary = destination + "." + Guid.NewGuid().ToString("N") + ".partial";
    try
    {
      await using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
      using (var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
      {
        foreach (var file in files)
        {
          try
          {
            await using var input = new FileStream(file.Source, FileMode.Open, FileAccess.Read,
              FileShare.ReadWrite | FileShare.Delete);
            long remaining = input.Length;
            await using var target = zip.CreateEntry(file.Entry, CompressionLevel.Optimal).Open();
            byte[] buffer = new byte[81920];
            while (remaining > 0)
            {
              int count = await input.ReadAsync(buffer.AsMemory(0, (int)Math.Min(buffer.Length, remaining)));
              if (count == 0) throw new IOException("Файл сократился во время чтения.");
              await target.WriteAsync(buffer.AsMemory(0, count));
              remaining -= count;
            }
          }
          catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
          {
            issues.Add($"{file.Entry}: {ex.Message}");
          }
        }
        await using var manifest = zip.CreateEntry("report.json").Open();
        await JsonSerializer.SerializeAsync(manifest, new
        {
          date = day, createdAt = DateTimeOffset.Now, files = files.Select(f => f.Entry), issues,
          note = "Снимок доступных файлов. Активные файлы могли измениться во время сбора. Протоколы сохранены в исходном формате."
        }, new JsonSerializerOptions { WriteIndented = true });
      }
      File.Move(temporary, destination, overwrite: true);
      return issues;
    }
    finally
    {
      if (File.Exists(temporary)) File.Delete(temporary);
    }
  }

  private static void Collect(string directory, string prefix,
    List<(string Source, string Entry)> files, List<string> issues)
  {
    if (!Directory.Exists(directory))
    {
      issues.Add($"{prefix}: за выбранную дату каталог отсутствует.");
      return;
    }
    foreach (string path in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories).OrderBy(p => p))
      files.Add((path, prefix + "/" + Path.GetRelativePath(directory, path).Replace('\\', '/')));
  }
}
