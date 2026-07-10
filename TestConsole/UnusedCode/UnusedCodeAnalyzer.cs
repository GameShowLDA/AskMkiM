using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace TestConsole.UnusedCode;

/// <summary>
/// Entry point for the Roslyn unused-code analysis scenario.
/// </summary>
internal static class UnusedCodeAnalyzer
{
  private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

  /// <summary>
  /// Runs the full unused-code analysis for the current solution.
  /// </summary>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>A task representing the asynchronous operation.</returns>
  public static async Task RunAsync(CancellationToken cancellationToken = default)
  {
    var solutionPath = ResolveSolutionPath();
    var solutionDirectory = Path.GetDirectoryName(solutionPath) ?? Directory.GetCurrentDirectory();
    var reportsDirectory = Path.Combine(solutionDirectory, "TestConsole", "Reports", "UnusedCode");
    ConfigureLogging(reportsDirectory);

    Console.WriteLine($"Solution: {solutionPath}");
    Console.WriteLine($"Reports: {reportsDirectory}");

    var started = DateTimeOffset.UtcNow;
    Logger.Info("Unused-code analysis started.");

    try
    {
      RegisterMsBuild();
      var scanner = new SolutionScanner();
      var solution = await scanner.OpenSolutionAsync(solutionPath, cancellationToken).ConfigureAwait(false);
      var projects = scanner.GetProjects(solution);
      var totalDocuments = projects.Sum(project => project.Documents.Count(IsAnalyzableDocument));
      var progress = new ConsoleProgressReporter();
      var findings = new List<UnusedCodeFinding>();
      var semanticModelCache = new SemanticModelCache();
      var symbolAnalyzer = new SymbolAnalyzer(semanticModelCache);
      var referenceAnalyzer = new ReferenceAnalyzer();
      var processedDocuments = 0;

      foreach (var project in projects)
      {
        cancellationToken.ThrowIfCancellationRequested();
        var projectStarted = DateTimeOffset.UtcNow;
        Logger.Info("Project analysis started: {Project}", project.Name);

        foreach (var document in project.Documents.Where(IsAnalyzableDocument))
        {
          cancellationToken.ThrowIfCancellationRequested();
          var documentStarted = DateTimeOffset.UtcNow;
          Logger.Info("Document analysis started: {Project}/{Document}", project.Name, document.Name);

          try
          {
            var candidates = await symbolAnalyzer.GetCandidatesAsync(document, cancellationToken).ConfigureAwait(false);
            Logger.Info(
              "Document candidate symbols collected. Project: {Project}. Document: {Document}. Count: {Count}. Elapsed: {Elapsed}",
              project.Name,
              document.Name,
              candidates.Count,
              DateTimeOffset.UtcNow - documentStarted);

            foreach (var candidate in candidates)
            {
              var referenceInfo = await referenceAnalyzer.AnalyzeAsync(candidate.Symbol, solution, cancellationToken)
                .ConfigureAwait(false);
              Logger.Info(
                "Symbol references analyzed. Project: {Project}. Symbol: {Symbol}. References: {References}",
                candidate.ProjectName,
                candidate.Symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                referenceInfo.ReferenceCount);

              if (referenceInfo.ReferenceCount != 0 ||
                await symbolAnalyzer.ShouldSkipAsync(candidate, solution, cancellationToken).ConfigureAwait(false))
              {
                continue;
              }

              findings.Add(symbolAnalyzer.CreateFinding(candidate, referenceInfo));
            }
          }
          catch (Exception ex) when (ex is not OperationCanceledException)
          {
            Logger.Error(ex, "Document analysis failed: {Project}/{Document}", project.Name, document.Name);
          }
          finally
          {
            processedDocuments++;
            progress.Report(new UnusedCodeProgress(
              project.Name,
              document.Name,
              processedDocuments,
              totalDocuments,
              DateTimeOffset.UtcNow - started));
          }
        }

        semanticModelCache.Clear();
        Logger.Info(
          "Project analysis completed: {Project}. Elapsed: {Elapsed}",
          project.Name,
          DateTimeOffset.UtcNow - projectStarted);
      }

      var emptyFolderScanner = new EmptyFolderScanner();
      var emptyFolders = await emptyFolderScanner.ScanAsync(projects, cancellationToken).ConfigureAwait(false);
      var duplicateTypeScanner = new DuplicateTypeScanner(semanticModelCache);
      var duplicateTypes = await duplicateTypeScanner.ScanAsync(projects, cancellationToken).ConfigureAwait(false);
      var result = new UnusedCodeAnalysisResult(
        findings
          .OrderBy(finding => finding.Project)
          .ThenBy(finding => finding.Namespace)
          .ThenBy(finding => finding.Kind)
          .ThenBy(finding => finding.FullName)
          .ToArray(),
        emptyFolders,
        duplicateTypes,
        DateTimeOffset.UtcNow - started);

      var reportBuilder = new ReportBuilder();
      var paths = await reportBuilder.BuildAllAsync(result, reportsDirectory, cancellationToken).ConfigureAwait(false);

      PrintFindings(result);
      Console.WriteLine();
      Console.WriteLine("Reports:");
      foreach (var path in paths)
      {
        Console.WriteLine(path);
      }

      Logger.Info(
        "Unused-code analysis completed. Findings: {Findings}. Empty folders: {EmptyFolders}. Duplicate types: {DuplicateTypes}. Elapsed: {Elapsed}",
        result.Findings.Count,
        result.EmptyFolders.Count,
        result.DuplicateTypes.Count,
        result.Elapsed);
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
      Logger.Error(ex, "Unused-code analysis failed.");
      Console.WriteLine(ex);
    }
    finally
    {
      LogManager.Flush();
    }
  }

  private static bool IsAnalyzableDocument(Document document)
  {
    return document.SupportsSyntaxTree && document.SourceCodeKind == SourceCodeKind.Regular;
  }

  private static void RegisterMsBuild()
  {
    if (MSBuildLocator.IsRegistered)
    {
      return;
    }

    var instance = MSBuildLocator.RegisterDefaults();
    Logger.Info("MSBuild registered: {Name} {Version} {Path}", instance.Name, instance.Version, instance.MSBuildPath);
  }

  private static string ResolveSolutionPath()
  {
    var baseDirectory = AppContext.BaseDirectory;
    var candidates = new[]
    {
      Path.Combine(baseDirectory, "..", "..", "AskMkiM.sln"),
      Path.Combine(Directory.GetCurrentDirectory(), "AskMkiM.sln"),
      Path.Combine(Directory.GetCurrentDirectory(), "..", "AskMkiM.sln"),
      @"D:\GitRep\AskMkiM\AskMkiM.sln"
    };

    foreach (var candidate in candidates)
    {
      var fullPath = Path.GetFullPath(candidate);
      if (File.Exists(fullPath))
      {
        return fullPath;
      }
    }

    throw new FileNotFoundException("Solution file AskMkiM.sln was not found.");
  }

  private static void ConfigureLogging(string reportsDirectory)
  {
    Directory.CreateDirectory(reportsDirectory);

    var config = new LoggingConfiguration();
    var fileTarget = new FileTarget("unusedCodeFile")
    {
      FileName = Path.Combine(reportsDirectory, "UnusedCode.log"),
      Layout = "${longdate}|${level:uppercase=true}|${logger}|${message}|${exception:format=tostring}"
    };

    config.AddTarget(fileTarget);
    config.AddRule(LogLevel.Debug, LogLevel.Fatal, fileTarget);
    LogManager.Configuration = config;
  }

  private static void PrintFindings(UnusedCodeAnalysisResult result)
  {
    foreach (var finding in result.Findings)
    {
      Console.WriteLine();
      Console.WriteLine(finding.Kind);
      Console.WriteLine(finding.FullName);
      Console.WriteLine($"Project: {finding.Project}");
      Console.WriteLine($"File: {finding.File}:{finding.Line}");
      Console.WriteLine($"References: {finding.References}");
      Console.WriteLine($"Reason: {finding.Reason}");
    }

    foreach (var folder in result.EmptyFolders)
    {
      Console.WriteLine();
      Console.WriteLine("EmptyFolder");
      Console.WriteLine(folder.Path);
      Console.WriteLine($"Project: {folder.Project}");
      Console.WriteLine($"Reason: {folder.Reason}");
    }

    foreach (var duplicate in result.DuplicateTypes)
    {
      Console.WriteLine();
      Console.WriteLine("DuplicateType");
      Console.WriteLine(duplicate.FullName);
      Console.WriteLine($"Kind: {duplicate.Kind}");
      Console.WriteLine($"Namespace: {duplicate.Namespace}");
      Console.WriteLine($"Occurrences: {duplicate.Occurrences.Count}");
      foreach (var occurrence in duplicate.Occurrences)
      {
        Console.WriteLine($"- {occurrence.Project}: {occurrence.File}:{occurrence.Line}");
      }

      Console.WriteLine($"Reason: {duplicate.Reason}");
    }

    Console.WriteLine();
    Console.WriteLine("==================================");
    Console.WriteLine($"Unused classes: {GetCount(result, UnusedSymbolKind.Class)}");
    Console.WriteLine($"Unused methods: {GetCount(result, UnusedSymbolKind.Method)}");
    Console.WriteLine($"Unused properties: {GetCount(result, UnusedSymbolKind.Property)}");
    Console.WriteLine($"Unused fields: {GetCount(result, UnusedSymbolKind.Field)}");
    Console.WriteLine($"Unused interfaces: {GetCount(result, UnusedSymbolKind.Interface)}");
    Console.WriteLine($"Unused enums: {GetCount(result, UnusedSymbolKind.Enum)}");
    Console.WriteLine($"Unused events: {GetCount(result, UnusedSymbolKind.Event)}");
    Console.WriteLine($"Empty folders: {result.EmptyFolders.Count}");
    Console.WriteLine($"Duplicate types: {result.DuplicateTypes.Count}");
    Console.WriteLine($"Total: {result.Findings.Count + result.EmptyFolders.Count + result.DuplicateTypes.Count}");
    Console.WriteLine("==================================");
  }

  private static int GetCount(UnusedCodeAnalysisResult result, UnusedSymbolKind kind)
  {
    return result.Counts.TryGetValue(kind, out var count) ? count : 0;
  }
}
