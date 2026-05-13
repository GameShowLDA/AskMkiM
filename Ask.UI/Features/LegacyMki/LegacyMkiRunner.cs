using System.Diagnostics;
using System.IO;

namespace Ask.UI.Features.LegacyMki;

public sealed record LegacyMkiRunResult(
  int ExitCode,
  string ProtocolPath);

public static class LegacyMkiRunner
{
  public static async Task<LegacyMkiRunResult> RunAsync(
    string mkiExePath,
    string controlProgramPath,
    int timeoutSeconds = 30,
    CancellationToken cancellationToken = default)
  {
    if (!File.Exists(mkiExePath))
      throw new FileNotFoundException($"mkiw.exe не найден: {mkiExePath}", mkiExePath);

    if (!File.Exists(controlProgramPath))
      throw new FileNotFoundException($"Файл программы контроля не найден: {controlProgramPath}", controlProgramPath);

    var mkiDir = Path.GetDirectoryName(mkiExePath)
      ?? throw new InvalidOperationException("Не удалось определить папку mkiw.exe.");

    var historyRoot = Path.Combine(mkiDir, "HISTORY");

    // Запоминаем время старта, чтобы потом не открыть старый протокол.
    var startedUtc = DateTime.UtcNow.AddSeconds(-2);

    using var process = new Process();

    process.StartInfo = new ProcessStartInfo
    {
      FileName = mkiExePath,
      WorkingDirectory = mkiDir,
      UseShellExecute = false,
      CreateNoWindow = true,
      WindowStyle = ProcessWindowStyle.Hidden
    };

    process.StartInfo.ArgumentList.Add("/runheadless");
    process.StartInfo.ArgumentList.Add(controlProgramPath);

    if (!process.Start())
      throw new InvalidOperationException("Не удалось запустить mkiw.exe.");

    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

    try
    {
      await process.WaitForExitAsync(timeoutCts.Token);
    }
    catch (OperationCanceledException)
    {
      try
      {
        if (!process.HasExited)
          process.Kill(entireProcessTree: true);
      }
      catch
      {
        // Игнорируем ошибку принудительного завершения.
      }

      throw new TimeoutException($"mkiw.exe работала больше {timeoutSeconds} секунд.");
    }

    if (process.ExitCode != 0)
      throw new InvalidOperationException($"mkiw.exe завершилась с кодом {process.ExitCode}.");

    var protocolPath = FindLatestProtocol(historyRoot, startedUtc);

    if (protocolPath == null)
      throw new FileNotFoundException($"Протокол не найден в папке: {historyRoot}");

    return new LegacyMkiRunResult(process.ExitCode, protocolPath);
  }

  private static string? FindLatestProtocol(string historyRoot, DateTime startedUtc)
  {
    if (!Directory.Exists(historyRoot))
      return null;

    var files = Directory
      .EnumerateFiles(historyRoot, "*.*", SearchOption.AllDirectories)
      .Select(path => new
      {
        Path = path,
        LastWriteUtc = File.GetLastWriteTimeUtc(path)
      })
      .OrderByDescending(x => x.LastWriteUtc)
      .ToList();

    // Сначала ищем файл, созданный/изменённый после запуска.
    var freshFile = files
      .Where(x => x.LastWriteUtc >= startedUtc)
      .Select(x => x.Path)
      .FirstOrDefault();

    if (freshFile != null)
      return freshFile;

    // Резервный вариант — как в батнике: просто последний файл в HISTORY.
    return files
      .Select(x => x.Path)
      .FirstOrDefault();
  }
}