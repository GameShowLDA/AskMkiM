using Ask.Core.Services.App;
using Ask.Core.Services.FilesUtility;
using Ask.Core.Shared.DTO.Settings;
using System.IO;

namespace Ask.UI.UnitTests.Services.FilesUtility;

public sealed class LastDirectoryServiceTests
{
  [Fact]
  public void RecentFiles_PersistOrderLimitAndDirectory_AndIgnoreMissingFiles()
  {
    string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings", "fileDialogSettings.yaml");
    byte[]? originalSettings = File.Exists(settingsPath) ? File.ReadAllBytes(settingsPath) : null;
    string folder = Path.Combine(Path.GetTempPath(), "ask-recent-files-test-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(folder);
    Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
    try
    {
      File.WriteAllText(settingsPath, "lastDirectoryPath: '" + folder.Replace("\\", "/") + "'");
      Assert.Empty(LastDirectoryService.GetRecentFiles());
      string[] files = Enumerable.Range(1, 4).Select(i => Path.Combine(folder, $"file{i}.txt")).ToArray();
      foreach (string file in files)
      {
        File.WriteAllText(file, "test");
        LastDirectoryService.RememberFile(file);
      }

      Assert.Equal(new[] { files[3], files[2], files[1] }, LastDirectoryService.GetRecentFiles());
      LastDirectoryService.RememberFile(files[2].ToUpperInvariant());
      Assert.Equal(new[] { files[2], files[3], files[1] },
        LastDirectoryService.GetRecentFiles(), StringComparer.OrdinalIgnoreCase);
      LastDirectoryService.SaveLastDirectory(folder);
      var settings = new YamlService<FileDialogSettings>(settingsPath).Load();
      Assert.Equal(folder, settings.LastDirectoryPath);
      Assert.Equal(3, settings.RecentFiles.Count);
      Assert.Equal(files[2], settings.RecentFiles[0], ignoreCase: true);

      File.Delete(files[2]);
      Assert.Equal(new[] { files[3], files[1] }, LastDirectoryService.GetRecentFiles());
      LastDirectoryService.RememberFile(Path.Combine(folder, "missing.txt"));
      Assert.Equal(new[] { files[3], files[1] }, LastDirectoryService.GetRecentFiles());
    }
    finally
    {
      if (originalSettings == null)
      {
        File.Delete(settingsPath);
      }
      else
      {
        File.WriteAllBytes(settingsPath, originalSettings);
      }

      Directory.Delete(folder, recursive: true);
    }
  }
}
