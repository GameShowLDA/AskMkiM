using System.IO;

namespace AskMkiM.Updater.Stub;

/// <summary>
/// Находит установку AskMkiM и запоминает последний выбранный путь.
/// </summary>
internal static class InstallationPathStore
{
  private static readonly string StorePath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "AskMkiM",
    "Updater",
    "target.txt");

  internal static string? FindExistingInstallation()
  {
    IEnumerable<string> candidates = ReadSavedPath()
      .Append(@"C:\AskMkiM\Bin\AskMkiM.exe")
      .Append(@"D:\AskMkiM\Bin\AskMkiM.exe");
    return candidates.FirstOrDefault(File.Exists);
  }

  internal static void Save(string executablePath)
  {
    Directory.CreateDirectory(Path.GetDirectoryName(StorePath)!);
    File.WriteAllText(StorePath, Path.GetFullPath(executablePath));
  }

  private static IEnumerable<string> ReadSavedPath()
  {
    if (!File.Exists(StorePath)) yield break;
    string value = File.ReadAllText(StorePath).Trim();
    if (!string.IsNullOrWhiteSpace(value)) yield return value;
  }
}
