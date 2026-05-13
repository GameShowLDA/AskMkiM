namespace Ask.Core.Shared.DTO.Settings;

/// <summary>
/// Настройки файловых диалогов.
/// </summary>
public sealed class FileDialogSettings
{
  public string LastDirectoryPath { get; set; } = string.Empty;
}