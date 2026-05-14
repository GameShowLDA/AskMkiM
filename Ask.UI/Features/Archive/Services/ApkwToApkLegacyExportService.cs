using Ask.Core.Services.FileFormats;
using Ask.Engine.ControlCommandAnalyser;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace Ask.UI.Features.Archive.Services
{
  public sealed class ApkwToApkLegacyExportService
  {
    private static readonly byte[] ApkSignature = Convert.FromHexString("9DE2AE20A0E0E5A8A2208F8A20A4ABEF2080918A2D8C8A880A0A00");
    private static readonly byte[] VrMarkerOrdinary = { 0x02, 0x15, 0x00 };
    private static readonly byte[] VrMarkerEnd = { 0x01, 0x21, 0x00 };
    private static readonly Encoding Cp866Encoding = CreateCp866Encoding();
    private static readonly byte[] OpkSignature = Cp866Encoding.GetBytes("Это файл ОПК для АСК-МКИ\n\n\0");

    public Task<ApkwToApkLegacyExportResult> ExportAsync(
      string inputArchivePath,
      string outputDirectory,
      IProgress<ApkwToApkLegacyExportProgress>? progress = null,
      CancellationToken cancellationToken = default)
    {
      return Task.Run(() => Export(inputArchivePath, outputDirectory, progress, cancellationToken), cancellationToken);
    }

    private static ApkwToApkLegacyExportResult Export(
      string inputArchivePath,
      string outputDirectory,
      IProgress<ApkwToApkLegacyExportProgress>? progress,
      CancellationToken cancellationToken)
    {
      if (string.IsNullOrWhiteSpace(inputArchivePath))
      {
        return CreateFailedResult(inputArchivePath, "Не указан путь к APKW-архиву.");
      }

      if (string.IsNullOrWhiteSpace(outputDirectory))
      {
        return CreateFailedResult(inputArchivePath, "Не указана папка для сохранения legacy-архива.");
      }

      var createdFiles = new List<string>();

      try
      {
        var sourcePath = Path.GetFullPath(inputArchivePath);
        var selectedDirectory = Path.GetFullPath(outputDirectory);

        if (!File.Exists(sourcePath))
        {
          return CreateFailedResult(sourcePath, $"Файл не найден: {sourcePath}");
        }

        if (!string.Equals(Path.GetExtension(sourcePath), ".apkw", StringComparison.OrdinalIgnoreCase))
        {
          return CreateFailedResult(sourcePath, "Поддерживается только конвертация файлов .apkw.");
        }

        Directory.CreateDirectory(selectedDirectory);
        var targetDirectory = CreateUniqueArchiveOutputDirectory(
          selectedDirectory,
          Path.GetFileNameWithoutExtension(sourcePath));

        var entries = ReadArchiveEntries(sourcePath);
        if (entries.Count == 0)
        {
          return CreateFailedResult(sourcePath, "В APKW-архиве нет файлов OPKW для экспорта.");
        }

        var apkEntries = new List<LegacyApkEntry>(entries.Count);
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var apkPath = BuildUniqueOutputPath(targetDirectory, Path.GetFileNameWithoutExtension(sourcePath), ".apk");

        for (var index = 0; index < entries.Count; index++)
        {
          cancellationToken.ThrowIfCancellationRequested();

          var entry = entries[index];
          var opkFileName = BuildUniqueLegacyFileName(entry.Name, usedNames);
          var opkPath = Path.Combine(targetDirectory, opkFileName);

          ReportProgress(
            progress,
            "Экспорт файлов OPK",
            $"Файл {index + 1} из {entries.Count}: {opkFileName}",
            opkFileName,
            index,
            entries.Count,
            entries.Count == 0 ? 90d : (index / (double)entries.Count) * 90d);

          var apkEntry = WriteOpkFile(opkPath, opkFileName, entry.Text);
          createdFiles.Add(opkPath);
          apkEntries.Add(apkEntry);

          ReportProgress(
            progress,
            "Экспорт файлов OPK",
            $"Готово {index + 1} из {entries.Count}: {opkFileName}",
            opkFileName,
            index + 1,
            entries.Count,
            ((index + 1d) / entries.Count) * 90d);
        }

        ReportProgress(
          progress,
          "Сборка APK",
          $"Создаем legacy-индекс {Path.GetFileName(apkPath)}.",
          Path.GetFileName(apkPath),
          entries.Count,
          entries.Count,
          96d);

        WriteApkFile(apkPath, apkEntries);
        createdFiles.Add(apkPath);

        ReportProgress(
          progress,
          "Готово",
          $"Создан legacy-архив {Path.GetFileName(apkPath)}.",
          Path.GetFileName(apkPath),
          entries.Count,
          entries.Count,
          100d);

        return new ApkwToApkLegacyExportResult
        {
          InputPath = sourcePath,
          OutputApkPath = apkPath,
          OutputDirectory = targetDirectory,
          EntriesCount = entries.Count,
          Success = true,
        };
      }
      catch (OperationCanceledException)
      {
        TryDeleteFiles(createdFiles);
        return CreateFailedResult(inputArchivePath, "Конвертация отменена.");
      }
      catch (Exception ex)
      {
        TryDeleteFiles(createdFiles);
        return CreateFailedResult(inputArchivePath, ex.Message);
      }
    }

    private static IReadOnlyList<ApkwEntry> ReadArchiveEntries(string archivePath)
    {
      var entries = new List<ApkwEntry>();

      using var encryptionSession = ArchiveEncryptionSession.Acquire(archivePath);
      using var archiveStream = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
      using var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read, leaveOpen: false);

      foreach (var entry in archive.Entries
        .Where(ArchiveManifestService.IsArchiveFileEntry)
        .Where(entry => string.Equals(Path.GetExtension(entry.Name), ".opkw", StringComparison.OrdinalIgnoreCase))
        .OrderBy(entry => entry.FullName, StringComparer.OrdinalIgnoreCase))
      {
        using var stream = entry.Open();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false);
        var text = CommandTranslationManager.NormalizeCommandMnemonics(
          TextSanitizer.RemoveLegacyControlChars(reader.ReadToEnd()));

        entries.Add(new ApkwEntry(entry.Name, text));
      }

      return entries;
    }

    private static LegacyApkEntry WriteOpkFile(string path, string fileName, string text)
    {
      var lines = NormalizeTextLines(text);
      var metadata = LegacyOpkMetadata.FromText(lines, fileName);
      var vkeyBlock = CreateVkeyBlock(metadata);
      var textRecords = lines
        .Select(line => Cp866Encoding.GetBytes(line + '\0'))
        .ToList();

      using var bodyStream = new MemoryStream();
      bodyStream.Write(OpkSignature);
      bodyStream.Write(vkeyBlock);
      WriteVrOrdinary(bodyStream, []);
      WriteVrOrdinary(bodyStream, textRecords.Select(line => VrRecord.Create(Array.Empty<byte>(), line)));

      var body = bodyStream.ToArray();
      var crc = WriteWithCrc(path, body);
      var fileInfo = new FileInfo(path);

      return new LegacyApkEntry(
        fileName,
        CreateApkRecordData(fileInfo.LastWriteTime, fileInfo.Length, crc),
        vkeyBlock);
    }

    private static void WriteApkFile(string path, IReadOnlyList<LegacyApkEntry> entries)
    {
      using var bodyStream = new MemoryStream();
      bodyStream.Write(ApkSignature);
      bodyStream.Write(BitConverter.GetBytes(0u));
      WriteVrOrdinary(
        bodyStream,
        entries.Select(entry => VrRecord.Create(
          Cp866Encoding.GetBytes(entry.FileName),
          entry.RecordData,
          [entry.VkeyBlock])));

      WriteWithCrc(path, bodyStream.ToArray());
    }

    private static byte[] CreateVkeyBlock(LegacyOpkMetadata metadata)
    {
      using var stream = new MemoryStream();
      WriteVrOrdinary(stream, metadata.ToVkeyRecords());
      return stream.ToArray();
    }

    private static byte[] CreateApkRecordData(DateTime lastWriteTime, long fileLength, uint crc)
    {
      using var stream = new MemoryStream();
      using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

      writer.Write((byte)(lastWriteTime.Year >> 8));
      writer.Write((byte)lastWriteTime.Year);
      writer.Write((byte)lastWriteTime.Month);
      writer.Write((byte)lastWriteTime.Day);
      writer.Write((byte)lastWriteTime.Hour);
      writer.Write((byte)lastWriteTime.Minute);
      writer.Write((byte)lastWriteTime.Second);
      writer.Write((byte)0);
      writer.Write((uint)Math.Min(fileLength, uint.MaxValue));
      writer.Write(crc);
      writer.Write(1u);
      writer.Flush();

      return stream.ToArray();
    }

    private static void WriteVrOrdinary(Stream stream, IEnumerable<VrRecord> records)
    {
      var materializedRecords = records.ToList();
      stream.Write(VrMarkerOrdinary);
      stream.Write(BitConverter.GetBytes((uint)materializedRecords.Count));

      foreach (var record in materializedRecords)
      {
        if (record.Key.Length > ushort.MaxValue || record.Data.Length > ushort.MaxValue)
        {
          throw new InvalidDataException("Строка слишком длинная для legacy VR-формата.");
        }

        stream.Write(BitConverter.GetBytes((ushort)record.Key.Length));
        stream.Write(BitConverter.GetBytes((ushort)record.Data.Length));
        stream.Write(BitConverter.GetBytes((ushort)record.Children.Count));
        stream.Write(record.Key);
        stream.Write(record.Data);

        foreach (var child in record.Children)
        {
          stream.Write(child);
        }
      }

      stream.Write(BitConverter.GetBytes((uint)materializedRecords.Count));
      stream.Write(VrMarkerEnd);
    }

    private static uint WriteWithCrc(string path, byte[] body)
    {
      var crc = ComputeCrc32(body);
      using var fileStream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
      fileStream.Write(body);
      fileStream.Write(BitConverter.GetBytes(crc));
      return crc;
    }

    private static IReadOnlyList<string> NormalizeTextLines(string text)
    {
      var lines = text
        .Replace("\r\n", "\n")
        .Replace('\r', '\n')
        .Split('\n')
        .Select(line => line.TrimEnd('\0'))
        .ToList();

      if (lines.Count > 0 && lines[^1].Length == 0)
      {
        lines.RemoveAt(lines.Count - 1);
      }

      return lines;
    }

    private static string BuildUniqueLegacyFileName(string entryName, ISet<string> usedNames)
    {
      var baseName = SanitizeLegacyDosBaseName(Path.GetFileNameWithoutExtension(entryName));
      var candidate = baseName + ".opk";
      var suffix = 1;

      while (!usedNames.Add(candidate))
      {
        var suffixText = "~" + suffix.ToString(CultureInfo.InvariantCulture);
        var prefixLength = Math.Max(1, 8 - suffixText.Length);
        candidate = $"{baseName[..Math.Min(baseName.Length, prefixLength)]}{suffixText}.opk";
        suffix++;
      }

      return candidate;
    }

    private static string SanitizeLegacyDosBaseName(string? value)
    {
      var normalized = string.IsNullOrWhiteSpace(value) ? "archive" : value.Trim();
      var builder = new StringBuilder(8);

      foreach (var currentChar in normalized)
      {
        if (builder.Length == 8)
        {
          break;
        }

        builder.Append(IsLegacyFileNameChar(currentChar) ? currentChar : '_');
      }

      return builder.Length == 0 ? "archive" : builder.ToString();
    }

    private static bool IsLegacyFileNameChar(char value)
    {
      return char.IsLetterOrDigit(value)
             || value is '_' or '-' or '~';
    }

    private static string BuildUniqueOutputPath(string directory, string baseName, string extension)
    {
      var normalizedBaseName = SanitizeFileName(baseName, "converted_archive");
      var candidatePath = Path.Combine(directory, normalizedBaseName + extension);
      if (!File.Exists(candidatePath))
      {
        return candidatePath;
      }

      var suffix = 1;
      while (true)
      {
        candidatePath = Path.Combine(directory, $"{normalizedBaseName}_{suffix}{extension}");
        if (!File.Exists(candidatePath))
        {
          return candidatePath;
        }

        suffix++;
      }
    }

    private static string SanitizeFileName(string? value, string fallback)
    {
      var normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
      foreach (var invalidChar in Path.GetInvalidFileNameChars())
      {
        normalized = normalized.Replace(invalidChar, '_');
      }

      return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }

    private static string CreateUniqueArchiveOutputDirectory(string parentDirectory, string archiveName)
    {
      var normalizedArchiveName = SanitizeFileName(archiveName, "converted_archive");
      var candidatePath = Path.Combine(parentDirectory, normalizedArchiveName);
      if (!Directory.Exists(candidatePath))
      {
        return Directory.CreateDirectory(candidatePath).FullName;
      }

      var suffix = 1;
      while (true)
      {
        candidatePath = Path.Combine(parentDirectory, $"{normalizedArchiveName}_{suffix}");
        if (!Directory.Exists(candidatePath))
        {
          return Directory.CreateDirectory(candidatePath).FullName;
        }

        suffix++;
      }
    }

    private static uint ComputeCrc32(byte[] bytes)
    {
      const uint polynomial = 0xEDB88320u;
      var crc = ~0u;

      foreach (var currentByte in bytes)
      {
        crc ^= currentByte;
        for (var bit = 0; bit < 8; bit++)
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

    private static void ReportProgress(
      IProgress<ApkwToApkLegacyExportProgress>? progress,
      string stage,
      string hint,
      string? currentFileName,
      int processedEntries,
      int totalEntries,
      double percent)
    {
      progress?.Report(new ApkwToApkLegacyExportProgress
      {
        Stage = stage,
        Hint = hint,
        CurrentFileName = currentFileName,
        ProcessedEntries = processedEntries,
        TotalEntries = totalEntries,
        Percent = Math.Clamp(percent, 0d, 100d),
      });
    }

    private static ApkwToApkLegacyExportResult CreateFailedResult(string? inputPath, string errorMessage)
    {
      return new ApkwToApkLegacyExportResult
      {
        InputPath = string.IsNullOrWhiteSpace(inputPath) ? string.Empty : Path.GetFullPath(inputPath),
        Success = false,
        ErrorMessage = errorMessage,
      };
    }

    private static void TryDeleteFiles(IEnumerable<string> paths)
    {
      foreach (var path in paths)
      {
        try
        {
          if (File.Exists(path))
          {
            File.Delete(path);
          }
        }
        catch
        {
        }
      }
    }

    private static Encoding CreateCp866Encoding()
    {
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
      return Encoding.GetEncoding(
        866,
        EncoderFallback.ReplacementFallback,
        DecoderFallback.ReplacementFallback);
    }

    private sealed record VrRecord(byte[] Key, byte[] Data, IReadOnlyList<byte[]> Children)
    {
      public static VrRecord Create(byte[] key, byte[] data)
      {
        return new VrRecord(key, data, []);
      }

      public static VrRecord Create(byte[] key, byte[] data, IReadOnlyList<byte[]> children)
      {
        return new VrRecord(key, data, children);
      }
    }

    private sealed record LegacyApkEntry(string FileName, byte[] RecordData, byte[] VkeyBlock);

    private sealed record LegacyOpkMetadata(
      string Number,
      string Name,
      string Opk,
      string Order,
      string Workshop,
      string InformationComplex,
      IReadOnlyList<string> DesignDocuments,
      string Note)
    {
      private static readonly Regex HeaderRegex = new(
        @"^\s*\d+\s+\S+\s+(?<number>\S+)(?:\s*[* ]\s*(?<name>.+?))?\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

      public static LegacyOpkMetadata FromText(IReadOnlyList<string> lines, string fileName)
      {
        var number = string.Empty;
        var name = string.Empty;
        var opk = string.Empty;
        var order = string.Empty;
        var workshop = string.Empty;
        var informationComplex = string.Empty;
        var designDocuments = new List<string>();
        var note = string.Empty;

        foreach (var line in lines)
        {
          var trimmed = line.Trim();
          if (trimmed.Length == 0)
          {
            continue;
          }

          if (number.Length == 0)
          {
            var headerMatch = HeaderRegex.Match(trimmed);
            if (headerMatch.Success)
            {
              number = CleanMetadataValue(headerMatch.Groups["number"].Value);
              name = CleanMetadataValue(headerMatch.Groups["name"].Value);
            }
          }

          ApplyKeyValue(trimmed, "ОПК", value => opk = value);
          ApplyKeyValue(trimmed, "OPK", value => opk = value);
          ApplyKeyValue(trimmed, "ЗАКАЗ", value => order = value);
          ApplyKeyValue(trimmed, "Заказ", value => order = value);
          ApplyKeyValue(trimmed, "ЦЕХ", value => workshop = value);
          ApplyKeyValue(trimmed, "Цех", value => workshop = value);
          ApplyKeyValue(trimmed, "ИК", value => informationComplex = value);
          ApplyKeyValue(trimmed, "КД", value =>
          {
            if (!string.IsNullOrWhiteSpace(value))
            {
              designDocuments.Add(value);
            }
          });
          ApplyKeyValue(trimmed, "ПРИМ", value => note = value);
          ApplyKeyValue(trimmed, "Прим", value => note = value);
        }

        if (number.Length == 0)
        {
          number = Path.GetFileNameWithoutExtension(fileName);
        }

        if (name.Length == 0)
        {
          name = Path.GetFileNameWithoutExtension(fileName);
        }

        if (opk.Length == 0)
        {
          opk = fileName;
        }

        if (order.Length == 0)
        {
          order = "000";
        }

        if (workshop.Length == 0)
        {
          workshop = "00";
        }

        if (informationComplex.Length == 0)
        {
          informationComplex = "АСК-МКИ";
        }

        if (designDocuments.Count == 0)
        {
          designDocuments.Add(number);
        }

        return new LegacyOpkMetadata(
          number,
          name,
          opk,
          order,
          workshop,
          informationComplex,
          designDocuments,
          note);
      }

      public IReadOnlyList<VrRecord> ToVkeyRecords()
      {
        var records = new List<VrRecord>
        {
          CreateTextRecord(".NUM", Number),
          CreateTextRecord(".NAM", Name),
          CreateParentRecord("ОПК", [Opk]),
          CreateParentRecord("ЗАКАЗ", [Order]),
          CreateParentRecord("ЦЕХ", [Workshop]),
          CreateParentRecord("ИК", [InformationComplex]),
          CreateParentRecord("КД", DesignDocuments),
        };

        if (!string.IsNullOrWhiteSpace(Note))
        {
          records.Add(CreateParentRecord("ПРИМ", [Note]));
        }

        return records;
      }

      private static void ApplyKeyValue(string line, string key, Action<string> apply)
      {
        var index = line.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
          return;
        }

        var valueStart = index + key.Length;
        while (valueStart < line.Length && (line[valueStart] == ' ' || line[valueStart] == '='))
        {
          valueStart++;
        }

        if (valueStart >= line.Length)
        {
          return;
        }

        apply(CleanMetadataValue(line[valueStart..]));
      }

      private static string CleanMetadataValue(string value)
      {
        return value
          .Trim()
          .Trim('"')
          .Trim()
          .TrimEnd(';', ',')
          .Trim();
      }

      private static VrRecord CreateTextRecord(string key, string value)
      {
        return VrRecord.Create(
          Cp866Encoding.GetBytes(key + '\0'),
          Cp866Encoding.GetBytes(value + '\0'));
      }

      private static VrRecord CreateParentRecord(string key, IReadOnlyList<string> values)
      {
        return VrRecord.Create(
          Cp866Encoding.GetBytes(key + '\0'),
          Array.Empty<byte>(),
          [CreateSingleValueBlock(values)]);
      }

      private static byte[] CreateSingleValueBlock(IReadOnlyList<string> values)
      {
        using var stream = new MemoryStream();
        WriteVrOrdinary(
          stream,
          values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => VrRecord.Create(Cp866Encoding.GetBytes(value + '\0'), Array.Empty<byte>())));

        return stream.ToArray();
      }
    }

    private sealed record ApkwEntry(string Name, string Text);
  }

  public sealed class ApkwToApkLegacyExportResult
  {
    public string InputPath { get; init; } = string.Empty;

    public string? OutputApkPath { get; init; }

    public string? OutputDirectory { get; init; }

    public int EntriesCount { get; init; }

    public bool Success { get; init; }

    public string? ErrorMessage { get; init; }
  }

  public sealed class ApkwToApkLegacyExportProgress
  {
    public string Stage { get; init; } = string.Empty;

    public string Hint { get; init; } = string.Empty;

    public string? CurrentFileName { get; init; }

    public int ProcessedEntries { get; init; }

    public int TotalEntries { get; init; }

    public double Percent { get; init; }
  }
}
