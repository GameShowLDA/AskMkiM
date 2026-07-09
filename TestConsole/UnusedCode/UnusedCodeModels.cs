using Microsoft.CodeAnalysis;

namespace TestConsole.UnusedCode;

/// <summary>
/// Defines the source-code symbol categories reported by the unused-code analyzer.
/// </summary>
internal enum UnusedSymbolKind
{
  /// <summary>A class declaration.</summary>
  Class,

  /// <summary>A record class declaration.</summary>
  Record,

  /// <summary>A struct or record struct declaration.</summary>
  Struct,

  /// <summary>An interface declaration.</summary>
  Interface,

  /// <summary>An enum declaration.</summary>
  Enum,

  /// <summary>A method or constructor declaration.</summary>
  Method,

  /// <summary>A property declaration.</summary>
  Property,

  /// <summary>An event declaration.</summary>
  Event,

  /// <summary>A field declaration.</summary>
  Field
}

/// <summary>
/// Represents a symbol selected for reference analysis.
/// </summary>
/// <param name="Symbol">The Roslyn symbol.</param>
/// <param name="Kind">The report symbol kind.</param>
/// <param name="ProjectName">The project containing the symbol declaration.</param>
/// <param name="Document">The document containing the symbol declaration.</param>
internal sealed record SymbolCandidate(
  ISymbol Symbol,
  UnusedSymbolKind Kind,
  string ProjectName,
  Document Document);

/// <summary>
/// Contains reference-analysis data for a symbol.
/// </summary>
/// <param name="ReferenceCount">The number of Roslyn reference locations found in the solution.</param>
internal sealed record SymbolReferenceInfo(int ReferenceCount);

/// <summary>
/// Describes an unused-code finding.
/// </summary>
/// <param name="Kind">The symbol category.</param>
/// <param name="FullName">The fully qualified symbol name.</param>
/// <param name="Project">The project that declares the symbol.</param>
/// <param name="Namespace">The containing namespace.</param>
/// <param name="File">The source file path.</param>
/// <param name="Line">The one-based declaration line number.</param>
/// <param name="References">The number of Roslyn references.</param>
/// <param name="Reason">The reason why the symbol is suspicious.</param>
internal sealed record UnusedCodeFinding(
  UnusedSymbolKind Kind,
  string FullName,
  string Project,
  string Namespace,
  string File,
  int Line,
  int References,
  string Reason);

/// <summary>
/// Describes an empty project folder found during solution scanning.
/// </summary>
/// <param name="Project">The project that owns the folder.</param>
/// <param name="Path">The empty folder path.</param>
/// <param name="Reason">The reason why the folder is suspicious.</param>
internal sealed record EmptyFolderFinding(
  string Project,
  string Path,
  string Reason);

/// <summary>
/// Stores aggregate analyzer output.
/// </summary>
/// <param name="Findings">The suspicious symbols.</param>
/// <param name="EmptyFolders">The empty project folders.</param>
/// <param name="Elapsed">The total analysis duration.</param>
internal sealed record UnusedCodeAnalysisResult(
  IReadOnlyList<UnusedCodeFinding> Findings,
  IReadOnlyList<EmptyFolderFinding> EmptyFolders,
  TimeSpan Elapsed)
{
  /// <summary>
  /// Gets a count grouped by symbol kind.
  /// </summary>
  public IReadOnlyDictionary<UnusedSymbolKind, int> Counts =>
    Findings
      .GroupBy(finding => finding.Kind)
      .ToDictionary(group => group.Key, group => group.Count());
}

/// <summary>
/// Reports progress while Roslyn projects and documents are being analyzed.
/// </summary>
/// <param name="Project">The current project name.</param>
/// <param name="Document">The current document name.</param>
/// <param name="ProcessedDocuments">The number of processed documents.</param>
/// <param name="TotalDocuments">The total number of documents in the solution.</param>
/// <param name="Elapsed">The elapsed analyzer time.</param>
internal sealed record UnusedCodeProgress(
  string Project,
  string Document,
  int ProcessedDocuments,
  int TotalDocuments,
  TimeSpan Elapsed);
