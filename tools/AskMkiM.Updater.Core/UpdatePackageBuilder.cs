using System.IO.Compression;
using System.Text;
using System.Text.Json;
using AskMkiM.Updater.Core.Models;

namespace AskMkiM.Updater.Core;

/// <summary>
/// Формирует автономный исполняемый файл обновления из опубликованной сборки AskMkiM.
/// </summary>
public sealed class UpdatePackageBuilder
{
  private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

  /// <summary>
  /// Создаёт автономный файл обновления на основе готового установщика.
  /// </summary>
  public async Task<UpdateManifest> BuildAsync(
    string sourceDirectory,
    string updaterStubPath,
    string outputPath,
    IProgress<string>? progress = null,
    CancellationToken cancellationToken = default)
  {
    string sourceRoot = Path.GetFullPath(sourceDirectory);
    string stubPath = Path.GetFullPath(updaterStubPath);
    string destinationPath = Path.GetFullPath(outputPath);
    if (!Directory.Exists(sourceRoot))
    {
      throw new DirectoryNotFoundException($"Каталог сборки не найден: {sourceRoot}");
    }

    if (!File.Exists(stubPath))
    {
      throw new FileNotFoundException("Не найден готовый автономный установщик.", stubPath);
    }

    if (string.Equals(stubPath, destinationPath, StringComparison.OrdinalIgnoreCase))
    {
      throw new InvalidOperationException("Готовое обновление нельзя сохранять поверх установщика-заготовки.");
    }

    string sourceExecutable = Path.Combine(sourceRoot, ApplicationVersionReader.ExecutableName);
    if (string.Equals(sourceExecutable, destinationPath, StringComparison.OrdinalIgnoreCase))
    {
      throw new InvalidOperationException("Готовое обновление нельзя сохранять поверх AskMkiM.exe.");
    }

    ApplicationVersionInfo version = ApplicationVersionReader.Read(
      sourceExecutable);
    var excludedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      stubPath,
      destinationPath,
    };
    IReadOnlyList<(string FullPath, string RelativePath)> files = EnumeratePayloadFiles(sourceRoot, excludedPaths);
    if (files.Count == 0)
    {
      throw new InvalidDataException("В выбранном каталоге нет программных файлов для обновления.");
    }

    progress?.Report("Вычисление контрольных сумм...");
    var entries = new List<UpdateFileEntry>(files.Count);
    foreach ((string fullPath, string relativePath) in files)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var info = new FileInfo(fullPath);
      entries.Add(new UpdateFileEntry(relativePath, info.Length, HashService.ComputeSha256(fullPath)));
    }

    var manifest = new UpdateManifest
    {
      Version = version.Version,
      BuildIdentifier = version.BuildIdentifier,
      Files = entries,
    };

    string tempArchive = Path.Combine(Path.GetTempPath(), $"AskMkiM.Update.{Guid.NewGuid():N}.zip");
    string tempOutput = destinationPath + $".{Guid.NewGuid():N}.tmp";
    try
    {
      progress?.Report("Упаковка новой версии...");
      await CreateArchiveAsync(tempArchive, files, manifest, cancellationToken);
      Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
      File.Copy(stubPath, tempOutput, true);

      await using (FileStream output = new(tempOutput, FileMode.Append, FileAccess.Write, FileShare.None))
      await using (FileStream archive = File.OpenRead(tempArchive))
      {
        await archive.CopyToAsync(output, cancellationToken);
        await output.WriteAsync(BitConverter.GetBytes(archive.Length), cancellationToken);
        await output.WriteAsync(Encoding.ASCII.GetBytes(PackageConstants.TrailerMagic), cancellationToken);
      }

      File.Move(tempOutput, destinationPath, true);
      progress?.Report($"Обновление {manifest.Version} создано.");
      return manifest;
    }
    finally
    {
      TryDelete(tempArchive);
      TryDelete(tempOutput);
    }
  }

  private static IReadOnlyList<(string FullPath, string RelativePath)> EnumeratePayloadFiles(
    string root,
    IReadOnlySet<string> excludedPaths)
  {
    return Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
      .Where(fullPath => !excludedPaths.Contains(Path.GetFullPath(fullPath)))
      .Select(fullPath =>
      {
        string relative = PathSafety.NormalizeRelativePath(Path.GetRelativePath(root, fullPath));
        return (FullPath: fullPath, RelativePath: relative);
      })
      .Where(file => !ProtectedPathPolicy.IsProtected(file.RelativePath))
      .Where(file => !string.Equals(
        file.RelativePath,
        PackageConstants.InstalledManifestName,
        StringComparison.OrdinalIgnoreCase))
      .Where(file => !file.RelativePath.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase))
      .OrderBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase)
      .ToArray();
  }

  private static async Task CreateArchiveAsync(
    string archivePath,
    IReadOnlyList<(string FullPath, string RelativePath)> files,
    UpdateManifest manifest,
    CancellationToken cancellationToken)
  {
    await using FileStream stream = new(archivePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
    using var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true);
    foreach ((string fullPath, string relativePath) in files)
    {
      cancellationToken.ThrowIfCancellationRequested();
      ZipArchiveEntry entry = archive.CreateEntry(relativePath, CompressionLevel.Optimal);
      await using Stream entryStream = entry.Open();
      await using FileStream input = File.OpenRead(fullPath);
      await input.CopyToAsync(entryStream, cancellationToken);
    }

    ZipArchiveEntry manifestEntry = archive.CreateEntry(PackageConstants.ManifestEntryName, CompressionLevel.Optimal);
    await using Stream manifestStream = manifestEntry.Open();
    await JsonSerializer.SerializeAsync(manifestStream, manifest, JsonOptions, cancellationToken);
  }

  private static void TryDelete(string path)
  {
    try
    {
      if (File.Exists(path)) File.Delete(path);
    }
    catch
    {
      // Временный файл будет удалён системой позднее.
    }
  }
}
