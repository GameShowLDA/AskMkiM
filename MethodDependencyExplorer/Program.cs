using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using System.Xml.Linq;
namespace MethodDependencyExplorer;


internal static class Program
{
  private static int Main(string[] args)
  {
    try
    {
      Console.OutputEncoding = Encoding.UTF8;

      var path = args.FirstOrDefault();
      if (string.IsNullOrWhiteSpace(path))
      {
        Console.Write("Укажи путь к .cs файлу: ");
        path = Console.ReadLine();
      }

      if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
      {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Файл не найден.");
        Console.ResetColor();
        return 1;
      }

      var code = File.ReadAllText(path, Encoding.UTF8);

      // 1) Парсинг и семантика
      var syntaxTree = CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(LanguageVersion.Preview));
      var compilation = CreateCompilation(syntaxTree);

      var model = compilation.GetSemanticModel(syntaxTree, ignoreAccessibility: true);
      var root = syntaxTree.GetCompilationUnitRoot();

      // 2) Собираем все локальные (в этом файле) методы и свойства
      var localSymbols = CollectLocalMethodAndPropertySymbols(root, model);

      // 3) Строим зависимости (только методы/свойства из этого же файла)
      var edges = BuildDependencies(root, model, localSymbols);

      // 4) Сводная таблица "символ → краткое summary"
      var summaries = localSymbols.ToDictionary(s => s, s => GetOneLineSummary(s));

      // 5) Печать: по каждому символу — дерево
      foreach (var symbol in localSymbols.OrderBy(DisplayName))
      {
        PrintRoot(symbol, summaries, edges);
      }

      return 0;
    }
    catch (Exception ex)
    {
      Console.ForegroundColor = ConsoleColor.Red;
      Console.WriteLine("Ошибка: " + ex);
      Console.ResetColor();
      return 2;
    }
  }

  // ------------------------------
  // Компиляция с базовыми reference'ами
  // ------------------------------
  private static CSharpCompilation CreateCompilation(SyntaxTree syntaxTree)
  {
    // Подтягиваем базовые сборки из текущего рантайма
    var refs = new List<MetadataReference>
    {
      MetadataReference.CreateFromFile(typeof(object).Assembly.Location),               // System.Private.CoreLib
      MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),              // System.Console
      MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),           // System.Linq
      MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location),               // System.Collections
      MetadataReference.CreateFromFile(typeof(System.Runtime.GCSettings).Assembly.Location) // System.Runtime
    };

    return CSharpCompilation.Create(
      assemblyName: "InMemoryAnalysis",
      syntaxTrees: new[] { syntaxTree },
      references: refs,
      options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
  }

  // ------------------------------
  // Сбор методов и свойств в файле
  // ------------------------------
  private static HashSet<ISymbol> CollectLocalMethodAndPropertySymbols(CompilationUnitSyntax root, SemanticModel model)
  {
    var set = new HashSet<ISymbol>(SymbolEqualityComparer.Default);

    // Методы
    foreach (var m in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
    {
      var sym = model.GetDeclaredSymbol(m);
      if (sym != null) set.Add(sym);
    }

    // Свойства
    foreach (var p in root.DescendantNodes().OfType<PropertyDeclarationSyntax>())
    {
      var sym = model.GetDeclaredSymbol(p);
      if (sym != null) set.Add(sym);
    }

    // Лямбды/локальные функции — игнорируем, если нет строгого требования
    return set;
  }

  // ------------------------------
  // Построение зависимостей: кто кого вызывает/читает
  // ------------------------------
  private static Dictionary<ISymbol, HashSet<ISymbol>> BuildDependencies(
    CompilationUnitSyntax root,
    SemanticModel model,
    HashSet<ISymbol> localSet)
  {
    var edges = new Dictionary<ISymbol, HashSet<ISymbol>>(SymbolEqualityComparer.Default);

    // Для методов — смотрим тело + стрелочное тело
    foreach (var m in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
    {
      var from = model.GetDeclaredSymbol(m);
      if (from == null || !localSet.Contains(from)) continue;

      var targets = FindTargetsInBody(m.Body, m.ExpressionBody?.Expression, model, localSet);
      edges[from] = targets;
    }

    // Для свойств — смотрим аксессоры (get/set) и стрелочное тело
    foreach (var p in root.DescendantNodes().OfType<PropertyDeclarationSyntax>())
    {
      var from = model.GetDeclaredSymbol(p);
      if (from == null || !localSet.Contains(from)) continue;

      var targets = new HashSet<ISymbol>(SymbolEqualityComparer.Default);

      // get/set с блоками
      foreach (var accessor in p.AccessorList?.Accessors ?? Enumerable.Empty<AccessorDeclarationSyntax>())
        UnionWith(targets, FindTargetsInBody(accessor.Body, accessor.ExpressionBody?.Expression, model, localSet));

      // property => expr
      if (p.ExpressionBody != null)
        UnionWith(targets, FindTargetsInBody(null, p.ExpressionBody.Expression, model, localSet));

      edges[from] = targets;
    }

    return edges;
  }

  private static void UnionWith(HashSet<ISymbol> set, HashSet<ISymbol> add)
  {
    foreach (var s in add) set.Add(s);
  }

  // Поиск вызовов методов и обращений к свойствам в теле/выражении
  private static HashSet<ISymbol> FindTargetsInBody(
    BlockSyntax? body,
    ExpressionSyntax? expressionBody,
    SemanticModel model,
    HashSet<ISymbol> localSet)
  {
    var result = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
    var nodes = Enumerable.Empty<SyntaxNode>();

    if (body != null) nodes = nodes.Concat(body.DescendantNodes());
    if (expressionBody != null) nodes = nodes.Concat(expressionBody.DescendantNodesAndSelf());

    foreach (var node in nodes)
    {
      // Вызовы методов
      if (node is InvocationExpressionSyntax inv)
      {
        var info = model.GetSymbolInfo(inv);
        var sym = info.Symbol;
        if (sym is IMethodSymbol ms && localSet.Contains(ms))
          result.Add(ms);
      }

      // Обращения к свойствам (через MemberAccess и через идентификатор)
      if (node is MemberAccessExpressionSyntax ma)
      {
        var sym = model.GetSymbolInfo(ma).Symbol;
        if (sym is IPropertySymbol ps && localSet.Contains(ps))
          result.Add(ps);
      }
      else if (node is IdentifierNameSyntax id)
      {
        var sym = model.GetSymbolInfo(id).Symbol;
        if (sym is IPropertySymbol ps && localSet.Contains(ps))
          result.Add(ps);
      }
    }

    return result;
  }

  // ------------------------------
  // Вывод дерева
  // ------------------------------
  private static void PrintRoot(ISymbol root, Dictionary<ISymbol, string> summaries, Dictionary<ISymbol, HashSet<ISymbol>> edges)
  {
    var isMethod = root is IMethodSymbol;
    var kindRu = isMethod ? "Метод" : "Свойство";

    var header = $"{kindRu}: {DisplayName(root)} — {summaries.GetValueOrDefault(root) ?? ""}".TrimEnd();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine(header);
    Console.ResetColor();

    var visited = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
    visited.Add(root);
    PrintChildren(root, edges, summaries, visited, prefix: "");
    Console.WriteLine();
  }

  private static void PrintChildren(
    ISymbol parent,
    Dictionary<ISymbol, HashSet<ISymbol>> edges,
    Dictionary<ISymbol, string> summaries,
    HashSet<ISymbol> path,
    string prefix)
  {
    if (!edges.TryGetValue(parent, out var children) || children.Count == 0) return;

    var list = children.OrderBy(DisplayName).ToList();
    for (int i = 0; i < list.Count; i++)
    {
      var child = list[i];
      var isLast = i == list.Count - 1;

      var branch = isLast ? " └─ " : " ├─ ";
      var nextPrefix = prefix + (isLast ? "   " : " │  ");

      var label = $"{DisplayName(child)} — {summaries.GetValueOrDefault(child) ?? ""}".TrimEnd();
      if (path.Contains(child))
      {
        // цикл
        Console.WriteLine(prefix + branch + label + "  (цикл)");
        continue;
      }

      Console.WriteLine(prefix + branch + label);

      path.Add(child);
      PrintChildren(child, edges, summaries, path, nextPrefix);
      path.Remove(child);
    }
  }

  // ------------------------------
  // Утилиты: имя и summary
  // ------------------------------
  private static string DisplayName(ISymbol symbol)
  {
    switch (symbol)
    {
      case IMethodSymbol m:
        var pars = string.Join(", ", m.Parameters.Select(p => p.Type.Name));
        var owner = m.ContainingType?.Name;
        return owner != null
          ? $"{owner}.{m.Name}({pars})"
          : $"{m.Name}({pars})";

      case IPropertySymbol p:
        var o = p.ContainingType?.Name;
        return o != null ? $"{o}.{p.Name} {{…}}" : $"{p.Name} {{…}}";

      default:
        return symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
    }
  }

  private static string GetOneLineSummary(ISymbol symbol)
  {
    try
    {
      var xml = symbol.GetDocumentationCommentXml(expandIncludes: true, cancellationToken: default);
      if (string.IsNullOrWhiteSpace(xml)) return "";

      var x = XDocument.Parse(xml);
      var sum = x.Descendants("summary").FirstOrDefault()?.Value ?? "";
      var oneLine = NormalizeWhitespace(sum);
      return oneLine;
    }
    catch
    {
      return "";
    }
  }

  private static string NormalizeWhitespace(string s)
  {
    if (string.IsNullOrWhiteSpace(s)) return "";
    var sb = new StringBuilder(s.Length);
    bool wasWs = false;
    foreach (var ch in s)
    {
      if (char.IsWhiteSpace(ch))
      {
        if (!wasWs) { sb.Append(' '); wasWs = true; }
      }
      else
      {
        sb.Append(ch);
        wasWs = false;
      }
    }
    return sb.ToString().Trim();
  }
}
