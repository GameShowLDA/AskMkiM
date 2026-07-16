using Microsoft.CodeAnalysis;
using NLog;

namespace TestConsole.UnusedCode;

/// <summary>
/// Scans project directories and finds empty folders that may be candidates for cleanup.
/// </summary>
internal sealed class EmptyFolderScanner
{
  private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
  private static readonly string[] IgnoredDirectoryNames =
  [
    ".git",
    ".vs",
    "bin",
    "obj",
    "node_modules",
    "packages",
    "Reports"
  ];

  /// <summary>
  /// Finds empty folders under all loaded project directories.
  /// </summary>
  /// <param name="projects">The projects loaded from the solution.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>Empty folder findings grouped by owning project.</returns>
  public async Task<IReadOnlyList<EmptyFolderFinding>> ScanAsync(
    IReadOnlyList<Project> projects,
    CancellationToken cancellationToken)
  {
    await Task.Yield();
    var started = DateTimeOffset.UtcNow;
    var findings = new List<EmptyFolderFinding>();

    foreach (var project in projects)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var projectDirectory = GetProjectDirectory(project);
      if (projectDirectory is null || !Directory.Exists(projectDirectory))
      {
        continue;
      }

      Logger.Info("Empty-folder scan started: {Project}. Directory: {Directory}", project.Name, projectDirectory);
      foreach (var directory in EnumerateDirectories(projectDirectory, cancellationToken))
      {
        cancellationToken.ThrowIfCancellationRequested();
        if (IsEmptyDirectory(directory))
        {
          findings.Add(new EmptyFolderFinding(
            project.Name,
            directory,
            "Folder does not contain files or subfolders."));
        }
      }

      Logger.Info("Empty-folder scan completed: {Project}", project.Name);
    }

    var result = findings
      .DistinctBy(finding => NormalizePath(finding.Path))
      .OrderBy(finding => finding.Project)
      .ThenBy(finding => finding.Path)
      .ToArray();

    Logger.Info(
      "Empty-folder scan completed. Count: {Count}. Elapsed: {Elapsed}",
      result.Length,
      DateTimeOffset.UtcNow - started);

    return result;
  }

  private static string? GetProjectDirectory(Project project)
  {
    return string.IsNullOrWhiteSpace(project.FilePath)
      ? null
      : Path.GetDirectoryName(project.FilePath);
  }

  private static IEnumerable<string> EnumerateDirectories(string root, CancellationToken cancellationToken)
  {
    var pending = new Stack<string>();
    pending.Push(root);

    while (pending.Count > 0)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var current = pending.Pop();
      IReadOnlyList<string> children;

      try
      {
        children = Directory.EnumerateDirectories(current)
          .Where(directory => !IsIgnoredDirectory(directory))
          .ToArray();
      }
      catch (Exception ex) when (ex is not OperationCanceledException)
      {
        Logger.Warn(ex, "Directory could not be enumerated: {Directory}", current);
        continue;
      }

      foreach (var child in children)
      {
        pending.Push(child);
        yield return child;
      }
    }
  }

  private static bool IsEmptyDirectory(string directory)
  {
    try
    {
      return !Directory.EnumerateFileSystemEntries(directory).Any();
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
      Logger.Warn(ex, "Directory could not be checked: {Directory}", directory);
      return false;
    }
  }

  private static bool IsIgnoredDirectory(string directory)
  {
    var name = Path.GetFileName(directory);
    return IgnoredDirectoryNames.Any(ignored =>
      string.Equals(name, ignored, StringComparison.OrdinalIgnoreCase));
  }

  private static string NormalizePath(string path)
  {
    return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
  }
}
