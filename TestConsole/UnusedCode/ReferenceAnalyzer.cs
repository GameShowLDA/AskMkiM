using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using NLog;

namespace TestConsole.UnusedCode;

/// <summary>
/// Counts source references for symbols by using Roslyn reference discovery.
/// </summary>
internal sealed class ReferenceAnalyzer
{
  private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
  private readonly object _usageIndexSync = new();
  private Task<IReadOnlyDictionary<string, HashSet<string>>>? _usageIndexTask;

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
    var locations = new HashSet<string>(StringComparer.Ordinal);

    foreach (var symbolToAnalyze in GetRelatedSymbols(symbol))
    {
      var references = await SymbolFinder.FindReferencesAsync(symbolToAnalyze, solution, cancellationToken)
        .ConfigureAwait(false);

      foreach (var location in references.SelectMany(reference => reference.Locations).Where(location => !location.IsImplicit))
      {
        var lineSpan = location.Location.GetLineSpan();
        locations.Add($"{lineSpan.Path}|{lineSpan.StartLinePosition.Line}|{lineSpan.StartLinePosition.Character}");
      }
    }

    if (symbol is INamedTypeSymbol typeSymbol)
    {
      await AddTypeUsageReferencesAsync(typeSymbol, solution, locations, cancellationToken).ConfigureAwait(false);
      if (locations.Count == 0)
      {
        await AddMemberUsageReferencesAsync(typeSymbol, solution, locations, cancellationToken).ConfigureAwait(false);
      }
    }

    if (locations.Count == 0)
    {
      await AddUsageIndexReferencesAsync(symbol, solution, locations, cancellationToken).ConfigureAwait(false);
    }

    var count = locations.Count;
    Logger.Debug(
      "References found. Symbol: {Symbol}. Count: {Count}. Elapsed: {Elapsed}",
      symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
      count,
      DateTimeOffset.UtcNow - started);

    return new SymbolReferenceInfo(count);
  }

  private static IEnumerable<ISymbol> GetRelatedSymbols(ISymbol symbol)
  {
    yield return symbol;

    if (symbol is not INamedTypeSymbol typeSymbol)
    {
      yield break;
    }

    foreach (var constructor in typeSymbol.Constructors.Where(constructor => !constructor.IsImplicitlyDeclared))
    {
      yield return constructor;
    }
  }

  private static async Task AddTypeUsageReferencesAsync(
    INamedTypeSymbol typeSymbol,
    Solution solution,
    ISet<string> locations,
    CancellationToken cancellationToken)
  {
    var derivedTypes = await SymbolFinder.FindDerivedClassesAsync(typeSymbol, solution, cancellationToken: cancellationToken)
      .ConfigureAwait(false);

    foreach (var derivedType in derivedTypes)
    {
      AddDeclarationLocation(derivedType, locations);
    }

    var implementations = await SymbolFinder.FindImplementationsAsync(typeSymbol, solution, cancellationToken: cancellationToken)
      .ConfigureAwait(false);

    foreach (var implementation in implementations)
    {
      AddDeclarationLocation(implementation, locations);
    }
  }

  private async Task AddMemberUsageReferencesAsync(
    INamedTypeSymbol typeSymbol,
    Solution solution,
    ISet<string> locations,
    CancellationToken cancellationToken)
  {
    foreach (var member in GetTypeMembers(typeSymbol))
    {
      cancellationToken.ThrowIfCancellationRequested();

      var references = await SymbolFinder.FindReferencesAsync(member, solution, cancellationToken)
        .ConfigureAwait(false);

      foreach (var location in references.SelectMany(reference => reference.Locations).Where(location => !location.IsImplicit))
      {
        var lineSpan = location.Location.GetLineSpan();
        locations.Add($"{lineSpan.Path}|{lineSpan.StartLinePosition.Line}|{lineSpan.StartLinePosition.Character}");
      }
    }

    if (locations.Count == 0)
    {
      foreach (var member in GetTypeMembers(typeSymbol))
      {
        await AddUsageIndexReferencesAsync(member, solution, locations, cancellationToken).ConfigureAwait(false);
      }
    }
  }

  private static IEnumerable<ISymbol> GetTypeMembers(INamedTypeSymbol typeSymbol)
  {
    return typeSymbol
      .GetMembers()
      .Where(member => !member.IsImplicitlyDeclared &&
        IsAccessibleMember(member) &&
        member.Kind is SymbolKind.Method or SymbolKind.Property or SymbolKind.Field or SymbolKind.Event);
  }

  private static bool IsAccessibleMember(ISymbol member)
  {
    return member.DeclaredAccessibility is Microsoft.CodeAnalysis.Accessibility.Public
      or Microsoft.CodeAnalysis.Accessibility.Internal
      or Microsoft.CodeAnalysis.Accessibility.Protected
      or Microsoft.CodeAnalysis.Accessibility.ProtectedOrInternal;
  }

  private static void AddDeclarationLocation(ISymbol symbol, ISet<string> locations)
  {
    foreach (var location in symbol.Locations.Where(location => location.IsInSource))
    {
      var lineSpan = location.GetLineSpan();
      locations.Add($"{lineSpan.Path}|{lineSpan.StartLinePosition.Line}|{lineSpan.StartLinePosition.Character}");
    }
  }

  private async Task AddUsageIndexReferencesAsync(
    ISymbol targetSymbol,
    Solution solution,
    ISet<string> locations,
    CancellationToken cancellationToken)
  {
    var usageIndex = await GetUsageIndexAsync(solution, cancellationToken).ConfigureAwait(false);
    foreach (var key in GetSymbolKeys(targetSymbol))
    {
      if (!usageIndex.TryGetValue(key, out var indexedLocations))
      {
        continue;
      }

      foreach (var location in indexedLocations)
      {
        locations.Add(location);
      }
    }

    var targetDeclarationKey = GetDeclarationLocationKey(targetSymbol);
    if (targetDeclarationKey is not null)
    {
      locations.Remove(targetDeclarationKey);
    }

    if (locations.Count > 0)
    {
      Logger.Debug(
        "Semantic usage index references found. Symbol: {Symbol}. Count: {Count}",
        targetSymbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
        locations.Count);
    }
  }

  private Task<IReadOnlyDictionary<string, HashSet<string>>> GetUsageIndexAsync(
    Solution solution,
    CancellationToken cancellationToken)
  {
    lock (_usageIndexSync)
    {
      _usageIndexTask ??= BuildUsageIndexAsync(solution, cancellationToken);
      return _usageIndexTask;
    }
  }

  private static async Task<IReadOnlyDictionary<string, HashSet<string>>> BuildUsageIndexAsync(
    Solution solution,
    CancellationToken cancellationToken)
  {
    var usages = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

    foreach (var project in solution.Projects)
    {
      foreach (var document in project.Documents.Where(document => document.SupportsSyntaxTree && document.SourceCodeKind == SourceCodeKind.Regular))
      {
        cancellationToken.ThrowIfCancellationRequested();

        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
          continue;
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
        {
          continue;
        }

        foreach (var node in root.DescendantNodes())
        {
          cancellationToken.ThrowIfCancellationRequested();
          if (!IsReferenceSyntax(node))
          {
            continue;
          }

          var location = node.GetLocation();
          var lineSpan = location.GetLineSpan();
          var locationKey = $"{lineSpan.Path}|{lineSpan.StartLinePosition.Line}|{lineSpan.StartLinePosition.Character}";

          foreach (var symbolKey in GetReferencedSymbolKeys(node, semanticModel, cancellationToken))
          {
            if (!usages.TryGetValue(symbolKey, out var symbolLocations))
            {
              symbolLocations = new HashSet<string>(StringComparer.Ordinal);
              usages.Add(symbolKey, symbolLocations);
            }

            symbolLocations.Add(locationKey);
          }
        }
      }
    }

    Logger.Info("Semantic usage index built. Symbol keys: {SymbolKeyCount}", usages.Count);
    return usages;
  }

  private static bool IsReferenceSyntax(SyntaxNode node)
  {
    return node is IdentifierNameSyntax
      or GenericNameSyntax
      or QualifiedNameSyntax
      or AliasQualifiedNameSyntax
      or ObjectCreationExpressionSyntax
      or ImplicitObjectCreationExpressionSyntax
      or MemberAccessExpressionSyntax
      or InvocationExpressionSyntax
      or AttributeSyntax
      or ConstructorInitializerSyntax;
  }

  private static IEnumerable<string> GetReferencedSymbolKeys(
    SyntaxNode node,
    SemanticModel semanticModel,
    CancellationToken cancellationToken)
  {
    var symbolInfo = semanticModel.GetSymbolInfo(node, cancellationToken);
    foreach (var key in GetSymbolKeys(symbolInfo.Symbol))
    {
      yield return key;
    }

    foreach (var candidate in symbolInfo.CandidateSymbols)
    {
      foreach (var key in GetSymbolKeys(candidate))
      {
        yield return key;
      }
    }

    var typeInfo = semanticModel.GetTypeInfo(node, cancellationToken);
    foreach (var key in GetSymbolKeys(typeInfo.Type))
    {
      yield return key;
    }

    foreach (var key in GetSymbolKeys(typeInfo.ConvertedType))
    {
      yield return key;
    }
  }

  private static IEnumerable<string> GetSymbolKeys(ISymbol? symbol)
  {
    if (symbol is null)
    {
      yield break;
    }

    switch (symbol)
    {
      case INamedTypeSymbol typeSymbol:
        yield return GetTypeKey(typeSymbol);
        yield return GetTypeNameKey(typeSymbol);
        break;

      case IMethodSymbol { MethodKind: MethodKind.Constructor } constructor:
        yield return GetMethodKey(constructor);
        foreach (var key in GetTypeSymbolKeys(constructor.ContainingType))
        {
          yield return key;
        }

        break;

      case IMethodSymbol method:
        yield return GetMethodKey(method);
        foreach (var key in GetTypeSymbolKeys(method.ContainingType))
        {
          yield return key;
        }

        break;

      case IFieldSymbol field:
        yield return $"{symbol.Kind}|{GetTypeKey(field.ContainingType)}|{field.Name}";
        foreach (var key in GetTypeSymbolKeys(field.ContainingType))
        {
          yield return key;
        }

        foreach (var key in GetTypeSymbolKeys(field.Type))
        {
          yield return key;
        }

        break;

      case IPropertySymbol property:
        yield return $"{symbol.Kind}|{GetTypeKey(property.ContainingType)}|{property.Name}";
        foreach (var key in GetTypeSymbolKeys(property.ContainingType))
        {
          yield return key;
        }

        foreach (var key in GetTypeSymbolKeys(property.Type))
        {
          yield return key;
        }

        break;

      case ILocalSymbol local:
        foreach (var key in GetTypeSymbolKeys(local.Type))
        {
          yield return key;
        }

        break;

      case IParameterSymbol parameter:
        foreach (var key in GetTypeSymbolKeys(parameter.Type))
        {
          yield return key;
        }

        break;

      default:
        if (symbol.ContainingType is not null)
        {
          yield return $"{symbol.Kind}|{GetTypeKey(symbol.ContainingType)}|{symbol.Name}";
        }
        break;
    }
  }

  private static IEnumerable<string> GetTypeSymbolKeys(ITypeSymbol typeSymbol)
  {
    if (typeSymbol is INamedTypeSymbol namedType)
    {
      yield return GetTypeKey(namedType);
      yield return GetTypeNameKey(namedType);
    }
  }

  private static string GetTypeKey(INamedTypeSymbol typeSymbol)
  {
    return $"Type|{typeSymbol.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}";
  }

  private static string GetTypeNameKey(INamedTypeSymbol typeSymbol)
  {
    return $"TypeName|{typeSymbol.MetadataName}";
  }

  private static string GetMethodKey(IMethodSymbol methodSymbol)
  {
    var containingType = GetTypeKey(methodSymbol.ContainingType);
    var parameters = string.Join(
      ",",
      methodSymbol.Parameters.Select(parameter =>
        parameter.Type.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
    return $"Method|{containingType}|{methodSymbol.MethodKind}|{methodSymbol.Name}|{parameters}";
  }

  private static string? GetDeclarationLocationKey(ISymbol symbol)
  {
    var location = symbol.Locations.FirstOrDefault(location => location.IsInSource);
    if (location is null)
    {
      return null;
    }

    var lineSpan = location.GetLineSpan();
    return $"{lineSpan.Path}|{lineSpan.StartLinePosition.Line}|{lineSpan.StartLinePosition.Character}";
  }
}
