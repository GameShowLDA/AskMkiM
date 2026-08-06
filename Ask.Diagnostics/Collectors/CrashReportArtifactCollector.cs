using Ask.Diagnostics.Abstractions;
using Ask.Diagnostics.Infrastructure;
using Ask.Diagnostics.Models;

namespace Ask.Diagnostics.Collectors
{
  /// <summary>
  /// Сохраняет дополнительные диагностические вложения, переданные источником ошибки.
  /// </summary>
  public sealed class CrashReportArtifactCollector : ICrashDataCollector
  {
    public string Name => "Artifacts";

    public int Order => 200;

    public async Task CollectAsync(CrashContext context, CancellationToken cancellationToken = default)
    {
      foreach (var artifact in context.Artifacts)
      {
        cancellationToken.ThrowIfCancellationRequested();

        var outputPath = ResolveOutputPath(context.PackageDirectory, artifact.FileName);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        if (artifact.IsPlainText)
        {
          await File.WriteAllTextAsync(
            outputPath,
            artifact.Content?.ToString() ?? string.Empty,
            cancellationToken).ConfigureAwait(false);
        }
        else
        {
          await JsonFileWriter.WriteAsync(outputPath, artifact.Content, cancellationToken).ConfigureAwait(false);
        }
      }
    }

    private static string ResolveOutputPath(string packageDirectory, string fileName)
    {
      var normalizedFileName = fileName.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
      var outputPath = Path.GetFullPath(Path.Combine(packageDirectory, normalizedFileName));
      var rootPath = Path.GetFullPath(packageDirectory) + Path.DirectorySeparatorChar;

      if (!outputPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
      {
        throw new InvalidOperationException($"Недопустимый путь диагностического вложения: {fileName}");
      }

      return outputPath;
    }
  }
}
