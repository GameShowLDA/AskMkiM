using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NLog;
using System.Xml.Linq;

namespace TestConsole.UnusedCode;

/// <summary>
/// Collects source symbols and decides whether zero-reference symbols should be reported.
/// </summary>
internal sealed class SymbolAnalyzer
{
  private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
  private readonly SemanticModelCache _semanticModelCache;
  private readonly object _dbSetEntityCacheSync = new();
  private readonly object _xamlTypeCacheSync = new();
  private Task<HashSet<ITypeSymbol>>? _dbSetEntityCache;
  private Task<HashSet<string>>? _xamlTypeCache;

  /// <summary>
  /// Initializes a new instance of the <see cref="SymbolAnalyzer"/> class.
  /// </summary>
  /// <param name="semanticModelCache">The semantic-model cache used during project analysis.</param>
  public SymbolAnalyzer(SemanticModelCache semanticModelCache)
  {
    _semanticModelCache = semanticModelCache;
  }

  /// <summary>
  /// Gets candidate symbols declared in a document.
  /// </summary>
  /// <param name="document">The document to inspect.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>Declared symbols that can be analyzed for references.</returns>
  public async Task<IReadOnlyList<SymbolCandidate>> GetCandidatesAsync(
    Document document,
    CancellationToken cancellationToken)
  {
    var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
    if (root is null || IsGeneratedDocument(document))
    {
      return Array.Empty<SymbolCandidate>();
    }

    var semanticModel = await _semanticModelCache.GetAsync(document, cancellationToken).ConfigureAwait(false);
    if (semanticModel is null)
    {
      return Array.Empty<SymbolCandidate>();
    }

    var projectName = document.Project.Name;
    var candidates = new List<SymbolCandidate>();

    foreach (var node in root.DescendantNodes())
    {
      cancellationToken.ThrowIfCancellationRequested();
      AddCandidateIfSupported(node, semanticModel, document, projectName, candidates, cancellationToken);
    }

    Logger.Debug(
      "Document analyzed. Project: {Project}. Document: {Document}. Candidate symbols: {Count}",
      projectName,
      document.Name,
      candidates.Count);

    return candidates;
  }

  /// <summary>
  /// Determines whether a symbol with zero references should be skipped because framework code can use it indirectly.
  /// </summary>
  /// <param name="candidate">The candidate symbol.</param>
  /// <param name="solution">The containing solution.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns><see langword="true"/> when the symbol should not be reported.</returns>
  public async Task<bool> ShouldSkipAsync(
    SymbolCandidate candidate,
    Solution solution,
    CancellationToken cancellationToken)
  {
    var symbol = candidate.Symbol;
    if (symbol.IsImplicitlyDeclared || !symbol.Locations.Any(location => location.IsInSource))
    {
      return true;
    }

    if (IsEntryPoint(symbol) ||
      IsCompilerOrGeneratedSymbol(symbol) ||
      IsSpecialAttributeSymbol(symbol) ||
      IsTestMethod(symbol) ||
      IsSerializationSymbol(symbol) ||
      await IsEfCoreSymbolAsync(symbol, solution, cancellationToken).ConfigureAwait(false) ||
      IsInterfaceImplementation(symbol) ||
      IsOverride(symbol) ||
      IsFrameworkExtensibilityMember(symbol) ||
      await IsXamlReferencedSymbolAsync(symbol, solution, cancellationToken).ConfigureAwait(false) ||
      IsXamlBackedSymbol(symbol) ||
      await HasDerivedImplementationAsync(symbol, solution, cancellationToken).ConfigureAwait(false))
    {
      return true;
    }

    return false;
  }

  /// <summary>
  /// Creates a report finding from a zero-reference candidate.
  /// </summary>
  /// <param name="candidate">The candidate symbol.</param>
  /// <param name="referenceInfo">Reference count information.</param>
  /// <returns>A report finding.</returns>
  public UnusedCodeFinding CreateFinding(SymbolCandidate candidate, SymbolReferenceInfo referenceInfo)
  {
    var symbol = candidate.Symbol;
    var location = symbol.Locations.FirstOrDefault(item => item.IsInSource);
    var line = location?.GetLineSpan().StartLinePosition.Line + 1 ?? 0;
    var file = location?.SourceTree?.FilePath ?? candidate.Document.FilePath ?? string.Empty;

    return new UnusedCodeFinding(
      candidate.Kind,
      symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
      candidate.ProjectName,
      GetNamespace(symbol),
      file,
      line,
      referenceInfo.ReferenceCount,
      GetReason(candidate.Kind));
  }

  private static void AddCandidateIfSupported(
    SyntaxNode node,
    SemanticModel semanticModel,
    Document document,
    string projectName,
    ICollection<SymbolCandidate> candidates,
    CancellationToken cancellationToken)
  {
    ISymbol? symbol = node switch
    {
      TypeDeclarationSyntax typeDeclaration => semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken),
      EnumDeclarationSyntax enumDeclaration => semanticModel.GetDeclaredSymbol(enumDeclaration, cancellationToken),
      MethodDeclarationSyntax methodDeclaration => semanticModel.GetDeclaredSymbol(methodDeclaration, cancellationToken),
      ConstructorDeclarationSyntax constructorDeclaration => semanticModel.GetDeclaredSymbol(constructorDeclaration, cancellationToken),
      PropertyDeclarationSyntax propertyDeclaration => semanticModel.GetDeclaredSymbol(propertyDeclaration, cancellationToken),
      EventDeclarationSyntax eventDeclaration => semanticModel.GetDeclaredSymbol(eventDeclaration, cancellationToken),
      VariableDeclaratorSyntax variable when variable.Parent?.Parent is FieldDeclarationSyntax or EventFieldDeclarationSyntax =>
        semanticModel.GetDeclaredSymbol(variable, cancellationToken),
      _ => null
    };

    if (symbol is null || symbol.IsImplicitlyDeclared)
    {
      return;
    }

    var kind = GetKind(node, symbol);
    if (kind is null)
    {
      return;
    }

    candidates.Add(new SymbolCandidate(symbol, kind.Value, projectName, document));
  }

  private static UnusedSymbolKind? GetKind(SyntaxNode node, ISymbol symbol)
  {
    return node switch
    {
      RecordDeclarationSyntax recordDeclaration when recordDeclaration.Kind() == SyntaxKind.RecordStructDeclaration => UnusedSymbolKind.Struct,
      RecordDeclarationSyntax => UnusedSymbolKind.Record,
      StructDeclarationSyntax => UnusedSymbolKind.Struct,
      InterfaceDeclarationSyntax => UnusedSymbolKind.Interface,
      EnumDeclarationSyntax => UnusedSymbolKind.Enum,
      ClassDeclarationSyntax => UnusedSymbolKind.Class,
      MethodDeclarationSyntax => UnusedSymbolKind.Method,
      ConstructorDeclarationSyntax => UnusedSymbolKind.Method,
      PropertyDeclarationSyntax => UnusedSymbolKind.Property,
      EventDeclarationSyntax => UnusedSymbolKind.Event,
      VariableDeclaratorSyntax when symbol.Kind == SymbolKind.Field => UnusedSymbolKind.Field,
      VariableDeclaratorSyntax when symbol.Kind == SymbolKind.Event => UnusedSymbolKind.Event,
      _ => null
    };
  }

  private static bool IsEntryPoint(ISymbol symbol)
  {
    return symbol is IMethodSymbol
    {
      Name: "Main",
      IsStatic: true,
      MethodKind: MethodKind.Ordinary
    };
  }

  private static bool IsCompilerOrGeneratedSymbol(ISymbol symbol)
  {
    return HasAttribute(symbol, "CompilerGeneratedAttribute", "GeneratedCodeAttribute") ||
      symbol.Locations.Any(location => location.SourceTree is null);
  }

  private static bool IsSpecialAttributeSymbol(ISymbol symbol)
  {
    if (symbol is INamedTypeSymbol namedType && InheritsFrom(namedType, "System.Attribute"))
    {
      return true;
    }

    return HasAttribute(
      symbol,
      "PreserveAttribute",
      "DynamicallyAccessedMembersAttribute",
      "DynamicDependencyAttribute",
      "RequiresUnreferencedCodeAttribute",
      "ExportAttribute",
      "ImportAttribute",
      "MEFAttribute");
  }

  private static bool IsTestMethod(ISymbol symbol)
  {
    return symbol is IMethodSymbol && HasAttribute(
      symbol,
      "FactAttribute",
      "TheoryAttribute",
      "TestAttribute",
      "TestCaseAttribute",
      "TestMethodAttribute",
      "DataTestMethodAttribute",
      "SetUpAttribute",
      "TearDownAttribute",
      "OneTimeSetUpAttribute",
      "OneTimeTearDownAttribute");
  }

  private static bool IsSerializationSymbol(ISymbol symbol)
  {
    return HasAttribute(
      symbol,
      "JsonPropertyAttribute",
      "JsonPropertyNameAttribute",
      "JsonIgnoreAttribute",
      "DataMemberAttribute",
      "DataContractAttribute",
      "EnumMemberAttribute",
      "XmlElementAttribute",
      "XmlAttributeAttribute",
      "XmlRootAttribute",
      "XmlIgnoreAttribute",
      "YamlMemberAttribute");
  }

  private async Task<bool> IsEfCoreSymbolAsync(
    ISymbol symbol,
    Solution solution,
    CancellationToken cancellationToken)
  {
    if (symbol is INamedTypeSymbol namedType)
    {
      return InheritsFrom(namedType, "Microsoft.EntityFrameworkCore.DbContext") ||
        InheritsFrom(namedType, "Microsoft.EntityFrameworkCore.Migrations.Migration") ||
        HasAttribute(namedType, "TableAttribute", "KeylessAttribute", "OwnedAttribute") ||
        await IsKnownDbSetEntityAsync(namedType, solution, cancellationToken).ConfigureAwait(false);
    }

    if (symbol is IPropertySymbol property)
    {
      return IsDbSet(property.Type) || HasAttribute(property, "KeyAttribute", "ColumnAttribute", "ForeignKeyAttribute");
    }

    return HasAttribute(symbol, "KeyAttribute", "ColumnAttribute", "ForeignKeyAttribute", "NotMappedAttribute");
  }

  private static bool IsInterfaceImplementation(ISymbol symbol)
  {
    if (symbol is INamedTypeSymbol namedType)
    {
      return namedType.Interfaces.Length > 0 || namedType.AllInterfaces.Length > 0;
    }

    var containingType = symbol.ContainingType;
    if (containingType is null)
    {
      return false;
    }

    foreach (var interfaceType in containingType.AllInterfaces)
    {
      foreach (var member in interfaceType.GetMembers())
      {
        var implementation = containingType.FindImplementationForInterfaceMember(member);
        if (SymbolEqualityComparer.Default.Equals(implementation, symbol))
        {
          return true;
        }
      }
    }

    return false;
  }

  private static bool IsOverride(ISymbol symbol)
  {
    return symbol switch
    {
      IMethodSymbol method => method.IsOverride,
      IPropertySymbol property => property.IsOverride,
      IEventSymbol eventSymbol => eventSymbol.IsOverride,
      _ => false
    };
  }

  private static bool IsFrameworkExtensibilityMember(ISymbol symbol)
  {
    if (symbol is IMethodSymbol { MethodKind: MethodKind.Constructor } constructor)
    {
      return constructor.DeclaredAccessibility is Microsoft.CodeAnalysis.Accessibility.Public
        or Microsoft.CodeAnalysis.Accessibility.Internal
        or Microsoft.CodeAnalysis.Accessibility.Protected
        or Microsoft.CodeAnalysis.Accessibility.ProtectedOrInternal;
    }

    return symbol switch
    {
      IMethodSymbol method => method.IsAbstract || method.IsVirtual,
      IPropertySymbol property => property.IsAbstract || property.IsVirtual,
      IEventSymbol eventSymbol => eventSymbol.IsAbstract || eventSymbol.IsVirtual,
      _ => false
    };
  }

  private static bool IsXamlBackedSymbol(ISymbol symbol)
  {
    var type = symbol as INamedTypeSymbol ?? symbol.ContainingType;
    if (type is null)
    {
      return false;
    }

    return IsPartial(type) &&
      (InheritsFrom(type, "System.Windows.Window") ||
        InheritsFrom(type, "System.Windows.Controls.Control") ||
        InheritsFrom(type, "System.Windows.Application") ||
        InheritsFrom(type, "System.Windows.Markup.IComponentConnector"));
  }

  private async Task<bool> IsXamlReferencedSymbolAsync(
    ISymbol symbol,
    Solution solution,
    CancellationToken cancellationToken)
  {
    if (symbol is not INamedTypeSymbol namedType)
    {
      return false;
    }

    var xamlTypes = await GetXamlReferencedTypesAsync(solution, cancellationToken).ConfigureAwait(false);
    return xamlTypes.Contains(namedType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)) ||
      xamlTypes.Contains(namedType.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
  }

  private Task<HashSet<string>> GetXamlReferencedTypesAsync(
    Solution solution,
    CancellationToken cancellationToken)
  {
    lock (_xamlTypeCacheSync)
    {
      _xamlTypeCache ??= BuildXamlReferencedTypesAsync(solution, cancellationToken);
      return _xamlTypeCache;
    }
  }

  private static async Task<HashSet<string>> BuildXamlReferencedTypesAsync(
    Solution solution,
    CancellationToken cancellationToken)
  {
    var referencedTypes = new HashSet<string>(StringComparer.Ordinal);
    var xamlFiles = GetXamlFiles(solution);

    foreach (var xamlFile in xamlFiles)
    {
      cancellationToken.ThrowIfCancellationRequested();

      try
      {
        await using var stream = File.OpenRead(xamlFile);
        var document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken).ConfigureAwait(false);
        foreach (var element in document.Descendants())
        {
          AddXamlElementType(element, referencedTypes);
        }
      }
      catch (Exception ex) when (ex is not OperationCanceledException)
      {
        Logger.Warn(ex, "XAML file could not be parsed: {XamlFile}", xamlFile);
      }
    }

    Logger.Info("XAML referenced types indexed: {Count}", referencedTypes.Count);
    return referencedTypes;
  }

  private static IReadOnlyList<string> GetXamlFiles(Solution solution)
  {
    var projectDirectories = solution.Projects
      .Select(project => project.FilePath)
      .Where(path => !string.IsNullOrWhiteSpace(path))
      .Select(path => Path.GetDirectoryName(path!))
      .Where(path => !string.IsNullOrWhiteSpace(path))
      .Distinct(StringComparer.OrdinalIgnoreCase)
      .ToArray();

    return projectDirectories
      .SelectMany(directory => Directory.EnumerateFiles(directory!, "*.xaml", SearchOption.AllDirectories))
      .Where(path => !IsIgnoredXamlPath(path))
      .Distinct(StringComparer.OrdinalIgnoreCase)
      .ToArray();
  }

  private static bool IsIgnoredXamlPath(string path)
  {
    return path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
      path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);
  }

  private static void AddXamlElementType(XElement element, ISet<string> referencedTypes)
  {
    var namespaceName = element.Name.NamespaceName;
    if (!TryGetClrNamespace(namespaceName, out var clrNamespace))
    {
      return;
    }

    var localName = element.Name.LocalName;
    if (string.IsNullOrWhiteSpace(localName) || localName.Contains('.', StringComparison.Ordinal))
    {
      return;
    }

    referencedTypes.Add($"{clrNamespace}.{localName}");
  }

  private static bool TryGetClrNamespace(string namespaceName, out string clrNamespace)
  {
    clrNamespace = string.Empty;
    const string prefix = "clr-namespace:";
    if (!namespaceName.StartsWith(prefix, StringComparison.Ordinal))
    {
      return false;
    }

    var value = namespaceName[prefix.Length..];
    var separatorIndex = value.IndexOf(';', StringComparison.Ordinal);
    clrNamespace = separatorIndex >= 0 ? value[..separatorIndex] : value;
    return !string.IsNullOrWhiteSpace(clrNamespace);
  }

  private static async Task<bool> HasDerivedImplementationAsync(
    ISymbol symbol,
    Solution solution,
    CancellationToken cancellationToken)
  {
    if (symbol is not IMethodSymbol and not IPropertySymbol and not IEventSymbol)
    {
      return false;
    }

    if (symbol.ContainingType is null || !IsVirtualLike(symbol))
    {
      return false;
    }

    var derivedTypes = await Microsoft.CodeAnalysis.FindSymbols.SymbolFinder
      .FindDerivedClassesAsync(symbol.ContainingType, solution, cancellationToken: cancellationToken)
      .ConfigureAwait(false);

    foreach (var derivedType in derivedTypes)
    {
      foreach (var member in derivedType.GetMembers(symbol.Name))
      {
        if (IsOverrideOf(member, symbol))
        {
          return true;
        }
      }
    }

    return false;
  }

  private static bool IsVirtualLike(ISymbol symbol)
  {
    return symbol switch
    {
      IMethodSymbol method => method.IsAbstract || method.IsVirtual,
      IPropertySymbol property => property.IsAbstract || property.IsVirtual,
      IEventSymbol eventSymbol => eventSymbol.IsAbstract || eventSymbol.IsVirtual,
      _ => false
    };
  }

  private static bool IsOverrideOf(ISymbol member, ISymbol baseMember)
  {
    return member switch
    {
      IMethodSymbol method => SymbolEqualityComparer.Default.Equals(method.OverriddenMethod, baseMember),
      IPropertySymbol property => SymbolEqualityComparer.Default.Equals(property.OverriddenProperty, baseMember),
      IEventSymbol eventSymbol => SymbolEqualityComparer.Default.Equals(eventSymbol.OverriddenEvent, baseMember),
      _ => false
    };
  }

  private async Task<bool> IsKnownDbSetEntityAsync(
    INamedTypeSymbol type,
    Solution solution,
    CancellationToken cancellationToken)
  {
    var entities = await GetDbSetEntityTypesAsync(solution, cancellationToken).ConfigureAwait(false);
    return entities.Contains(type);
  }

  private Task<HashSet<ITypeSymbol>> GetDbSetEntityTypesAsync(
    Solution solution,
    CancellationToken cancellationToken)
  {
    lock (_dbSetEntityCacheSync)
    {
      _dbSetEntityCache ??= BuildDbSetEntityTypesAsync(solution, cancellationToken);
      return _dbSetEntityCache;
    }
  }

  private static async Task<HashSet<ITypeSymbol>> BuildDbSetEntityTypesAsync(
    Solution solution,
    CancellationToken cancellationToken)
  {
    var entityTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

    foreach (var project in solution.Projects)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
      if (compilation is null)
      {
        continue;
      }

      foreach (var syntaxTree in compilation.SyntaxTrees)
      {
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        cancellationToken.ThrowIfCancellationRequested();
        var root = await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
        foreach (var propertyDeclaration in root.DescendantNodes().OfType<PropertyDeclarationSyntax>())
        {
          var property = semanticModel.GetDeclaredSymbol(propertyDeclaration, cancellationToken);
          if (property is null || !IsDbSet(property.Type))
          {
            continue;
          }

          if (property.Type is INamedTypeSymbol { TypeArguments.Length: 1 } dbSet &&
            dbSet.TypeArguments[0] is { } entityType)
          {
            entityTypes.Add(entityType);
          }
        }
      }
    }

    return entityTypes;
  }

  private static bool IsDbSet(ITypeSymbol type)
  {
    return type is INamedTypeSymbol namedType &&
      namedType.ConstructedFrom.ToDisplayString() == "Microsoft.EntityFrameworkCore.DbSet<TEntity>";
  }

  private static bool HasAttribute(ISymbol symbol, params string[] attributeNames)
  {
    return symbol.GetAttributes().Any(attribute =>
    {
      var name = attribute.AttributeClass?.Name;
      var fullName = attribute.AttributeClass?.ToDisplayString();
      return attributeNames.Any(expected =>
        string.Equals(name, expected, StringComparison.Ordinal) ||
        string.Equals(fullName, expected, StringComparison.Ordinal) ||
        fullName?.EndsWith("." + expected, StringComparison.Ordinal) == true);
    });
  }

  private static bool InheritsFrom(INamedTypeSymbol type, string metadataName)
  {
    for (var current = type; current is not null; current = current.BaseType)
    {
      if (current.ToDisplayString() == metadataName)
      {
        return true;
      }
    }

    return type.AllInterfaces.Any(interfaceType => interfaceType.ToDisplayString() == metadataName);
  }

  private static bool IsPartial(INamedTypeSymbol type)
  {
    return type.DeclaringSyntaxReferences
      .Select(reference => reference.GetSyntax())
      .OfType<TypeDeclarationSyntax>()
      .Any(declaration => declaration.Modifiers.Any(SyntaxKind.PartialKeyword));
  }

  private static bool IsGeneratedDocument(Document document)
  {
    return document.FilePath is null ||
      document.FilePath.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
      document.FilePath.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) ||
      document.FilePath.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase) ||
      document.FilePath.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase);
  }

  private static string GetNamespace(ISymbol symbol)
  {
    var containingNamespace = symbol.ContainingNamespace;
    return containingNamespace is null || containingNamespace.IsGlobalNamespace
      ? "<global>"
      : containingNamespace.ToDisplayString();
  }

  private static string GetReason(UnusedSymbolKind kind)
  {
    return kind switch
    {
      UnusedSymbolKind.Class => "На класс отсутствуют ссылки.",
      UnusedSymbolKind.Record => "На record отсутствуют ссылки.",
      UnusedSymbolKind.Struct => "На struct отсутствуют ссылки.",
      UnusedSymbolKind.Interface => "На интерфейс отсутствуют ссылки.",
      UnusedSymbolKind.Enum => "На enum отсутствуют ссылки.",
      UnusedSymbolKind.Method => "Метод нигде не вызывается.",
      UnusedSymbolKind.Property => "Свойство нигде не используется.",
      UnusedSymbolKind.Event => "Событие нигде не используется.",
      UnusedSymbolKind.Field => "Поле нигде не используется.",
      _ => "На символ отсутствуют ссылки."
    };
  }
}
