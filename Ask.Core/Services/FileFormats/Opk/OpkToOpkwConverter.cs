using System.IO;

namespace Ask.Core.Services.FileFormats.Opk
{
  /// <summary>
  /// Converts a legacy OPK file to OPKW through an intermediate PK file.
  /// </summary>
  public sealed class OpkToOpkwConverter
  {
    private readonly IOpkToPkConverter _opkToPkConverter;
    private readonly Func<string, string, OpkToOpkwTranslationResult> _pkToOpkwConverter;

    public OpkToOpkwConverter(
      IOpkToPkConverter opkToPkConverter,
      Func<string, string, OpkToOpkwTranslationResult> pkToOpkwConverter)
    {
      _opkToPkConverter = opkToPkConverter ?? throw new ArgumentNullException(nameof(opkToPkConverter));
      _pkToOpkwConverter = pkToOpkwConverter ?? throw new ArgumentNullException(nameof(pkToOpkwConverter));
    }

    public OpkToOpkwConversionResult Convert(string inputPath, string outputDirectory)
    {
      if (string.IsNullOrWhiteSpace(inputPath))
      {
        return CreateFailedResult(inputPath, "Не указан путь к исходному OPK-файлу.");
      }

      if (string.IsNullOrWhiteSpace(outputDirectory))
      {
        return CreateFailedResult(inputPath, "Не указана папка для сохранения OPKW-файла.");
      }

      var intermediateDirectory = CreateTemporaryConversionDirectory();

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

        var pkResult = _opkToPkConverter.Convert(sourcePath, intermediateDirectory);
        if (!pkResult.Success || string.IsNullOrWhiteSpace(pkResult.OutputPath))
        {
          return CreateFailedResult(
            sourcePath,
            pkResult.ErrorMessage ?? "Не удалось преобразовать OPK в промежуточный PK-файл.");
        }

        var opkwResult = _pkToOpkwConverter(pkResult.OutputPath, targetDirectory);
        if (!opkwResult.Success || string.IsNullOrWhiteSpace(opkwResult.OutputPath))
        {
          return new OpkToOpkwConversionResult
          {
            InputPath = sourcePath,
            IntermediatePkPath = pkResult.OutputPath,
            Success = false,
            ErrorMessage = opkwResult.ErrorMessage ?? "Не удалось преобразовать OPK в OPKW.",
            ErrorCount = opkwResult.ErrorCount,
          };
        }

        return new OpkToOpkwConversionResult
        {
          InputPath = sourcePath,
          OutputPath = opkwResult.OutputPath,
          Success = true,
          ErrorCount = opkwResult.ErrorCount,
        };
      }
      catch (Exception ex)
      {
        return CreateFailedResult(inputPath, ex.Message);
      }
      finally
      {
        TryDeleteDirectory(intermediateDirectory);
      }
    }

    private static OpkToOpkwConversionResult CreateFailedResult(string? inputPath, string errorMessage)
    {
      return new OpkToOpkwConversionResult
      {
        InputPath = string.IsNullOrWhiteSpace(inputPath) ? string.Empty : Path.GetFullPath(inputPath),
        Success = false,
        ErrorMessage = errorMessage,
      };
    }

    private static string CreateTemporaryConversionDirectory()
    {
      var directoryPath = Path.Combine(
        Path.GetTempPath(),
        "AskMkiM",
        "OpkToOpkw",
        Guid.NewGuid().ToString("N"));

      Directory.CreateDirectory(directoryPath);
      return directoryPath;
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
  }
}
