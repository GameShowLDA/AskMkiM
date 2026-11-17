namespace AskMkiM.Updater.Core.Models;

/// <summary>
/// Содержит сведения о найденной сборке AskMkiM.
/// </summary>
public sealed record ApplicationVersionInfo(
  string ExecutablePath,
  string Version,
  string BuildIdentifier);
