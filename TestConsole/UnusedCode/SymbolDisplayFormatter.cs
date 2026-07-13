using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TestConsole.UnusedCode;

/// <summary>
/// Provides stable symbol names for reports without compiler diagnostic placeholders.
/// </summary>
internal static class SymbolDisplayFormatter
{
  private static readonly SymbolDisplayFormat TypeFormat = new(
    globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
    typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
    genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
    memberOptions: SymbolDisplayMemberOptions.IncludeContainingType,
    parameterOptions: SymbolDisplayParameterOptions.IncludeType,
    miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
      SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

  private static readonly SymbolDisplayFormat ParameterTypeFormat = new(
    globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
    typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
    genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
    miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
      SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

  /// <summary>
  /// Builds a fully qualified report name for the specified symbol.
  /// </summary>
  /// <param name="symbol">The symbol to format.</param>
  /// <returns>A stable report name.</returns>
  public static string GetFullName(ISymbol symbol)
  {
    if (symbol is INamedTypeSymbol type)
    {
      return GetTypeName(type);
    }

    return $"{GetOwnerName(symbol)}.{GetMemberName(symbol)}";
  }

  /// <summary>
  /// Gets the namespace or containing type that owns the specified symbol.
  /// </summary>
  /// <param name="symbol">The symbol to inspect.</param>
  /// <returns>The owner name.</returns>
  public static string GetOwnerName(ISymbol symbol)
  {
    if (symbol is INamedTypeSymbol type)
    {
      return GetNamespaceName(type);
    }

    return GetMemberOwnerName(symbol);
  }

  /// <summary>
  /// Gets the simple type name or member signature for the specified symbol.
  /// </summary>
  /// <param name="symbol">The symbol to inspect.</param>
  /// <returns>The member display name.</returns>
  public static string GetMemberName(ISymbol symbol)
  {
    return symbol switch
    {
      INamedTypeSymbol type => GetTypeShortName(type),
      IMethodSymbol method => GetMethodMemberName(method),
      IPropertySymbol property => property.Name,
      IEventSymbol eventSymbol => eventSymbol.Name,
      IFieldSymbol field => field.Name,
      _ => RemoveInvalidGlobalCode(symbol.ToDisplayString(TypeFormat))
    };
  }

  /// <summary>
  /// Builds a fully qualified report name for a type.
  /// </summary>
  /// <param name="type">The type symbol.</param>
  /// <returns>A stable type name.</returns>
  public static string GetTypeName(ITypeSymbol type)
  {
    return RemoveInvalidGlobalCode(type.ToDisplayString(TypeFormat));
  }

  private static string GetMethodName(IMethodSymbol method)
  {
    var owner = GetMemberOwnerName(method);
    return $"{owner}.{GetMethodMemberName(method)}";
  }

  private static string GetMethodMemberName(IMethodSymbol method)
  {
    var owner = GetMemberOwnerName(method);
    var name = method.MethodKind == MethodKind.Constructor && method.ContainingType is not null
      ? method.ContainingType.Name
      : method.MethodKind == MethodKind.Constructor
        ? GetOwnerShortName(owner)
      : method.Name;

    if (method.TypeParameters.Length > 0)
    {
      name += $"<{string.Join(", ", method.TypeParameters.Select(parameter => parameter.Name))}>";
    }

    return $"{name}({string.Join(", ", method.Parameters.Select(FormatParameter))})";
  }

  private static string GetOwnerShortName(string owner)
  {
    var lastSegment = owner.Split('.', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
    var genericStartIndex = lastSegment?.IndexOf('<', StringComparison.Ordinal) ?? -1;
    return genericStartIndex > 0
      ? lastSegment![..genericStartIndex]
      : lastSegment ?? ".ctor";
  }

  private static string FormatParameter(IParameterSymbol parameter)
  {
    var prefix = parameter.RefKind switch
    {
      RefKind.Ref => "ref ",
      RefKind.Out => "out ",
      RefKind.In => "in ",
      _ => string.Empty
    };

    return prefix + RemoveInvalidGlobalCode(parameter.Type.ToDisplayString(ParameterTypeFormat));
  }

  private static string GetMemberOwnerName(ISymbol symbol)
  {
    if (symbol.ContainingType is not null)
    {
      return GetTypeName(symbol.ContainingType);
    }

    if (TryGetSyntaxOwnerName(symbol, out var syntaxOwnerName))
    {
      return syntaxOwnerName;
    }

    var containingNamespace = symbol.ContainingNamespace;
    return containingNamespace is null || containingNamespace.IsGlobalNamespace
      ? "<global>"
      : containingNamespace.ToDisplayString();
  }

  private static string GetNamespaceName(INamedTypeSymbol type)
  {
    var containingNamespace = type.ContainingNamespace;
    return containingNamespace is null || containingNamespace.IsGlobalNamespace
      ? "<global>"
      : containingNamespace.ToDisplayString();
  }

  private static string GetTypeShortName(INamedTypeSymbol type)
  {
    var name = type.Name;
    if (type.TypeParameters.Length > 0)
    {
      name += $"<{string.Join(", ", type.TypeParameters.Select(parameter => parameter.Name))}>";
    }

    return name;
  }

  private static bool TryGetSyntaxOwnerName(ISymbol symbol, out string ownerName)
  {
    ownerName = string.Empty;
    var syntax = symbol.DeclaringSyntaxReferences
      .Select(reference => reference.GetSyntax())
      .FirstOrDefault();

    var typeDeclaration = syntax?.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().FirstOrDefault();
    if (typeDeclaration is null)
    {
      return false;
    }

    var typeNames = typeDeclaration
      .AncestorsAndSelf()
      .OfType<TypeDeclarationSyntax>()
      .Reverse()
      .Select(GetSyntaxTypeName);
    var namespaceName = GetSyntaxNamespace(typeDeclaration);
    ownerName = string.IsNullOrWhiteSpace(namespaceName)
      ? string.Join(".", typeNames)
      : $"{namespaceName}.{string.Join(".", typeNames)}";

    return true;
  }

  private static string GetSyntaxTypeName(TypeDeclarationSyntax typeDeclaration)
  {
    return typeDeclaration.Identifier.ValueText + typeDeclaration.TypeParameterList?.ToString();
  }

  private static string GetSyntaxNamespace(SyntaxNode node)
  {
    return node.Ancestors()
      .OfType<BaseNamespaceDeclarationSyntax>()
      .Select(namespaceDeclaration => namespaceDeclaration.Name.ToString())
      .FirstOrDefault() ?? string.Empty;
  }

  private static string RemoveInvalidGlobalCode(string value)
  {
    return value
      .Replace("global::", string.Empty, StringComparison.Ordinal)
      .Replace(".<invalid-global-code>", string.Empty, StringComparison.Ordinal)
      .Replace("<invalid-global-code>.", string.Empty, StringComparison.Ordinal)
      .Replace("<invalid-global-code>", string.Empty, StringComparison.Ordinal);
  }
}
