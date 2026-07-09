using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using NLog;

namespace TestConsole.UnusedCode;

/// <summary>
/// Counts source references for symbols by using Roslyn reference discovery.
/// </summary>
internal sealed class ReferenceAnalyzer
{
  private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

  /// <summary>
  /// Finds references to a symbol within the complete solution.
  /// </summary>
  /// <param name="symbol">The symbol to analyze.</param>
  /// <param name="solution">The solution that contains the symbol.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>Reference count information.</returns>
  public async Task<SymbolReferenceInfo> AnalyzeAsync(
    ISymbol symbol,
    Solution solution,
    CancellationToken cancellationToken)
  {
    var started = DateTimeOffset.UtcNow;
    var references = await SymbolFinder.FindReferencesAsync(symbol, solution, cancellationToken)
      .ConfigureAwait(false);

    var count = references.Sum(reference => reference.Locations.Count(location => !location.IsImplicit));
    Logger.Debug(
      "References found. Symbol: {Symbol}. Count: {Count}. Elapsed: {Elapsed}",
      symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
      count,
      DateTimeOffset.UtcNow - started);

    return new SymbolReferenceInfo(count);
  }
}
