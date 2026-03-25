using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace TestConsole
{
  internal static class NamespaceFolderScanner
  {
    private static readonly Regex SolutionProjectRegex = new(
      "^Project\\(\"\\{[^\\}]+\\}\"\\)\\s=\\s\"(?<name>[^\"]+)\",\\s\"(?<path>[^\"]+\\.csproj)\",\\s\"\\{[^\\}]+\\}\"$",
      RegexOptions.Compiled);

    public static void Run()
    {
      Console.WriteLine();
      Console.WriteLine("=== \u041f\u0440\u043e\u0432\u0435\u0440\u043a\u0430 namespace \u043f\u043e \u043f\u0430\u043f\u043a\u0430\u043c ===");

      string? solutionPath = FindSolutionPath();
      if (solutionPath is null)
      {
        WriteError("\u041d\u0435 \u0443\u0434\u0430\u043b\u043e\u0441\u044c \u043d\u0430\u0439\u0442\u0438 \u0444\u0430\u0439\u043b \u0440\u0435\u0448\u0435\u043d\u0438\u044f AskMkiM.sln.");
        return;
      }

      List<ProjectDescriptor> projects = LoadProjects(solutionPath);
      if (projects.Count == 0)
      {
        WriteError("\u0412 \u0440\u0435\u0448\u0435\u043d\u0438\u0438 \u043d\u0435 \u043d\u0430\u0439\u0434\u0435\u043d\u043e \u043d\u0438 \u043e\u0434\u043d\u043e\u0433\u043e \u043f\u0440\u043e\u0435\u043a\u0442\u0430 .csproj.");
        return;
      }

      var results = new List<ProjectScanResult>(projects.Count);

      foreach (ProjectDescriptor project in projects)
      {
        Console.WriteLine($"\u0421\u043a\u0430\u043d\u0438\u0440\u043e\u0432\u0430\u043d\u0438\u0435 \u043f\u0440\u043e\u0435\u043a\u0442\u0430: {project.Name}");
        results.Add(ScanProject(project));
      }

      PrintSummary(results);
      PromptNamespaceFix(results);
    }

    private static ProjectScanResult ScanProject(ProjectDescriptor project)
    {
      string projectDirectory = Path.GetDirectoryName(project.Path)
        ?? throw new InvalidOperationException($"Project directory was not resolved for {project.Path}.");

      List<string> sourceFiles = EnumerateSourceFiles(projectDirectory).ToList();
      var scannedFiles = new List<FileScanResult>(sourceFiles.Count);
      var rootNamespaceCandidates = new Dictionary<string, int>(StringComparer.Ordinal);

      foreach (string sourceFile in sourceFiles)
      {
        FileScanResult fileResult = ScanFile(projectDirectory, sourceFile);
        scannedFiles.Add(fileResult);

        foreach (string candidate in fileResult.RootNamespaceCandidates)
        {
          rootNamespaceCandidates[candidate] = rootNamespaceCandidates.TryGetValue(candidate, out int count)
            ? count + 1
            : 1;
        }
      }

      string rootNamespace = ResolveRootNamespace(project.Path, projectDirectory, rootNamespaceCandidates);
      var issues = new List<NamespaceIssue>();

      foreach (FileScanResult file in scannedFiles)
      {
        string expectedNamespace = ComposeNamespace(rootNamespace, file.RelativeDirectoryNamespace);

        foreach (TypeScanInfo type in file.Types.Where(IsClassLike))
        {
          if (string.Equals(type.Namespace, expectedNamespace, StringComparison.Ordinal))
            continue;

          issues.Add(new NamespaceIssue(
            file.AbsolutePath,
            file.RelativePath,
            type.DisplayName,
            type.LineNumber,
            type.Namespace,
            expectedNamespace));
        }
      }

      return new ProjectScanResult(project.Name, rootNamespace, sourceFiles.Count, issues);
    }

    private static FileScanResult ScanFile(string projectDirectory, string filePath)
    {
      string relativePath = Path.GetRelativePath(projectDirectory, filePath);
      string relativeDirectoryPath = Path.GetDirectoryName(relativePath) ?? string.Empty;
      string relativeDirectoryNamespace = NormalizePathAsNamespace(relativeDirectoryPath);

      SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(filePath));
      CompilationUnitSyntax root = syntaxTree.GetCompilationUnitRoot();

      List<TypeScanInfo> types = root
        .DescendantNodes()
        .OfType<BaseTypeDeclarationSyntax>()
        .Select(type => CreateTypeScanInfo(syntaxTree, type))
        .ToList();

      var rootNamespaceCandidates = new HashSet<string>(StringComparer.Ordinal);
      foreach (TypeScanInfo type in types)
      {
        if (TryGetRootNamespaceCandidate(type.Namespace, relativeDirectoryNamespace, out string? candidate))
          rootNamespaceCandidates.Add(candidate);
      }

      return new FileScanResult(filePath, relativePath, relativeDirectoryNamespace, types, rootNamespaceCandidates);
    }

    private static TypeScanInfo CreateTypeScanInfo(SyntaxTree syntaxTree, BaseTypeDeclarationSyntax declaration)
    {
      FileLinePositionSpan lineSpan = syntaxTree.GetLineSpan(declaration.Identifier.Span);

      return new TypeScanInfo(
        declaration,
        BuildTypeDisplayName(declaration),
        GetNamespace(declaration),
        lineSpan.StartLinePosition.Line + 1);
    }

    private static string BuildTypeDisplayName(BaseTypeDeclarationSyntax declaration)
    {
      IEnumerable<string> containingTypes = declaration
        .Ancestors()
        .OfType<BaseTypeDeclarationSyntax>()
        .Reverse()
        .Select(static ancestor => ancestor.Identifier.Text);

      return string.Join(".", containingTypes.Append(declaration.Identifier.Text));
    }

    private static string GetNamespace(BaseTypeDeclarationSyntax declaration)
    {
      IEnumerable<string> namespaces = declaration
        .Ancestors()
        .OfType<BaseNamespaceDeclarationSyntax>()
        .Reverse()
        .Select(static ancestor => ancestor.Name.ToString());

      return string.Join(".", namespaces);
    }

    private static bool TryGetRootNamespaceCandidate(
      string actualNamespace,
      string relativeDirectoryNamespace,
      out string candidate)
    {
      candidate = string.Empty;

      if (string.IsNullOrWhiteSpace(actualNamespace))
        return false;

      if (string.IsNullOrWhiteSpace(relativeDirectoryNamespace))
      {
        candidate = actualNamespace;
        return true;
      }

      string suffix = "." + relativeDirectoryNamespace;
      if (!actualNamespace.EndsWith(suffix, StringComparison.Ordinal))
        return false;

      candidate = actualNamespace[..^suffix.Length];
      return !string.IsNullOrWhiteSpace(candidate);
    }

    private static string ResolveRootNamespace(
      string projectPath,
      string projectDirectory,
      IReadOnlyDictionary<string, int> rootNamespaceCandidates)
    {
      if (rootNamespaceCandidates.Count > 0)
      {
        return rootNamespaceCandidates
          .OrderByDescending(static pair => pair.Value)
          .ThenByDescending(static pair => pair.Key.Length)
          .ThenBy(static pair => pair.Key, StringComparer.Ordinal)
          .First()
          .Key;
      }

      string? configuredRootNamespace = ReadProjectProperty(projectPath, "RootNamespace");
      if (!string.IsNullOrWhiteSpace(configuredRootNamespace))
        return configuredRootNamespace;

      string? assemblyName = ReadProjectProperty(projectPath, "AssemblyName");
      if (!string.IsNullOrWhiteSpace(assemblyName))
        return assemblyName;

      return Path.GetFileName(projectDirectory);
    }

    private static string? ReadProjectProperty(string projectPath, string propertyName)
    {
      XDocument document = XDocument.Load(projectPath);

      return document
        .Descendants()
        .FirstOrDefault(element =>
          string.Equals(element.Name.LocalName, propertyName, StringComparison.OrdinalIgnoreCase)
          && !string.IsNullOrWhiteSpace(element.Value))
        ?.Value
        .Trim();
    }

    private static List<ProjectDescriptor> LoadProjects(string solutionPath)
    {
      string solutionDirectory = Path.GetDirectoryName(solutionPath)
        ?? throw new InvalidOperationException($"Solution directory was not resolved for {solutionPath}.");

      var projects = new List<ProjectDescriptor>();

      foreach (string line in File.ReadLines(solutionPath))
      {
        Match match = SolutionProjectRegex.Match(line);
        if (!match.Success)
          continue;

        string relativeProjectPath = match.Groups["path"].Value.Replace('\\', Path.DirectorySeparatorChar);
        string absoluteProjectPath = Path.GetFullPath(Path.Combine(solutionDirectory, relativeProjectPath));

        if (!File.Exists(absoluteProjectPath))
          continue;

        projects.Add(new ProjectDescriptor(match.Groups["name"].Value, absoluteProjectPath));
      }

      return projects;
    }

    private static IEnumerable<string> EnumerateSourceFiles(string projectDirectory)
    {
      foreach (string filePath in Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories))
      {
        string relativePath = Path.GetRelativePath(projectDirectory, filePath);
        string[] segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (segments.Any(static segment =>
              segment.Equals("bin", StringComparison.OrdinalIgnoreCase)
              || segment.Equals("obj", StringComparison.OrdinalIgnoreCase)))
        {
          continue;
        }

        yield return filePath;
      }
    }

    private static string? FindSolutionPath()
    {
      var current = new DirectoryInfo(AppContext.BaseDirectory);

      while (current is not null)
      {
        string candidate = Path.Combine(current.FullName, "AskMkiM.sln");
        if (File.Exists(candidate))
          return candidate;

        current = current.Parent;
      }

      return null;
    }

    private static bool IsClassLike(TypeScanInfo type)
    {
      if (type.Declaration is ClassDeclarationSyntax)
        return true;

      if (type.Declaration is RecordDeclarationSyntax record)
        return !record.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword);

      return false;
    }

    private static string NormalizePathAsNamespace(string path)
    {
      if (string.IsNullOrWhiteSpace(path))
        return string.Empty;

      string[] segments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
      return string.Join(".", segments.Where(static segment => !string.IsNullOrWhiteSpace(segment)));
    }

    private static string ComposeNamespace(string rootNamespace, string relativeDirectoryNamespace)
    {
      if (string.IsNullOrWhiteSpace(rootNamespace))
        return relativeDirectoryNamespace;

      if (string.IsNullOrWhiteSpace(relativeDirectoryNamespace))
        return rootNamespace;

      return rootNamespace + "." + relativeDirectoryNamespace;
    }

    private static void PrintSummary(IReadOnlyCollection<ProjectScanResult> results)
    {
      int totalFiles = results.Sum(static result => result.FileCount);
      int totalIssues = results.Sum(static result => result.Issues.Count);

      Console.WriteLine();
      Console.WriteLine($"\u041f\u0440\u043e\u0432\u0435\u0440\u0435\u043d\u043e \u043f\u0440\u043e\u0435\u043a\u0442\u043e\u0432: {results.Count}");
      Console.WriteLine($"\u041f\u0440\u043e\u0432\u0435\u0440\u0435\u043d\u043e \u0444\u0430\u0439\u043b\u043e\u0432: {totalFiles}");
      Console.WriteLine($"\u041d\u0430\u0439\u0434\u0435\u043d\u043e \u043d\u0435\u0441\u043e\u0432\u043f\u0430\u0434\u0435\u043d\u0438\u0439: {totalIssues}");

      if (totalIssues == 0)
      {
        WriteSuccess("\u041d\u0435\u0441\u043e\u0432\u043f\u0430\u0434\u0435\u043d\u0438\u0439 namespace \u0441 \u0440\u0430\u0441\u043f\u043e\u043b\u043e\u0436\u0435\u043d\u0438\u0435\u043c \u0444\u0430\u0439\u043b\u043e\u0432 \u043d\u0435 \u043d\u0430\u0439\u0434\u0435\u043d\u043e.");
        return;
      }

      foreach (ProjectScanResult project in results.Where(static result => result.Issues.Count > 0))
      {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"[{project.Name}] \u0431\u0430\u0437\u043e\u0432\u044b\u0439 namespace: {FormatNamespace(project.RootNamespace)}");
        Console.ResetColor();

        foreach (NamespaceIssue issue in project.Issues
                   .OrderBy(static issue => issue.FilePath, StringComparer.OrdinalIgnoreCase)
                   .ThenBy(static issue => issue.LineNumber)
                   .ThenBy(static issue => issue.TypeName, StringComparer.Ordinal))
        {
          Console.WriteLine($"{issue.FilePath}:{issue.LineNumber}  {issue.TypeName}");
          WriteColoredNamespaceLine("  actual:   ", FormatNamespace(issue.ActualNamespace), ConsoleColor.Red);
          WriteColoredNamespaceLine("  expected: ", FormatNamespace(issue.ExpectedNamespace), ConsoleColor.Green);
        }
      }
    }

    private static void PromptNamespaceFix(IReadOnlyCollection<ProjectScanResult> results)
    {
      List<NamespaceIssue> issues = results
        .SelectMany(static result => result.Issues)
        .ToList();

      if (issues.Count == 0)
        return;

      Console.WriteLine();
      Console.ForegroundColor = ConsoleColor.Yellow;
      Console.WriteLine("\u0412\u043d\u0438\u043c\u0430\u043d\u0438\u0435: \u0431\u0443\u0434\u0443\u0442 \u0438\u0437\u043c\u0435\u043d\u0435\u043d\u044b \u0442\u043e\u043b\u044c\u043a\u043e \u043e\u0431\u044a\u044f\u0432\u043b\u0435\u043d\u0438\u044f namespace \u0432 \u0441\u0430\u043c\u0438\u0445 \u0444\u0430\u0439\u043b\u0430\u0445 \u043a\u043b\u0430\u0441\u0441\u043e\u0432.");
      Console.WriteLine("\u0421\u0441\u044b\u043b\u043a\u0438, using \u0438 \u043e\u0431\u0440\u0430\u0449\u0435\u043d\u0438\u044f \u0438\u0437 \u0434\u0440\u0443\u0433\u0438\u0445 \u043f\u0440\u043e\u0435\u043a\u0442\u043e\u0432 \u043c\u043e\u0433\u0443\u0442 \u0432\u0440\u0435\u043c\u0435\u043d\u043d\u043e \u043f\u043e\u0442\u0435\u0440\u044f\u0442\u044c\u0441\u044f \u0438\u043b\u0438 \u043f\u043e\u0434\u0441\u0432\u0435\u0442\u0438\u0442\u044c\u0441\u044f \u043e\u0448\u0438\u0431\u043a\u0430\u043c\u0438.");
      Console.WriteLine("\u042d\u0442\u043e \u043d\u043e\u0440\u043c\u0430\u043b\u044c\u043d\u043e: \u0430\u0432\u0442\u043e\u0437\u0430\u043c\u0435\u043d\u0430 \u0434\u0435\u043b\u0430\u0435\u0442\u0441\u044f \u0442\u043e\u043b\u044c\u043a\u043e \u0432 \u043a\u043b\u0430\u0441\u0441\u0435.");
      Console.ResetColor();

      Console.Write("\u0417\u0430\u043c\u0435\u043d\u0438\u0442\u044c namespace \u0432 \u043d\u0430\u0439\u0434\u0435\u043d\u043d\u044b\u0445 \u0444\u0430\u0439\u043b\u0430\u0445? [y/N]: ");
      string? answer = Console.ReadLine();
      if (!IsYes(answer))
      {
        Console.WriteLine("\u0417\u0430\u043c\u0435\u043d\u0430 namespace \u043e\u0442\u043c\u0435\u043d\u0435\u043d\u0430.");
        return;
      }

      ApplyNamespaceFixes(issues);
    }

    private static bool IsYes(string? answer)
    {
      if (string.IsNullOrWhiteSpace(answer))
        return false;

      return answer.Trim().ToLowerInvariant() switch
      {
        "y" => true,
        "yes" => true,
        "\u0434" => true,
        "\u0434\u0430" => true,
        _ => false
      };
    }

    private static void ApplyNamespaceFixes(IReadOnlyCollection<NamespaceIssue> issues)
    {
      List<NamespaceFixPlan> plans = BuildFixPlans(issues);
      if (plans.Count == 0)
      {
        WriteError("\u041d\u0435 \u043d\u0430\u0439\u0434\u0435\u043d\u043e \u0444\u0430\u0439\u043b\u043e\u0432 \u0434\u043b\u044f \u0430\u0432\u0442\u043e\u0437\u0430\u043c\u0435\u043d\u044b namespace.");
        return;
      }

      var results = new List<NamespaceFixResult>(plans.Count);
      foreach (NamespaceFixPlan plan in plans)
      {
        results.Add(ApplyNamespaceFix(plan));
      }

      Console.WriteLine();
      Console.WriteLine($"\u041f\u0440\u0430\u0432\u043e\u043a \u0432\u043d\u0435\u0441\u0435\u043d\u043e: {results.Count(static result => result.Status == NamespaceFixStatus.Changed)}");
      Console.WriteLine($"\u041f\u0440\u043e\u043f\u0443\u0449\u0435\u043d\u043e: {results.Count(static result => result.Status == NamespaceFixStatus.Skipped)}");
      Console.WriteLine($"\u041e\u0448\u0438\u0431\u043e\u043a: {results.Count(static result => result.Status == NamespaceFixStatus.Failed)}");

      foreach (NamespaceFixResult result in results.Where(static result => result.Status != NamespaceFixStatus.Changed))
      {
        Console.WriteLine();
        WriteColoredNamespaceLine(result.RelativePath + "  ", result.Message, result.Status == NamespaceFixStatus.Failed ? ConsoleColor.Red : ConsoleColor.Yellow);
      }
    }

    private static List<NamespaceFixPlan> BuildFixPlans(IReadOnlyCollection<NamespaceIssue> issues)
    {
      return issues
        .GroupBy(static issue => issue.AbsoluteFilePath, StringComparer.OrdinalIgnoreCase)
        .Select(group =>
        {
          List<string> expectedNamespaces = group
            .Select(static issue => issue.ExpectedNamespace)
            .Distinct(StringComparer.Ordinal)
            .ToList();

          return new NamespaceFixPlan(
            group.Key,
            group.First().FilePath,
            expectedNamespaces.Count == 1 ? expectedNamespaces[0] : string.Empty,
            group
              .Select(static issue => issue.TypeName)
              .Distinct(StringComparer.Ordinal)
              .OrderBy(static typeName => typeName, StringComparer.Ordinal)
              .ToList());
        })
        .Where(static plan => !string.IsNullOrWhiteSpace(plan.ExpectedNamespace))
        .ToList();
    }

    private static NamespaceFixResult ApplyNamespaceFix(NamespaceFixPlan plan)
    {
      try
      {
        string source = File.ReadAllText(plan.AbsolutePath);
        if (!TryRewriteNamespace(source, plan.ExpectedNamespace, out string? updatedSource, out string message))
        {
          return new NamespaceFixResult(plan.RelativePath, NamespaceFixStatus.Skipped, message);
        }

        if (string.Equals(source, updatedSource, StringComparison.Ordinal))
        {
          return new NamespaceFixResult(plan.RelativePath, NamespaceFixStatus.Skipped, "\u0412 \u0444\u0430\u0439\u043b\u0435 \u0443\u0436\u0435 \u0441\u0442\u043e\u0438\u0442 \u043d\u0443\u0436\u043d\u044b\u0439 namespace.");
        }

        File.WriteAllText(plan.AbsolutePath, updatedSource);
        WriteColoredNamespaceLine(plan.RelativePath + "  ", plan.ExpectedNamespace, ConsoleColor.Green);
        return new NamespaceFixResult(plan.RelativePath, NamespaceFixStatus.Changed, "\u0417\u0430\u043c\u0435\u043d\u0430 namespace \u0432\u044b\u043f\u043e\u043b\u043d\u0435\u043d\u0430.");
      }
      catch (Exception ex)
      {
        return new NamespaceFixResult(plan.RelativePath, NamespaceFixStatus.Failed, ex.Message);
      }
    }

    private static bool TryRewriteNamespace(
      string source,
      string expectedNamespace,
      out string updatedSource,
      out string message)
    {
      updatedSource = source;
      message = string.Empty;

      SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);
      CompilationUnitSyntax root = syntaxTree.GetCompilationUnitRoot();
      List<BaseNamespaceDeclarationSyntax> namespaces = root.Members.OfType<BaseNamespaceDeclarationSyntax>().ToList();

      if (namespaces.Count == 1 && root.Members.Count == 1)
      {
        BaseNamespaceDeclarationSyntax namespaceDeclaration = namespaces[0];
        TextSpan nameSpan = namespaceDeclaration.Name.Span;
        updatedSource = source[..nameSpan.Start] + expectedNamespace + source[nameSpan.End..];
        message = "\u0417\u0430\u043c\u0435\u043d\u0430 namespace \u0432\u044b\u043f\u043e\u043b\u043d\u0435\u043d\u0430.";
        return true;
      }

      if (namespaces.Count == 0)
      {
        MemberDeclarationSyntax? firstMember = root.Members.FirstOrDefault();
        if (firstMember is null)
        {
          message = "\u0412 \u0444\u0430\u0439\u043b\u0435 \u043d\u0435\u0442 \u0442\u0438\u043f\u043e\u0432 \u0434\u043b\u044f \u0437\u0430\u043c\u0435\u043d\u044b.";
          return false;
        }

        string newLine = DetectNewLine(source);
        int insertPosition = firstMember.FullSpan.Start;
        updatedSource = source.Insert(insertPosition, $"namespace {expectedNamespace};{newLine}{newLine}");
        message = "\u0417\u0430\u043c\u0435\u043d\u0430 namespace \u0432\u044b\u043f\u043e\u043b\u043d\u0435\u043d\u0430.";
        return true;
      }

      message = "\u041f\u0440\u043e\u043f\u0443\u0441\u043a: \u0432 \u0444\u0430\u0439\u043b\u0435 \u043d\u0435\u0441\u043a\u043e\u043b\u044c\u043a\u043e namespace \u0438\u043b\u0438 \u0441\u043b\u043e\u0436\u043d\u0430\u044f \u0441\u0442\u0440\u0443\u043a\u0442\u0443\u0440\u0430.";
      return false;
    }

    private static string DetectNewLine(string source)
    {
      return source.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
    }

    private static string FormatNamespace(string value)
    {
      return string.IsNullOrWhiteSpace(value) ? "<global namespace>" : value;
    }

    private static void WriteError(string message)
    {
      Console.ForegroundColor = ConsoleColor.Red;
      Console.WriteLine(message);
      Console.ResetColor();
    }

    private static void WriteSuccess(string message)
    {
      Console.ForegroundColor = ConsoleColor.Green;
      Console.WriteLine(message);
      Console.ResetColor();
    }

    private static void WriteColoredNamespaceLine(string prefix, string value, ConsoleColor color)
    {
      Console.Write(prefix);
      Console.ForegroundColor = color;
      Console.WriteLine(value);
      Console.ResetColor();
    }

    private sealed record ProjectDescriptor(string Name, string Path);

    private sealed record FileScanResult(
      string AbsolutePath,
      string RelativePath,
      string RelativeDirectoryNamespace,
      IReadOnlyList<TypeScanInfo> Types,
      IReadOnlyCollection<string> RootNamespaceCandidates);

    private sealed record TypeScanInfo(
      BaseTypeDeclarationSyntax Declaration,
      string DisplayName,
      string Namespace,
      int LineNumber);

    private sealed record NamespaceIssue(
      string AbsoluteFilePath,
      string FilePath,
      string TypeName,
      int LineNumber,
      string ActualNamespace,
      string ExpectedNamespace);

    private sealed record ProjectScanResult(
      string Name,
      string RootNamespace,
      int FileCount,
      IReadOnlyList<NamespaceIssue> Issues);

    private sealed record NamespaceFixPlan(
      string AbsolutePath,
      string RelativePath,
      string ExpectedNamespace,
      IReadOnlyList<string> TypeNames);

    private sealed record NamespaceFixResult(
      string RelativePath,
      NamespaceFixStatus Status,
      string Message);

    private enum NamespaceFixStatus
    {
      Changed,
      Skipped,
      Failed
    }
  }
}
