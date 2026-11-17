namespace Ask.Core.Shared.DTO.Settings;

/// <summary>
/// Настройки файловых диалогов.
/// </summary>
public sealed class FileDialogSettings
{
  /// <summary>Пути последних открытых файлов, от нового к старому.</summary>
  public List<string> RecentFiles { get; set; } = new();

  public string LastDirectoryPath { get; set; } = string.Empty;
}