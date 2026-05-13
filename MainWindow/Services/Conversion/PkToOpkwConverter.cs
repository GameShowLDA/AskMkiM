using Ask.Core.Services.FileFormats;
using Ask.Engine.ControlCommandAnalyser;
using System.IO;
using System.Text;

namespace MainWindowProgram.Services.Conversion
{
  /// <summary>
  /// Converts PK and PKW control-program files into OPKW files using the application's translation pipeline.
  /// </summary>
  public sealed class PkToOpkwConverter
  {
    public PkToOpkwConversionResult Convert(string inputPath, string outputDirectory)
    {
      if (string.IsNullOrWhiteSpace(inputPath))
      {
        return CreateFailedResult(inputPath, "Не указан путь к исходному PK/PKW-файлу.");
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

        var extension = Path.GetExtension(sourcePath);
        if (!string.Equals(extension, ".pk", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(extension, ".pkw", StringComparison.OrdinalIgnoreCase))
        {
          return CreateFailedResult(sourcePath, "Поддерживается только конвертация файлов .pk и .pkw.");
        }

        Directory.CreateDirectory(targetDirectory);

        var sourceText = ReadSourceText(sourcePath);
        var manager = new CommandTranslationManager();
        var translation = manager.BuildTranslation(sourceText);
        var warningCount = translation.Models.Sum(model => model.Warnings?.Count ?? 0);
        var errorCount = translation.Models.Sum(model => model.Errors?.Count ?? 0);
        if (errorCount > 0)
        {
          AnnotateSourcePk(sourcePath, sourceText, errorCount);

          return new PkToOpkwConversionResult
          {
            InputPath = sourcePath,
            SavedWithErrors = true,
            Success = false,
            ErrorCount = errorCount,
            WarningCount = warningCount,
            LinesCount = CountLines(sourceText) + 2,
            ErrorMessage = $"Трансляция завершилась с ошибками: {errorCount}. Пометка добавлена в первую строку промежуточного PK.",
          };
        }

        var outputPath = BuildUniqueOutputPath(sourcePath, targetDirectory);
        File.WriteAllText(outputPath, translation.FormattedText, new UTF8Encoding(false));

        return new PkToOpkwConversionResult
        {
          InputPath = sourcePath,
          OutputPath = outputPath,
          Success = true,
          ErrorCount = 0,
          LinesCount = CountLines(translation.FormattedText),
          WarningCount = warningCount,
        };
      }
      catch (Exception ex)
      {
        return CreateFailedResult(inputPath, ex.Message);
      }
    }

    private static PkToOpkwConversionResult CreateFailedResult(string? inputPath, string errorMessage)
    {
      return new PkToOpkwConversionResult
      {
        InputPath = string.IsNullOrWhiteSpace(inputPath) ? string.Empty : Path.GetFullPath(inputPath),
        Success = false,
        ErrorMessage = errorMessage,
      };
    }

    private static void AnnotateSourcePk(string sourcePath, string sourceText, int errorCount)
    {
      var header = $"//=======НАЙДЕНО {errorCount} ОШИБОК";
      var annotatedText = string.IsNullOrWhiteSpace(sourceText)
        ? header + "\r\n"
        : header + "\r\n\r\n" + sourceText.Replace("\n", "\r\n");

      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
      var encoding = string.Equals(Path.GetExtension(sourcePath), ".pkw", StringComparison.OrdinalIgnoreCase)
        ? Encoding.UTF8
        : Encoding.GetEncoding(866);

      if (encoding == Encoding.UTF8)
      {
        File.WriteAllText(sourcePath, annotatedText, new UTF8Encoding(false));
        return;
      }

      File.WriteAllText(sourcePath, annotatedText, encoding);
    }

    private static string ReadSourceText(string path)
    {
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

      var encoding = string.Equals(Path.GetExtension(path), ".pkw", StringComparison.OrdinalIgnoreCase)
        ? Encoding.UTF8
        : Encoding.GetEncoding(866);

      return CommandTranslationManager.NormalizeCommandMnemonics(
          TextSanitizer.RemoveLegacyControlChars(File.ReadAllText(path, encoding)))
        .Replace("\r\n", "\n")
        .Replace('\r', '\n');
    }
    private static string BuildUniqueOutputPath(string inputPath, string outputDirectory)
    {
      var baseFileName = Path.GetFileNameWithoutExtension(inputPath);
      var candidatePath = Path.Combine(outputDirectory, baseFileName + ".opkw");
      if (!File.Exists(candidatePath))
      {
        return candidatePath;
      }

      var index = 1;
      while (true)
      {
        candidatePath = Path.Combine(outputDirectory, $"{baseFileName}_{index}.opkw");
        if (!File.Exists(candidatePath))
        {
          return candidatePath;
        }

        index++;
      }
    }

    private static int CountLines(string text)
    {
      if (string.IsNullOrEmpty(text))
      {
        return 0;
      }

      return text
        .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None)
        .Length;
    }
  }
}
