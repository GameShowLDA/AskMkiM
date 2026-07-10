using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NLog;

namespace TestConsole.UnusedCode;

/// <summary>
/// Finds duplicated type declarations across loaded solution projects.
/// </summary>
internal sealed class DuplicateTypeScanner
{
  private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
  private readonly SemanticModelCache _semanticModelCache;

  /// <summary>
  /// Initializes a new instance of the <see cref="DuplicateTypeScanner"/> class.
  /// </summary>
  /// <param name="semanticModelCache">The semantic-model cache used while reading declarations.</param>
  public DuplicateTypeScanner(SemanticModelCache semanticModelCache)
  {
    _semanticModelCache = semanticModelCache;
  }

  /// <summary>
  /// Finds duplicated type declarations across the specified projects.
  /// </summary>
  /// <param name="projects">The projects loaded from the solution.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>Duplicated type findings.</returns>
  public async Task<IReadOnlyList<DuplicateTypeFinding>> ScanAsync(
    IReadOnlyList<Project> projects,
    CancellationToken cancellationToken)
  {
    var started = DateTimeOffset.UtcNow;
    var declarations = new List<TypeDeclarationInfo>();

    foreach (var project in projects)
    {
      cancellationToken.ThrowIfCancellationRequested();
      Logger.Info("Duplicate type scan started: {Project}", project.Name);

      foreach (var document in project.Documents.Where(IsAnalyzableDocument))
      {
        cancellationToken.ThrowIfCancellationRequested();
        await AddDocumentDeclarationsAsync(project.Name, document, declarations, cancellationToken)
          .ConfigureAwait(false);
      }

      _semanticModelCache.Clear();
      Logger.Info("Duplicate type scan completed: {Project}", project.Name);
    }

    var findings = declarations
      .GroupBy(declaration => $"{declaration.Kind}|{declaration.FullName}", StringComparer.Ordinal)
      .Where(group => IsSuspiciousDuplicate(group))
      .Select(group => CreateFinding(group))
      .OrderBy(finding => finding.FullName)
      .ToArray();

    Logger.Info(
      "Duplicate type scan completed. Count: {Count}. Elapsed: {Elapsed}",
      findings.Length,
      DateTimeOffset.UtcNow - started);

    return findings;
  }

  private async Task AddDocumentDeclarationsAsync(
    string projectName,
    Document document,
    ICollection<TypeDeclarationInfo> declarations,
    CancellationToken cancellationToken)
  {
    var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
    if (root is null)
    {
      return;
    }

    var semanticModel = await _semanticModelCache.GetAsync(document, cancellationToken).ConfigureAwait(false);
    if (semanticModel is null)
    {
      return;
    }

    foreach (var node in root.DescendantNodes().Where(IsTypeDeclaration))
    {
      cancellationToken.ThrowIfCancellationRequested();
      var symbol = GetDeclaredTypeSymbol(node, semanticModel, cancellationToken);
      if (symbol is null || symbol.IsImplicitlyDeclared)
      {
        continue;
      }

      var location = symbol.Locations.FirstOrDefault(location => location.IsInSource);
      var file = location?.SourceTree?.FilePath ?? document.FilePath ?? string.Empty;
      var line = location?.GetLineSpan().StartLinePosition.Line + 1 ?? 0;
      var kind = GetKind(node);
      if (kind is null)
      {
        continue;
      }

      declarations.Add(new TypeDeclarationInfo(
        kind.Value,
        symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
        GetNamespace(symbol),
        projectName,
        file,
        line,
        IsPartial(node)));
    }
  }

  private static bool IsSuspiciousDuplicate(IGrouping<string, TypeDeclarationInfo> group)
  {
    var declarations = group
      .DistinctBy(declaration => $"{declaration.Project}|{NormalizePath(declaration.File)}|{declaration.Line}")
      .ToArray();

    if (declarations.Length < 2)
    {
      return false;
    }

    if (declarations.Select(declaration => declaration.Project).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
    {
      return true;
    }

    return declarations.Any(declaration => !declaration.IsPartial);
  }

  private static DuplicateTypeFinding CreateFinding(IGrouping<string, TypeDeclarationInfo> group)
  {
    var first = group.First();
    var occurrences = group
      .DistinctBy(declaration => $"{declaration.Project}|{NormalizePath(declaration.File)}|{declaration.Line}")
      .OrderBy(declaration => declaration.Project)
      .ThenBy(declaration => declaration.File)
      .ThenBy(declaration => declaration.Line)
      .Select(declaration => new DuplicateTypeOccurrence(
        declaration.Project,
        declaration.File,
        declaration.Line))
      .ToArray();

    return new DuplicateTypeFinding(
      first.Kind,
      first.FullName,
      first.Namespace,
      occurrences,
      "Type with the same fully qualified name is declared in multiple solution locations.");
  }

  private static ISymbol? GetDeclaredTypeSymbol(
    SyntaxNode node,
    SemanticModel semanticModel,
    CancellationToken cancellationToken)
  {
    return node switch
    {
      BaseTypeDeclarationSyntax typeDeclaration => semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken),
      _ => null
    };
  }

  private static bool IsTypeDeclaration(SyntaxNode node)
  {
    return node is TypeDeclarationSyntax or EnumDeclarationSyntax;
  }

  private static UnusedSymbolKind? GetKind(SyntaxNode node)
  {
    return node switch
    {
      RecordDeclarationSyntax recordDeclaration when recordDeclaration.Kind() == SyntaxKind.RecordStructDeclaration => UnusedSymbolKind.Struct,
      RecordDeclarationSyntax => UnusedSymbolKind.Record,
      StructDeclarationSyntax => UnusedSymbolKind.Struct,
      InterfaceDeclarationSyntax => UnusedSymbolKind.Interface,
      EnumDeclarationSyntax => UnusedSymbolKind.Enum,
      ClassDeclarationSyntax => UnusedSymbolKind.Class,
      _ => null
    };
  }

  private static bool IsPartial(SyntaxNode node)
  {
    return node is TypeDeclarationSyntax typeDeclaration &&
      typeDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword);
  }

  private static bool IsAnalyzableDocument(Document document)
  {
    return document.SupportsSyntaxTree &&
      document.SourceCodeKind == SourceCodeKind.Regular &&
      document.FilePath is not null &&
      !document.FilePath.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) &&
      !document.FilePath.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) &&
      !document.FilePath.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase) &&
      !document.FilePath.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase);
  }

  private static string GetNamespace(ISymbol symbol)
  {
    var containingNamespace = symbol.ContainingNamespace;
    return containingNamespace is null || containingNamespace.IsGlobalNamespace
      ? "<global>"
      : containingNamespace.ToDisplayString();
  }

  private static string NormalizePath(string path)
  {
    return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
  }

  private sealed record TypeDeclarationInfo(
    UnusedSymbolKind Kind,
    string FullName,
    string Namespace,
    string Project,
    string File,
    int Line,
    bool IsPartial);
}
