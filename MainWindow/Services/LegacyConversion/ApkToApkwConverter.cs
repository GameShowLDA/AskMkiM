using Ask.Core.Services.FileFormats.Opk;
using MainWindowProgram.Services.Conversion;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UI.Services.Archive;

namespace MainWindowProgram.Services.LegacyConversion
{
  /// <summary>
  /// Converts a legacy APK archive index plus legacy OPK files on disk into a modern APKW archive.
  /// </summary>
  public sealed class ApkToApkwConverter
  {
    private static readonly byte[] ApkSignature = System.Convert.FromHexString("9DE2AE20A0E0E5A8A2208F8A20A4ABEF2080918A2D8C8A880A0A00");
    private static readonly byte[] VrMarkerRecord = { 0x02, 0x02, 0x00 };
    private static readonly byte[] VrMarkerOrdinary = { 0x02, 0x15, 0x00 };
    private static readonly byte[] VrMarkerEnd = { 0x01, 0x21, 0x00 };
    private static readonly Encoding OemEncoding = CreateOemEncoding();

    private readonly OpkToPkConverter _opkToPkConverter = new OpkToPkConverter();
    private readonly PkToOpkwConverter _pkToOpkwConverter = new PkToOpkwConverter();

    public ApkToApkwConversionResult Convert(string inputPath)
      => ConvertAsync(inputPath).GetAwaiter().GetResult();

    public Task<ApkToApkwConversionResult> ConvertAsync(
      string inputPath,
      IProgress<ApkToApkwProgressInfo>? progress = null,
      CancellationToken cancellationToken = default)
    {
      return Task.Run(() => ConvertInternal(inputPath, progress, cancellationToken), cancellationToken);
    }

    private ApkToApkwConversionResult ConvertInternal(
      string inputPath,
      IProgress<ApkToApkwProgressInfo>? progress,
      CancellationToken cancellationToken)
    {
      if (string.IsNullOrWhiteSpace(inputPath))
      {
        return CreateFailedResult(inputPath, "Не указан путь к исходному APK-архиву.");
      }

      try
      {
        var sourcePath = Path.GetFullPath(inputPath);
        if (!File.Exists(sourcePath))
        {
          return CreateFailedResult(sourcePath, $"Файл не найден: {sourcePath}");
        }

        if (!string.Equals(Path.GetExtension(sourcePath), ".apk", StringComparison.OrdinalIgnoreCase))
        {
          return CreateFailedResult(sourcePath, "Поддерживается только конвертация файлов .apk.");
        }

        var sourceDirectory = Path.GetDirectoryName(sourcePath)
          ?? throw new DirectoryNotFoundException("Не удалось определить папку исходного APK-архива.");
        ReportProgress(progress, "Подготовка архива", "Читаем структуру старого APK и готовим каталоги.", null, 0, 0, 2d);

        var intermediateDirectory = CreateIntermediateOutputDirectory(sourcePath);
        var apk = ApkFile.Read(sourcePath);
        var entries = apk.GetEntries();
        var totalEntries = entries.Count;

        var availableOpkFiles = Directory
          .EnumerateFiles(sourceDirectory, "*.opk", SearchOption.AllDirectories)
          .Where(path => !path.StartsWith(intermediateDirectory, StringComparison.OrdinalIgnoreCase))
          .ToList();

        var successfulEntries = new List<PreparedArchiveEntry>();
        var problemPkPaths = new List<string>();
        var translationErrorCount = 0;
        var translationFailedFilesCount = 0;
        var preparationFailedFilesCount = 0;
        var failedEntriesCount = 0;
        var entryIndex = 0;

        foreach (var entry in entries)
        {
          cancellationToken.ThrowIfCancellationRequested();
          entryIndex++;
          var currentProgressBase = totalEntries == 0
            ? 15d
            : 15d + ((entryIndex - 1d) / totalEntries) * 75d;

          ReportProgress(
            progress,
            "Поиск исходного OPK",
            $"Файл {entryIndex} из {totalEntries}: {entry.NameExt}",
            entry.NameExt,
            entryIndex - 1,
            totalEntries,
            currentProgressBase);

          var sourceOpkPath = ResolveSourceOpkPath(entry, availableOpkFiles);
          if (string.IsNullOrWhiteSpace(sourceOpkPath))
          {
            failedEntriesCount++;
            preparationFailedFilesCount++;
            continue;
          }

          ReportProgress(
            progress,
            "Конвертация OPK в PK",
            $"Преобразуем {Path.GetFileName(sourceOpkPath)} в промежуточный PK.",
            Path.GetFileName(sourceOpkPath),
            entryIndex - 1,
            totalEntries,
            currentProgressBase + 8d);

          var pkResult = _opkToPkConverter.Convert(sourceOpkPath, intermediateDirectory);
          if (!pkResult.Success || string.IsNullOrWhiteSpace(pkResult.OutputPath))
          {
            failedEntriesCount++;
            preparationFailedFilesCount++;
            continue;
          }

          ReportProgress(
            progress,
            "Конвертация PK в OPKW",
            $"Транслируем {Path.GetFileName(pkResult.OutputPath)} в OPKW.",
            Path.GetFileName(pkResult.OutputPath),
            entryIndex - 1,
            totalEntries,
            currentProgressBase + 14d);

          var opkwResult = _pkToOpkwConverter.Convert(pkResult.OutputPath, intermediateDirectory);
          if (!opkwResult.Success)
          {
            failedEntriesCount++;
            translationFailedFilesCount++;
            translationErrorCount += opkwResult.ErrorCount;
            problemPkPaths.Add(pkResult.OutputPath);
            continue;
          }

          if (string.IsNullOrWhiteSpace(opkwResult.OutputPath))
          {
            failedEntriesCount++;
            preparationFailedFilesCount++;
            continue;
          }

          successfulEntries.Add(new PreparedArchiveEntry
          {
            Entry = entry,
            EntryIndex = entryIndex,
            OpkwPath = opkwResult.OutputPath
          });

          var processedEntries = entryIndex;
          var percent = totalEntries == 0
            ? 90d
            : 15d + (processedEntries / (double)totalEntries) * 75d;
          ReportProgress(
            progress,
            "Файл подготовлен",
            $"Готово {processedEntries} из {totalEntries}: {Path.GetFileName(opkwResult.OutputPath)}",
            Path.GetFileName(opkwResult.OutputPath),
            processedEntries,
            totalEntries,
            percent);
        }

        if (failedEntriesCount > 0)
        {
          ReportProgress(
            progress,
            "Конвертация завершена с ошибками",
            "Промежуточные файлы сохранены. Подготавливаем результат.",
            null,
            totalEntries,
            totalEntries,
            100d);

          return CreateFailedResult(
            sourcePath,
            BuildFailureSummary(
              translationErrorCount,
              translationFailedFilesCount,
              preparationFailedFilesCount),
            intermediateDirectoryPath: intermediateDirectory,
            problemPkPaths: problemPkPaths
              .Where(File.Exists)
              .Distinct(StringComparer.OrdinalIgnoreCase)
              .ToList(),
            failedEntriesCount: failedEntriesCount,
            translationErrorCount: translationErrorCount,
            translationFailedFilesCount: translationFailedFilesCount,
            preparationFailedFilesCount: preparationFailedFilesCount);
        }

        ReportProgress(
          progress,
          "Сборка нового архива",
          $"Упаковываем {successfulEntries.Count} файлов в новый APKW.",
          null,
          totalEntries,
          totalEntries,
          92d);
        return CreateArchiveResult(sourcePath, successfulEntries, intermediateDirectory, progress);
      }
      catch (OperationCanceledException)
      {
        return CreateFailedResult(inputPath, "Конвертация отменена.");
      }
      catch (Exception ex)
      {
        return CreateFailedResult(inputPath, ex.Message);
      }
    }

    private ApkToApkwConversionResult CreateArchiveResult(
      string sourcePath,
      IReadOnlyList<PreparedArchiveEntry> successfulEntries,
      string intermediateDirectory,
      IProgress<ApkToApkwProgressInfo>? progress)
    {
      using var archiveManager = new ArchiveManager();
      var stagingDirectory = CreateTemporaryWorkingDirectory();
      string? createdArchivePath = null;

      try
      {
        createdArchivePath = CreateUniqueArchive(archiveManager, Path.GetFileNameWithoutExtension(sourcePath));
        var usedFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < successfulEntries.Count; index++)
        {
          var item = successfulEntries[index];
          var archiveEntryFileName = BuildUniqueArchiveEntryFileName(item.Entry, item.EntryIndex, usedFileNames);
          var archiveSourceFilePath = PrepareArchiveEntryFile(item.OpkwPath, stagingDirectory, archiveEntryFileName);

          var progressValue = successfulEntries.Count == 0
            ? 98d
            : 92d + ((index + 1d) / successfulEntries.Count) * 8d;
          ReportProgress(
            progress,
            "Сборка нового архива",
            $"Добавляем в архив {index + 1} из {successfulEntries.Count}: {archiveEntryFileName}",
            archiveEntryFileName,
            index + 1,
            successfulEntries.Count,
            progressValue);

          archiveManager.OpenArchive(createdArchivePath);
          archiveManager.AddFileToOpenedArchive(archiveSourceFilePath);
        }

        return new ApkToApkwConversionResult
        {
          InputPath = sourcePath,
          CreatedArchivePath = createdArchivePath,
          IntermediateDirectoryPath = intermediateDirectory,
          EntriesCount = successfulEntries.Count,
          Success = true,
        };
      }
      catch (Exception ex)
      {
        if (!string.IsNullOrWhiteSpace(createdArchivePath))
        {
          TryDeleteFile(createdArchivePath);
        }

        return CreateFailedResult(sourcePath, ex.Message, intermediateDirectoryPath: intermediateDirectory);
      }
      finally
      {
        TryDeleteDirectory(stagingDirectory);
      }
    }

    private static ApkToApkwConversionResult CreateFailedResult(
      string? inputPath,
      string errorMessage,
      string? intermediateDirectoryPath = null,
      string? failedEntryName = null,
      string? failedSourceOpkPath = null,
      IReadOnlyList<string>? problemPkPaths = null,
      int failedEntriesCount = 0,
      int translationErrorCount = 0,
      int translationFailedFilesCount = 0,
      int preparationFailedFilesCount = 0)
    {
      return new ApkToApkwConversionResult
      {
        InputPath = string.IsNullOrWhiteSpace(inputPath) ? string.Empty : Path.GetFullPath(inputPath),
        IntermediateDirectoryPath = intermediateDirectoryPath,
        FailedEntryName = failedEntryName,
        FailedSourceOpkPath = failedSourceOpkPath,
        ProblemPkPaths = problemPkPaths ?? [],
        FailedEntriesCount = failedEntriesCount,
        TranslationErrorCount = translationErrorCount,
        TranslationFailedFilesCount = translationFailedFilesCount,
        PreparationFailedFilesCount = preparationFailedFilesCount,
        Success = false,
        ErrorMessage = errorMessage,
      };
    }

    private static string BuildFailureSummary(
      int translationErrorCount,
      int translationFailedFilesCount,
      int preparationFailedFilesCount)
    {
      var lines = new List<string>
      {
        $"Найдено {translationErrorCount} ошибок в {translationFailedFilesCount} файлах."
      };

      if (preparationFailedFilesCount > 0)
      {
        lines.Add($"Ещё {preparationFailedFilesCount} файлов не удалось подготовить к трансляции.");
      }

      return string.Join(Environment.NewLine, lines);
    }

    private static void ReportProgress(
      IProgress<ApkToApkwProgressInfo>? progress,
      string stage,
      string hint,
      string? currentFileName,
      int processedEntries,
      int totalEntries,
      double percent)
    {
      progress?.Report(new ApkToApkwProgressInfo
      {
        Stage = stage,
        Hint = hint,
        CurrentFileName = currentFileName,
        ProcessedEntries = processedEntries,
        TotalEntries = totalEntries,
        Percent = Math.Clamp(percent, 0d, 100d),
      });
    }

    private static string CreateIntermediateOutputDirectory(string sourcePath)
    {
      var sourceDirectory = Path.GetDirectoryName(sourcePath)
        ?? throw new DirectoryNotFoundException("Не удалось определить каталог исходного APK-архива.");
      var baseDirectoryName = Path.GetFileNameWithoutExtension(sourcePath) + "_intermediate";
      var candidatePath = Path.Combine(sourceDirectory, baseDirectoryName);

      if (!Directory.Exists(candidatePath))
      {
        Directory.CreateDirectory(candidatePath);
        return candidatePath;
      }

      var suffix = 1;
      while (true)
      {
        candidatePath = Path.Combine(sourceDirectory, $"{baseDirectoryName}_{suffix}");
        if (!Directory.Exists(candidatePath))
        {
          Directory.CreateDirectory(candidatePath);
          return candidatePath;
        }

        suffix++;
      }
    }

    private static string CreateUniqueArchive(ArchiveManager archiveManager, string preferredName)
    {
      var normalizedBaseName = SanitizeFileName(preferredName, "converted_archive");
      var candidateName = normalizedBaseName;
      var suffix = 1;

      while (true)
      {
        try
        {
          return archiveManager.CreateArchive(candidateName);
        }
        catch (InvalidOperationException)
        {
          candidateName = $"{normalizedBaseName}_{suffix}";
          suffix++;
        }
      }
    }

    private static string? ResolveSourceOpkPath(ApkArchiveEntry entry, IReadOnlyList<string> candidateFiles)
    {
      if (string.IsNullOrWhiteSpace(entry.NameExt))
      {
        return null;
      }

      var targetFileName = Path.GetFileName(entry.NameExt);

      var directMatch = candidateFiles.FirstOrDefault(filePath =>
        string.Equals(Path.GetFileName(filePath), targetFileName, StringComparison.OrdinalIgnoreCase));
      if (!string.IsNullOrWhiteSpace(directMatch))
      {
        return directMatch;
      }

      var shortNameMatch = candidateFiles.FirstOrDefault(filePath =>
        string.Equals(GetShortFileName(filePath), targetFileName, StringComparison.OrdinalIgnoreCase));
      if (!string.IsNullOrWhiteSpace(shortNameMatch))
      {
        return shortNameMatch;
      }

      var normalizedTarget = NormalizeLegacyName(targetFileName);
      if (!string.IsNullOrWhiteSpace(normalizedTarget))
      {
        var normalizedMatches = candidateFiles
          .Where(filePath =>
          {
            var normalizedCandidate = NormalizeLegacyName(Path.GetFileName(filePath));
            return string.Equals(normalizedCandidate, normalizedTarget, StringComparison.OrdinalIgnoreCase)
              || normalizedCandidate.StartsWith(normalizedTarget, StringComparison.OrdinalIgnoreCase);
          })
          .ToList();

        if (normalizedMatches.Count == 1)
        {
          return normalizedMatches[0];
        }
      }

      var targetStem = Path.GetFileNameWithoutExtension(targetFileName);
      var tildeIndex = targetStem.IndexOf('~');
      if (tildeIndex > 0)
      {
        var prefix = targetStem[..tildeIndex];
        var prefixMatches = candidateFiles
          .Where(filePath => Path.GetFileNameWithoutExtension(filePath)
            .StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
          .ToList();

        if (prefixMatches.Count == 1)
        {
          return prefixMatches[0];
        }
      }

      return null;
    }

    private static string BuildUniqueArchiveEntryFileName(ApkArchiveEntry entry, int entryIndex, ISet<string> usedFileNames)
    {
      var preferredBaseName = !string.IsNullOrWhiteSpace(entry.NameExt)
        ? Path.GetFileNameWithoutExtension(entry.NameExt)
        : $"archive_entry_{entryIndex:D3}";

      preferredBaseName = SanitizeFileName(preferredBaseName, $"archive_entry_{entryIndex:D3}");
      var candidateFileName = preferredBaseName + ".opkw";
      var suffix = 1;

      while (!usedFileNames.Add(candidateFileName))
      {
        candidateFileName = $"{preferredBaseName}_{suffix}.opkw";
        suffix++;
      }

      return candidateFileName;
    }

    private static string PrepareArchiveEntryFile(string sourceOpkwPath, string temporaryDirectory, string archiveEntryFileName)
    {
      Directory.CreateDirectory(temporaryDirectory);

      var archiveSourceFilePath = Path.Combine(temporaryDirectory, archiveEntryFileName);
      if (string.Equals(sourceOpkwPath, archiveSourceFilePath, StringComparison.OrdinalIgnoreCase))
      {
        return archiveSourceFilePath;
      }

      File.Copy(sourceOpkwPath, archiveSourceFilePath, overwrite: true);
      return archiveSourceFilePath;
    }

    private static string CreateTemporaryWorkingDirectory()
    {
      var directoryPath = Path.Combine(
        Path.GetTempPath(),
        "AskMkiM",
        "ApkToApkw",
        Guid.NewGuid().ToString("N"));

      Directory.CreateDirectory(directoryPath);
      return directoryPath;
    }

    private static string SanitizeFileName(string? value, string fallback)
    {
      var normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
      normalized = Path.GetFileNameWithoutExtension(normalized);

      foreach (var invalidChar in Path.GetInvalidFileNameChars())
      {
        normalized = normalized.Replace(invalidChar, '_');
      }

      return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }

    private static void TryDeleteFile(string? path)
    {
      if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
      {
        return;
      }

      try
      {
        File.Delete(path);
      }
      catch
      {
      }
    }

    private static void TryDeleteDirectory(string? path)
    {
      if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
      {
        return;
      }

      try
      {
        Directory.Delete(path, recursive: true);
      }
      catch
      {
      }
    }

    private static string GetShortFileName(string path)
    {
      if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
      {
        return string.Empty;
      }

      var buffer = new StringBuilder(260);
      var length = GetShortPathName(path, buffer, (uint)buffer.Capacity);
      if (length == 0)
      {
        return string.Empty;
      }

      return Path.GetFileName(buffer.ToString());
    }

    private static string NormalizeLegacyName(string fileName)
    {
      if (string.IsNullOrWhiteSpace(fileName))
      {
        return string.Empty;
      }

      var stem = Path.GetFileNameWithoutExtension(fileName);
      var tildeIndex = stem.IndexOf('~');
      if (tildeIndex >= 0)
      {
        stem = stem[..tildeIndex];
      }

      var builder = new StringBuilder(stem.Length);
      foreach (var ch in stem)
      {
        if (char.IsLetterOrDigit(ch))
        {
          builder.Append(char.ToUpperInvariant(ch));
        }
      }

      return builder.ToString();
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetShortPathName(string longPath, StringBuilder shortPath, uint bufferLength);

    private static Encoding CreateOemEncoding()
    {
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
      return Encoding.GetEncoding(866);
    }

    private static uint ComputeCrc32(byte[] bytes)
    {
      const uint polynomial = 0xEDB88320u;
      uint crc = ~0u;

      foreach (var currentByte in bytes)
      {
        crc ^= currentByte;
        for (var bitIndex = 0; bitIndex < 8; bitIndex++)
        {
          var carry = (crc & 1u) != 0;
          crc >>= 1;
          if (carry)
          {
            crc ^= polynomial;
          }
        }
      }

      return ~crc;
    }

    private static string DecodeOemString(byte[]? bytes)
    {
      if (bytes == null || bytes.Length == 0)
      {
        return string.Empty;
      }

      var endIndex = Array.IndexOf(bytes, (byte)0);
      var count = endIndex >= 0 ? endIndex : bytes.Length;
      return OemEncoding.GetString(bytes, 0, count).Trim();
    }

    private sealed class PreparedArchiveEntry
    {
      public ApkArchiveEntry Entry { get; init; } = null!;

      public int EntryIndex { get; init; }

      public string OpkwPath { get; init; } = string.Empty;
    }

    private sealed class ApkFile
    {
      public VrNode VrAll { get; private init; } = null!;

      public static ApkFile Read(string path)
      {
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length < ApkSignature.Length + sizeof(uint) * 2)
        {
          throw new InvalidDataException("Файл слишком короткий для APK.");
        }

        var body = new byte[bytes.Length - sizeof(uint)];
        Buffer.BlockCopy(bytes, 0, body, 0, body.Length);

        var expectedCrc = ComputeCrc32(body);
        var actualCrc = BitConverter.ToUInt32(bytes, bytes.Length - sizeof(uint));
        if (expectedCrc != actualCrc)
        {
          throw new InvalidDataException("CRC APK не совпадает.");
        }

        using var stream = new MemoryStream(bytes, writable: false);
        using var reader = new BinaryReader(stream);

        var signature = reader.ReadBytes(ApkSignature.Length);
        if (!signature.SequenceEqual(ApkSignature))
        {
          throw new InvalidDataException("Сигнатура APK не совпадает с ожидаемым форматом.");
        }

        _ = reader.ReadUInt32();
        var vrAll = ReadVr(reader);

        if (reader.BaseStream.Position != reader.BaseStream.Length - sizeof(uint))
        {
          throw new InvalidDataException("После блока vrall остались лишние байты.");
        }

        return new ApkFile
        {
          VrAll = vrAll
        };
      }

      public IReadOnlyList<ApkArchiveEntry> GetEntries()
      {
        return VrAll.Records
          .Select(record => new ApkArchiveEntry
          {
            NameExt = DecodeOemString(record.KeyBytes)
          })
          .Where(entry => !string.IsNullOrWhiteSpace(entry.NameExt))
          .ToList();
      }

      private static VrNode ReadVr(BinaryReader reader)
      {
        var marker = reader.ReadBytes(3);
        if (marker.Length != 3)
        {
          throw new EndOfStreamException("Не удалось прочитать маркер VR.");
        }

        var isRecord = marker.SequenceEqual(VrMarkerRecord);
        var isOrdinary = marker.SequenceEqual(VrMarkerOrdinary);
        if (!isRecord && !isOrdinary)
        {
          throw new InvalidDataException("Неизвестный маркер VR.");
        }

        var node = new VrNode
        {
          IsFixedRecord = isRecord,
          RecordCount = reader.ReadUInt32()
        };

        if (isRecord)
        {
          node.FixedRecordSize = reader.ReadUInt16();
          for (uint index = 0; index < node.RecordCount; index++)
          {
            var data = reader.ReadBytes(node.FixedRecordSize);
            if (data.Length != node.FixedRecordSize)
            {
              throw new EndOfStreamException("Оборван fixed-record VR.");
            }

            node.Records.Add(new VrRecord
            {
              DataBytes = data
            });
          }
        }
        else
        {
          for (uint index = 0; index < node.RecordCount; index++)
          {
            var keyLength = reader.ReadUInt16();
            var dataLength = reader.ReadUInt16();
            var childCount = reader.ReadUInt16();

            var keyBytes = reader.ReadBytes(keyLength);
            if (keyBytes.Length != keyLength)
            {
              throw new EndOfStreamException("Оборван key в VR.");
            }

            var dataBytes = reader.ReadBytes(dataLength);
            if (dataBytes.Length != dataLength)
            {
              throw new EndOfStreamException("Оборваны data в VR.");
            }

            var record = new VrRecord
            {
              KeyBytes = keyBytes,
              DataBytes = dataBytes
            };

            for (uint childIndex = 0; childIndex < childCount; childIndex++)
            {
              record.Children.Add(ReadVr(reader));
            }

            node.Records.Add(record);
          }
        }

        var repeatedCount = reader.ReadUInt32();
        if (repeatedCount != node.RecordCount)
        {
          throw new InvalidDataException("Контрольное число записей VR не совпадает.");
        }

        var endMarker = reader.ReadBytes(3);
        if (!endMarker.SequenceEqual(VrMarkerEnd))
        {
          throw new InvalidDataException("Неверный маркер конца VR.");
        }

        return node;
      }
    }

    private sealed class VrNode
    {
      public bool IsFixedRecord { get; init; }

      public uint RecordCount { get; init; }

      public ushort FixedRecordSize { get; set; }

      public List<VrRecord> Records { get; } = new List<VrRecord>();
    }

    private sealed class VrRecord
    {
      public byte[] KeyBytes { get; init; } = Array.Empty<byte>();

      public byte[] DataBytes { get; init; } = Array.Empty<byte>();

      public List<VrNode> Children { get; } = new List<VrNode>();
    }

    private sealed class ApkArchiveEntry
    {
      public string NameExt { get; init; } = string.Empty;
    }
  }
}
