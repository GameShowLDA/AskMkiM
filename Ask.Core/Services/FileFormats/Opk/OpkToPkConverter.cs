using System.IO;
using System.Text;
using System.Text.Json;
using Ask.Core.Services.FileFormats;

namespace Ask.Core.Services.FileFormats.Opk
{
  /// <summary>
  /// Выполняет конвертацию OPK-файлов в PK-файлы с нормализацией содержимого.
  /// </summary>
  public sealed class OpkToPkConverter : IOpkToPkConverter
  {
    /// <summary>
    /// Хранит кодировку CP866, используемую при работе с содержимым OPK-файлов.
    /// </summary>
    private static readonly Encoding Cp866Encoding = CreateCp866Encoding();

    /// <summary>
    /// Хранит сигнатуру OPK-файла, используемую по умолчанию при чтении.
    /// </summary>
    private static readonly byte[] DefaultSignature = Cp866Encoding.GetBytes("Это файл ОПК для АСК-МКИ\n\n\0");

    /// <summary>
    /// Определяет параметры сериализации metadata-файла.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
      WriteIndented = true,
    };

    /// <summary>
    /// Читает структуру OPK-файла и извлекает его содержимое.
    /// </summary>
    private readonly OpkFileReader _reader = new(DefaultSignature);

    /// <summary>
    /// Нормализует похожие латинские символы в текстовых строках.
    /// </summary>
    private readonly LookalikeLatinToCyrillicNormalizer _lookalikeNormalizer = new(Cp866Encoding);

    /// <summary>
    /// Конвертирует указанный OPK-файл в PK-файл и сохраняет результат в целевую папку.
    /// </summary>
    /// <param name="inputPath">Путь к исходному OPK-файлу.</param>
    /// <param name="outputDirectory">Папка для сохранения результирующего PK-файла.</param>
    /// <returns>Результат выполнения конвертации.</returns>
    public ConversionResult Convert(string inputPath, string outputDirectory)
    {
      if (string.IsNullOrWhiteSpace(inputPath))
      {
        return CreateFailedResult(inputPath, "Не указан путь к входному OPK-файлу.");
      }

      if (string.IsNullOrWhiteSpace(outputDirectory))
      {
        return CreateFailedResult(inputPath, "Не указана папка для сохранения результата.");
      }

      try
      {
        var sourcePath = Path.GetFullPath(inputPath);
        var targetDirectory = Path.GetFullPath(outputDirectory);

        if (!File.Exists(sourcePath))
        {
          return CreateFailedResult(sourcePath, $"Файл не найден: {sourcePath}");
        }

        if (!string.Equals(Path.GetExtension(sourcePath), ".opk", StringComparison.OrdinalIgnoreCase))
        {
          return CreateFailedResult(sourcePath, "Поддерживается только конвертация файлов .opk.");
        }

        Directory.CreateDirectory(targetDirectory);

        var opkFile = _reader.Read(sourcePath);
        var normalizedTextRecords = NormalizeTextRecords(opkFile.TextRecords);
        var outputPath = BuildUniqueOutputPath(sourcePath, targetDirectory);
        var metadataPath = outputPath + ".meta.json";

        try
        {
          File.WriteAllBytes(outputPath, JoinLines(normalizedTextRecords));
          SaveMetadata(metadataPath, opkFile);
        }
        catch
        {
          TryDeleteFile(outputPath);
          TryDeleteFile(metadataPath);
          throw;
        }

        return new ConversionResult
        {
          InputPath = sourcePath,
          OutputPath = outputPath,
          MetadataPath = metadataPath,
          Success = true,
          LinesCount = normalizedTextRecords.Count,
        };
      }
      catch (Exception ex)
      {
        return CreateFailedResult(inputPath, ex.Message);
      }
    }

    /// <summary>
    /// Создаёт результат неуспешной конвертации.
    /// </summary>
    /// <param name="inputPath">Путь к исходному файлу.</param>
    /// <param name="errorMessage">Текст ошибки.</param>
    /// <returns>Результат с признаком ошибки.</returns>
    private static ConversionResult CreateFailedResult(string? inputPath, string errorMessage)
    {
      return new ConversionResult
      {
        InputPath = string.IsNullOrWhiteSpace(inputPath) ? string.Empty : Path.GetFullPath(inputPath),
        Success = false,
        ErrorMessage = errorMessage,
      };
    }

    /// <summary>
    /// Формирует уникальный путь для результирующего PK-файла в целевой папке.
    /// </summary>
    /// <param name="inputPath">Путь к исходному OPK-файлу.</param>
    /// <param name="outputDirectory">Целевая папка для сохранения.</param>
    /// <returns>Уникальный путь к PK-файлу.</returns>
    private static string BuildUniqueOutputPath(string inputPath, string outputDirectory)
    {
      var baseFileName = Path.GetFileNameWithoutExtension(inputPath);
      var candidatePath = Path.Combine(outputDirectory, baseFileName + ".pk");

      if (!File.Exists(candidatePath) && !File.Exists(candidatePath + ".meta.json"))
      {
        return candidatePath;
      }

      var index = 1;
      while (true)
      {
        candidatePath = Path.Combine(outputDirectory, $"{baseFileName}_{index}.pk");
        if (!File.Exists(candidatePath) && !File.Exists(candidatePath + ".meta.json"))
        {
          return candidatePath;
        }

        index++;
      }
    }

    /// <summary>
    /// Сохраняет metadata-файл рядом с результирующим PK-файлом.
    /// </summary>
    /// <param name="metadataPath">Путь к metadata-файлу.</param>
    /// <param name="opkFile">Содержимое исходного OPK-файла.</param>
    private static void SaveMetadata(string metadataPath, OpkFileContent opkFile)
    {
      var metadata = new PkMetadata
      {
        SignatureBase64 = System.Convert.ToBase64String(opkFile.SignatureBytes),
        VkeyBlockBase64 = System.Convert.ToBase64String(opkFile.VkeyBlock),
        VbinBlockBase64 = System.Convert.ToBase64String(opkFile.VbinBlock),
      };

      var json = JsonSerializer.Serialize(metadata, JsonOptions);
      File.WriteAllText(metadataPath, json, Encoding.UTF8);
    }

    /// <summary>
    /// Объединяет набор строк в единый PK-документ с переводами строк в формате CRLF.
    /// </summary>
    /// <param name="lines">Строки, которые требуется объединить.</param>
    /// <returns>Байтовое представление итогового PK-файла.</returns>
    private static byte[] JoinLines(IReadOnlyList<byte[]> lines)
    {
      using var stream = new MemoryStream();

      for (var index = 0; index < lines.Count; index++)
      {
        stream.Write(lines[index], 0, lines[index].Length);
        if (index + 1 < lines.Count)
        {
          stream.WriteByte(0x0D);
          stream.WriteByte(0x0A);
        }
      }

      return stream.ToArray();
    }

    /// <summary>
    /// Нормализует набор строк, подготавливая их к записи в PK-файл.
    /// </summary>
    /// <param name="lines">Исходные строки из блока vtxt.</param>
    /// <returns>Нормализованный набор строк.</returns>
    private IReadOnlyList<byte[]> NormalizeTextRecords(IReadOnlyList<byte[]> lines)
    {
      var normalizedRecords = new List<byte[]>(lines.Count);

      foreach (var line in lines)
      {
        var trimmedLine = TrimAfterFirstNullByte(line);
        if (trimmedLine.Length == 0 && Array.IndexOf(line, (byte)0x00) >= 0)
        {
          continue;
        }

        normalizedRecords.Add(_lookalikeNormalizer.Normalize(trimmedLine));
      }

      return normalizedRecords;
    }

    /// <summary>
    /// Обрезает строку по первому нулевому байту.
    /// </summary>
    /// <param name="bytes">Исходная строка в байтовом представлении.</param>
    /// <returns>Байты строки до первого нулевого байта.</returns>
    private static byte[] TrimAfterFirstNullByte(byte[] bytes)
    {
      if (bytes.Length == 0)
      {
        return bytes;
      }

      var firstNullIndex = Array.IndexOf(bytes, (byte)0x00);
      if (firstNullIndex < 0)
      {
        return bytes;
      }

      if (firstNullIndex == 0)
      {
        return [];
      }

      var normalizedBytes = new byte[firstNullIndex];
      Buffer.BlockCopy(bytes, 0, normalizedBytes, 0, firstNullIndex);
      return normalizedBytes;
    }

    /// <summary>
    /// Пытается удалить файл, игнорируя ошибки очистки.
    /// </summary>
    /// <param name="path">Путь к файлу, который нужно удалить.</param>
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
        // Игнорируем ошибки очистки частично созданных файлов.
      }
    }

    /// <summary>
    /// Создаёт экземпляр кодировки CP866 и регистрирует необходимые провайдеры.
    /// </summary>
    /// <returns>Экземпляр кодировки CP866.</returns>
    private static Encoding CreateCp866Encoding()
    {
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
      return Encoding.GetEncoding(866);
    }

    /// <summary>
    /// Описывает metadata, сохраняемую рядом с PK-файлом.
    /// </summary>
    private sealed class PkMetadata
    {
      /// <summary>
      /// Получает сигнатуру исходного OPK-файла в формате Base64.
      /// </summary>
      public string SignatureBase64 { get; init; } = string.Empty;

      /// <summary>
      /// Получает блок vkey в формате Base64.
      /// </summary>
      public string VkeyBlockBase64 { get; init; } = string.Empty;

      /// <summary>
      /// Получает блок vbin в формате Base64.
      /// </summary>
      public string VbinBlockBase64 { get; init; } = string.Empty;
    }
  }
}
