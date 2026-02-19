using System.IO;

namespace MainWindowProgram.Init
{
  /// <summary>
  /// Централизованный список расширений, которые приложение поддерживает для прямого открытия.
  /// </summary>
  internal static class SupportedFileExtensions
  {
    private static readonly HashSet<string> _supportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
      ".pk",
      ".pkw",
      ".opk",
      ".opkw",
      ".lst",
      ".lstw",
      ".acs",
      ".txt"
    };

    private static readonly HashSet<string> _associationExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
      ".pk",
      ".pkw",
      ".opk",
      ".opkw",
      ".lst",
      ".lstw",
      ".acs"
    };

    internal static IReadOnlyCollection<string> All => _supportedExtensions;
    internal static IReadOnlyCollection<string> ExplorerAssociationExtensions => _associationExtensions;

    internal static bool IsSupportedPath(string? path)
    {
      if (string.IsNullOrWhiteSpace(path))
      {
        return false;
      }

      return IsSupportedExtension(Path.GetExtension(path));
    }

    internal static IReadOnlyList<string> ExtractSupportedExistingFiles(IEnumerable<string>? args)
    {
      var result = new List<string>();

      if (args == null)
      {
        return result;
      }

      foreach (var arg in args)
      {
        if (TryResolveSupportedExistingFile(arg, out var fullPath) && !result.Contains(fullPath, StringComparer.OrdinalIgnoreCase))
        {
          result.Add(fullPath);
        }
      }

      return result;
    }

    internal static bool TryResolveSupportedExistingFile(string? rawArg, out string fullPath)
    {
      fullPath = string.Empty;

      if (string.IsNullOrWhiteSpace(rawArg))
      {
        return false;
      }

      var candidate = rawArg.Trim().Trim('"');
      if (string.IsNullOrWhiteSpace(candidate))
      {
        return false;
      }

      try
      {
        fullPath = Path.GetFullPath(candidate);
      }
      catch
      {
        return false;
      }

      if (!File.Exists(fullPath))
      {
        return false;
      }

      if (!IsSupportedExtension(Path.GetExtension(fullPath)))
      {
        return false;
      }

      return true;
    }

    private static bool IsSupportedExtension(string? extension)
    {
      if (string.IsNullOrWhiteSpace(extension))
      {
        return false;
      }

      return _supportedExtensions.Contains(extension);
    }
  }
}
