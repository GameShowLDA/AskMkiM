using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using AskMkiM.Updater.Core.Models;

namespace AskMkiM.Updater.Core;

/// <summary>
/// Проверяет и устанавливает автономное обновление с резервным копированием и откатом.
/// </summary>
public sealed class UpdateInstallationService
{
  private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

  private sealed record BackupRecord(string TargetPath, string? BackupPath, bool Existed);

  /// <summary>
  /// Проверяет, запущен ли обновляемый экземпляр AskMkiM.
  /// </summary>
  public static bool IsApplicationRunning(string executablePath)
  {
    string expected = Path.GetFullPath(executablePath);
    foreach (Process process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(expected)))
    {
      using (process)
      {
        try
        {
          if (string.Equals(process.MainModule?.FileName, expected, StringComparison.OrdinalIgnoreCase))
          {
            return true;
          }
        }
        catch
        {
          // Если путь процесса недоступен, безопаснее считать приложение запущенным.
          return true;
        }
      }
    }

    return false;
  }

  /// <summary>
  /// Устанавливает пакет в каталог выбранного экземпляра AskMkiM.
  /// </summary>
  public async Task<UpdateInstallationResult> InstallAsync(
    OpenedUpdatePackage package,
    string targetExecutablePath,
    IProgress<string>? progress = null,
    CancellationToken cancellationToken = default)
  {
    ApplicationVersionInfo currentVersion = ApplicationVersionReader.Read(targetExecutablePath);
    string targetRoot = Path.GetDirectoryName(currentVersion.ExecutablePath)!;
    if (IsApplicationRunning(currentVersion.ExecutablePath))
    {
      throw new InvalidOperationException("Закройте AskMkiM перед установкой обновления.");
    }

    EnsureWritable(targetRoot);
    string stagingDirectory = Path.Combine(targetRoot, $".update-staging-{Guid.NewGuid():N}");
    string backupDirectory = CreateBackupDirectory(targetRoot, currentVersion.Version, package.Manifest.Version);
    var backupRecords = new Dictionary<string, BackupRecord>(StringComparer.OrdinalIgnoreCase);
    var appliedFiles = new List<BackupRecord>();
    int replacedFiles = 0;
    int removedFiles = 0;

    try
    {
      progress?.Report("Проверка файлов обновления...");
      Directory.CreateDirectory(stagingDirectory);
      await ExtractAndVerifyAsync(package, stagingDirectory, progress, cancellationToken);

      IReadOnlyList<string> obsoleteFiles = ReadObsoleteManagedFiles(targetRoot, package.Manifest);
      IReadOnlyList<UpdateFileEntry> changedFiles = package.Manifest.Files
        .Where(file => IsDifferent(targetRoot, file))
        .ToArray();

      progress?.Report("Создание резервной копии...");
      Directory.CreateDirectory(backupDirectory);
      BackupProtectedData(targetRoot, backupDirectory);

      foreach (UpdateFileEntry file in changedFiles)
      {
        cancellationToken.ThrowIfCancellationRequested();
        string targetPath = PathSafety.CombineInsideRoot(targetRoot, file.Path);
        backupRecords[targetPath] = BackupFile(targetRoot, backupDirectory, targetPath);
      }

      foreach (string relativePath in obsoleteFiles)
      {
        cancellationToken.ThrowIfCancellationRequested();
        string targetPath = PathSafety.CombineInsideRoot(targetRoot, relativePath);
        backupRecords[targetPath] = BackupFile(targetRoot, backupDirectory, targetPath);
      }

      string installedManifestPath = Path.Combine(targetRoot, PackageConstants.InstalledManifestName);
      backupRecords[installedManifestPath] = BackupFile(targetRoot, backupDirectory, installedManifestPath);

      progress?.Report("Замена программных файлов...");
      foreach (UpdateFileEntry file in changedFiles)
      {
        cancellationToken.ThrowIfCancellationRequested();
        string sourcePath = PathSafety.CombineInsideRoot(stagingDirectory, file.Path);
        string targetPath = PathSafety.CombineInsideRoot(targetRoot, file.Path);
        ReplaceFile(sourcePath, targetPath);
        appliedFiles.Add(backupRecords[targetPath]);
        replacedFiles++;
      }

      foreach (string relativePath in obsoleteFiles)
      {
        cancellationToken.ThrowIfCancellationRequested();
        string targetPath = PathSafety.CombineInsideRoot(targetRoot, relativePath);
        if (File.Exists(targetPath))
        {
          File.Delete(targetPath);
          appliedFiles.Add(backupRecords[targetPath]);
          removedFiles++;
        }
      }

      WriteJsonAtomically(installedManifestPath, package.Manifest);
      appliedFiles.Add(backupRecords[installedManifestPath]);
      WriteBackupMetadata(backupDirectory, currentVersion, package.Manifest, changedFiles, obsoleteFiles);
      progress?.Report("Обновление установлено.");
      return new UpdateInstallationResult(
        currentVersion,
        package.Manifest,
        backupDirectory,
        replacedFiles,
        removedFiles);
    }
    catch (Exception installationException)
    {
      progress?.Report("Ошибка установки. Восстановление предыдущих файлов...");
      IReadOnlyList<Exception> rollbackErrors = Rollback(appliedFiles);
      if (rollbackErrors.Count > 0)
      {
        throw new AggregateException(
          $"Не удалось полностью восстановить предыдущую версию. Резервная копия: {backupDirectory}",
          new[] { installationException }.Concat(rollbackErrors));
      }

      throw;
    }
    finally
    {
      TryDeleteDirectory(stagingDirectory);
    }
  }

  private static async Task ExtractAndVerifyAsync(
    OpenedUpdatePackage package,
    string stagingDirectory,
    IProgress<string>? progress,
    CancellationToken cancellationToken)
  {
    using ZipArchive archive = ZipFile.OpenRead(package.ArchivePath);
    foreach (UpdateFileEntry file in package.Manifest.Files)
    {
      cancellationToken.ThrowIfCancellationRequested();
      string relativePath = PathSafety.NormalizeRelativePath(file.Path);
      if (ProtectedPathPolicy.IsProtected(relativePath))
      {
        throw new InvalidDataException($"Пакет пытается изменить защищённый файл: {relativePath}");
      }

      ZipArchiveEntry entry = archive.GetEntry(relativePath)
        ?? throw new InvalidDataException($"В пакете отсутствует файл {relativePath}.");
      string destinationPath = PathSafety.CombineInsideRoot(stagingDirectory, relativePath);
      Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
      await using (Stream input = entry.Open())
      await using (FileStream output = new(destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
      {
        await input.CopyToAsync(output, cancellationToken);
      }

      string hash = HashService.ComputeSha256(destinationPath);
      if (!string.Equals(hash, file.Sha256, StringComparison.OrdinalIgnoreCase))
      {
        throw new InvalidDataException($"Контрольная сумма файла {relativePath} не совпадает.");
      }

      progress?.Report($"Проверен: {relativePath}");
    }
  }

  private static IReadOnlyList<string> ReadObsoleteManagedFiles(
    string targetRoot,
    UpdateManifest newManifest)
  {
    string path = Path.Combine(targetRoot, PackageConstants.InstalledManifestName);
    if (!File.Exists(path)) return Array.Empty<string>();

    try
    {
      UpdateManifest? previous = JsonSerializer.Deserialize<UpdateManifest>(File.ReadAllText(path));
      if (previous is null) return Array.Empty<string>();
      var currentPaths = new HashSet<string>(
        newManifest.Files.Select(file => PathSafety.NormalizeRelativePath(file.Path)),
        StringComparer.OrdinalIgnoreCase);
      return previous.Files
        .Select(file => PathSafety.NormalizeRelativePath(file.Path))
        .Where(path => !currentPaths.Contains(path) && !ProtectedPathPolicy.IsProtected(path))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
    }
    catch (Exception ex) when (ex is JsonException or InvalidDataException)
    {
      throw new InvalidDataException("Не удалось прочитать манифест установленной версии.", ex);
    }
  }

  private static bool IsDifferent(string targetRoot, UpdateFileEntry file)
  {
    string targetPath = PathSafety.CombineInsideRoot(targetRoot, file.Path);
    return !File.Exists(targetPath)
      || new FileInfo(targetPath).Length != file.Length
      || !string.Equals(HashService.ComputeSha256(targetPath), file.Sha256, StringComparison.OrdinalIgnoreCase);
  }

  private static BackupRecord BackupFile(
    string targetRoot,
    string backupRoot,
    string targetPath)
  {
    if (!File.Exists(targetPath)) return new BackupRecord(targetPath, null, false);
    string relativePath = Path.GetRelativePath(targetRoot, targetPath);
    string backupPath = PathSafety.CombineInsideRoot(Path.Combine(backupRoot, "ProgramFiles"), relativePath);
    Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
    File.Copy(targetPath, backupPath, true);
    return new BackupRecord(targetPath, backupPath, true);
  }

  private static void BackupProtectedData(string targetRoot, string backupRoot)
  {
    string dataRoot = Path.Combine(backupRoot, "DataSnapshot");
    foreach (string relativePath in new[] { "Resources/app.db", "Resources/role-auth.json" })
    {
      string source = PathSafety.CombineInsideRoot(targetRoot, relativePath);
      if (!File.Exists(source)) continue;
      string destination = PathSafety.CombineInsideRoot(dataRoot, relativePath);
      Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
      File.Copy(source, destination, true);
    }

    string settingsSource = Path.Combine(targetRoot, "Settings");
    if (!Directory.Exists(settingsSource)) return;
    foreach (string source in Directory.EnumerateFiles(settingsSource, "*", SearchOption.AllDirectories))
    {
      string relativePath = Path.GetRelativePath(targetRoot, source);
      string destination = PathSafety.CombineInsideRoot(dataRoot, relativePath);
      Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
      File.Copy(source, destination, true);
    }
  }

  private static void ReplaceFile(string sourcePath, string targetPath)
  {
    Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
    string temporaryPath = targetPath + $".update-{Guid.NewGuid():N}.tmp";
    try
    {
      File.Copy(sourcePath, temporaryPath, true);
      File.Move(temporaryPath, targetPath, true);
    }
    finally
    {
      if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
    }
  }

  private static IReadOnlyList<Exception> Rollback(IEnumerable<BackupRecord> touchedFiles)
  {
    var errors = new List<Exception>();
    foreach (BackupRecord record in touchedFiles.Reverse())
    {
      try
      {
        if (record.Existed && record.BackupPath is not null && File.Exists(record.BackupPath))
        {
          Directory.CreateDirectory(Path.GetDirectoryName(record.TargetPath)!);
          File.Copy(record.BackupPath, record.TargetPath, true);
        }
        else if (!record.Existed && File.Exists(record.TargetPath))
        {
          File.Delete(record.TargetPath);
        }
      }
      catch (Exception ex)
      {
        errors.Add(new IOException($"Не удалось восстановить {record.TargetPath}.", ex));
      }
    }

    return errors;
  }

  private static void WriteBackupMetadata(
    string backupDirectory,
    ApplicationVersionInfo currentVersion,
    UpdateManifest manifest,
    IReadOnlyCollection<UpdateFileEntry> changedFiles,
    IReadOnlyCollection<string> obsoleteFiles)
  {
    var metadata = new
    {
      CreatedAt = DateTimeOffset.Now,
      PreviousVersion = currentVersion.Version,
      PreviousBuildIdentifier = currentVersion.BuildIdentifier,
      TargetVersion = manifest.Version,
      TargetBuildIdentifier = manifest.BuildIdentifier,
      ReplacedFiles = changedFiles.Select(file => file.Path).ToArray(),
      RemovedFiles = obsoleteFiles,
    };
    File.WriteAllText(
      Path.Combine(backupDirectory, "backup-manifest.json"),
      JsonSerializer.Serialize(metadata, JsonOptions));
  }

  private static void WriteJsonAtomically<T>(string path, T value)
  {
    string temporaryPath = path + $".{Guid.NewGuid():N}.tmp";
    File.WriteAllText(temporaryPath, JsonSerializer.Serialize(value, JsonOptions));
    File.Move(temporaryPath, path, true);
  }

  private static string CreateBackupDirectory(string targetRoot, string oldVersion, string newVersion)
  {
    string safeOld = SanitizeFileName(oldVersion);
    string safeNew = SanitizeFileName(newVersion);
    string basePath = Path.Combine(
      targetRoot,
      "UpdateBackups",
      $"{DateTime.Now:yyyyMMdd_HHmmss}_{safeOld}_to_{safeNew}");
    string candidate = basePath;
    for (int suffix = 2; Directory.Exists(candidate); suffix++)
    {
      candidate = $"{basePath}_{suffix}";
    }

    return candidate;
  }

  private static string SanitizeFileName(string value)
  {
    foreach (char invalid in Path.GetInvalidFileNameChars()) value = value.Replace(invalid, '_');
    return value;
  }

  private static void EnsureWritable(string targetRoot)
  {
    string probe = Path.Combine(targetRoot, $".write-probe-{Guid.NewGuid():N}");
    try
    {
      File.WriteAllText(probe, string.Empty);
    }
    catch (UnauthorizedAccessException ex)
    {
      throw new UnauthorizedAccessException(
        "Нет прав на изменение каталога AskMkiM. Выберите установку, доступную текущему пользователю.", ex);
    }
    finally
    {
      if (File.Exists(probe)) File.Delete(probe);
    }
  }

  private static void TryDeleteDirectory(string path)
  {
    try
    {
      if (Directory.Exists(path)) Directory.Delete(path, true);
    }
    catch
    {
      // Остаток временного каталога не влияет на установленную версию.
    }
  }
}
