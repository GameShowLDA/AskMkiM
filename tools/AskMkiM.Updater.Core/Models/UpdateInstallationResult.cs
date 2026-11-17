namespace AskMkiM.Updater.Core.Models;

/// <summary>
/// Содержит результат успешной установки обновления.
/// </summary>
public sealed record UpdateInstallationResult(
  ApplicationVersionInfo PreviousVersion,
  UpdateManifest InstalledManifest,
  string BackupDirectory,
  int ReplacedFiles,
  int RemovedFiles);
