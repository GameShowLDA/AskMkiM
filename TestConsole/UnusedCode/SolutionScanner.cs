using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using NLog;

namespace TestConsole.UnusedCode;

/// <summary>
/// Opens and enumerates a Roslyn solution using MSBuild.
/// </summary>
internal sealed class SolutionScanner
{
  private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

  /// <summary>
  /// Opens the specified solution with <see cref="MSBuildWorkspace"/>.
  /// </summary>
  /// <param name="solutionPath">The solution file path.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The loaded Roslyn solution.</returns>
  public async Task<Solution> OpenSolutionAsync(string solutionPath, CancellationToken cancellationToken)
  {
    Logger.Info("Opening solution: {SolutionPath}", solutionPath);
    var started = DateTimeOffset.UtcNow;

    var properties = new Dictionary<string, string>
    {
      ["Configuration"] = "Debug",
      ["Platform"] = "Any CPU"
    };

    var workspace = MSBuildWorkspace.Create(properties);
    workspace.WorkspaceFailed += (_, args) => Logger.Warn("{Diagnostic}", args.Diagnostic.Message);

    var solution = await workspace.OpenSolutionAsync(solutionPath, cancellationToken: cancellationToken)
      .ConfigureAwait(false);

    var projectCount = solution.Projects.Count();
    var documentCount = solution.Projects.Sum(project => project.Documents.Count());
    Logger.Info(
      "Solution loaded. Projects: {ProjectCount}. Documents: {DocumentCount}. Elapsed: {Elapsed}",
      projectCount,
      documentCount,
      DateTimeOffset.UtcNow - started);

    return solution;
  }

  /// <summary>
  /// Gets projects in dependency order so project references are available before consumers are analyzed.
  /// </summary>
  /// <param name="solution">The loaded solution.</param>
  /// <returns>Projects from the current solution, including project references.</returns>
  public IReadOnlyList<Project> GetProjects(Solution solution)
  {
    var dependencyGraph = solution.GetProjectDependencyGraph();
    var projectIds = dependencyGraph.GetTopologicallySortedProjects(cancellationToken: CancellationToken.None);
    var projects = projectIds
      .Select(solution.GetProject)
      .Where(project => project is not null)
      .Cast<Project>()
      .ToArray();

    Logger.Info("Projects prepared for analysis: {ProjectCount}", projects.Length);
    foreach (var project in projects)
    {
      Logger.Info("Project loaded: {ProjectName}. Documents: {DocumentCount}", project.Name, project.Documents.Count());
    }

    return projects;
  }
}
