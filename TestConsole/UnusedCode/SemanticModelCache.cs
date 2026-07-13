using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;

namespace TestConsole.UnusedCode;

/// <summary>
/// Caches semantic models for documents during a project pass.
/// </summary>
internal sealed class SemanticModelCache
{
  private readonly ConcurrentDictionary<DocumentId, Task<SemanticModel?>> _semanticModels = new();

  /// <summary>
  /// Gets a cached semantic model for the specified document.
  /// </summary>
  /// <param name="document">The Roslyn document.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The semantic model, or <see langword="null"/> when Roslyn cannot create one.</returns>
  public Task<SemanticModel?> GetAsync(Document document, CancellationToken cancellationToken)
  {
    return _semanticModels.GetOrAdd(
      document.Id,
      _ => document.GetSemanticModelAsync(cancellationToken));
  }

  /// <summary>
  /// Clears all cached semantic models after a project has been analyzed.
  /// </summary>
  public void Clear()
  {
    _semanticModels.Clear();
  }
}
