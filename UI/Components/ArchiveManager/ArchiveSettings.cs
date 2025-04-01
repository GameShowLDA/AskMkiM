using System.IO;

namespace UI.Components.ArchiveManager
{
  public static class ArchiveSettings
  {
    public static string ArchivePath { get; private set; }

    public static string TempArchivePath { get; private set; }

    public static string PkPath { get; private set; }

    public static string IndexName { get; private set; }

    public static string YamlName { get; private set; }

    #region Работа с файлами и путями к файлам.

    /// <summary>
    /// Устанавливает пути к ресурсам и связанным элементам.
    /// </summary>
    static public void SetPath()
    {
      ArchivePath = ".\\Archives";
      TempArchivePath = "Temp archives";
      IndexName = "index.json";
      YamlName = "control.yaml";
    }
    #endregion

    static ArchiveSettings()
    {
      SetPath();

      if (!Directory.Exists(ArchivePath))
      {
        Directory.CreateDirectory(ArchivePath);
      }

      if (!Directory.Exists(Path.Combine(Path.GetTempPath(), ArchivePath)))
      {
        Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), ArchivePath));
      }

      var directoryInfo = new DirectoryInfo(ArchivePath);

      if (directoryInfo.Attributes != FileAttributes.Hidden)
      {
        directoryInfo.Attributes = FileAttributes.Hidden;
      }
    }
  }
}
