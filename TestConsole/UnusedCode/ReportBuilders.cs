using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace TestConsole.UnusedCode;

/// <summary>
/// Builds analyzer report files.
/// </summary>
internal interface IReportBuilder
{
  /// <summary>
  /// Gets the target report file extension.
  /// </summary>
  string Extension { get; }

  /// <summary>
  /// Writes the report to disk.
  /// </summary>
  /// <param name="result">The analyzer result.</param>
  /// <param name="filePath">The output file path.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>A task representing the asynchronous operation.</returns>
  Task BuildAsync(UnusedCodeAnalysisResult result, string filePath, CancellationToken cancellationToken);
}

/// <summary>
/// Coordinates all concrete report builders.
/// </summary>
internal sealed class ReportBuilder
{
  private readonly IReadOnlyList<IReportBuilder> _builders =
  [
    new MarkdownReportBuilder(),
    new HtmlReportBuilder(),
    new JsonReportBuilder()
  ];

  /// <summary>
  /// Writes Markdown, HTML, and JSON reports to the specified directory.
  /// </summary>
  /// <param name="result">The analyzer result.</param>
  /// <param name="reportsDirectory">The report directory.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The generated file paths.</returns>
  public async Task<IReadOnlyList<string>> BuildAllAsync(
    UnusedCodeAnalysisResult result,
    string reportsDirectory,
    CancellationToken cancellationToken)
  {
    Directory.CreateDirectory(reportsDirectory);
    var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
    var paths = new List<string>();

    foreach (var builder in _builders)
    {
      var filePath = Path.Combine(reportsDirectory, $"UnusedCode_{timestamp}.{builder.Extension}");
      await builder.BuildAsync(result, filePath, cancellationToken).ConfigureAwait(false);
      paths.Add(filePath);
    }

    return paths;
  }
}

/// <summary>
/// Builds a Markdown report grouped by project, namespace, and symbol kind.
/// </summary>
internal sealed class MarkdownReportBuilder : IReportBuilder
{
  /// <inheritdoc />
  public string Extension => "md";

  /// <inheritdoc />
  public async Task BuildAsync(UnusedCodeAnalysisResult result, string filePath, CancellationToken cancellationToken)
  {
    var builder = new StringBuilder();
    builder.AppendLine("# Unused code report");
    builder.AppendLine();
    builder.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    builder.AppendLine($"Elapsed: {result.Elapsed}");
    builder.AppendLine();

    foreach (var projectGroup in result.Findings.OrderBy(f => f.Project).GroupBy(f => f.Project))
    {
      builder.AppendLine($"## Project: {projectGroup.Key}");
      foreach (var namespaceGroup in projectGroup.OrderBy(f => f.Namespace).GroupBy(f => f.Namespace))
      {
        builder.AppendLine($"### Namespace: {namespaceGroup.Key}");
        foreach (var kindGroup in namespaceGroup.OrderBy(f => f.Kind).GroupBy(f => f.Kind))
        {
          builder.AppendLine($"#### {kindGroup.Key}");
          foreach (var finding in kindGroup.OrderBy(f => f.File).ThenBy(f => f.Line))
          {
            builder.AppendLine();
            builder.AppendLine($"**{finding.Kind}**");
            builder.AppendLine();
            builder.AppendLine(finding.FullName);
            builder.AppendLine($"Project: {finding.Project}");
            builder.AppendLine($"File: {MakeRelative(finding.File)}:{finding.Line}");
            builder.AppendLine($"References: {finding.References}");
            builder.AppendLine($"Reason: {finding.Reason}");
          }
        }
      }
    }

    AppendStatistics(builder, result);
    await File.WriteAllTextAsync(filePath, builder.ToString(), Encoding.UTF8, cancellationToken)
      .ConfigureAwait(false);
  }

  private static void AppendStatistics(StringBuilder builder, UnusedCodeAnalysisResult result)
  {
    builder.AppendLine();
    builder.AppendLine("==================================");
    builder.AppendLine($"Unused classes: {GetCount(result, UnusedSymbolKind.Class)}");
    builder.AppendLine($"Unused methods: {GetCount(result, UnusedSymbolKind.Method)}");
    builder.AppendLine($"Unused properties: {GetCount(result, UnusedSymbolKind.Property)}");
    builder.AppendLine($"Unused fields: {GetCount(result, UnusedSymbolKind.Field)}");
    builder.AppendLine($"Unused interfaces: {GetCount(result, UnusedSymbolKind.Interface)}");
    builder.AppendLine($"Unused enums: {GetCount(result, UnusedSymbolKind.Enum)}");
    builder.AppendLine($"Unused events: {GetCount(result, UnusedSymbolKind.Event)}");
    builder.AppendLine($"Total: {result.Findings.Count}");
    builder.AppendLine("==================================");
  }

  private static int GetCount(UnusedCodeAnalysisResult result, UnusedSymbolKind kind)
  {
    return result.Counts.TryGetValue(kind, out var count) ? count : 0;
  }

  private static string MakeRelative(string filePath)
  {
    return Path.GetRelativePath(AppContext.BaseDirectory, filePath);
  }
}

/// <summary>
/// Builds an interactive HTML report.
/// </summary>
internal sealed class HtmlReportBuilder : IReportBuilder
{
  /// <inheritdoc />
  public string Extension => "html";

  /// <inheritdoc />
  public async Task BuildAsync(UnusedCodeAnalysisResult result, string filePath, CancellationToken cancellationToken)
  {
    var encoder = HtmlEncoder.Default;
    var builder = new StringBuilder();
    var projects = result.Findings.Select(f => f.Project).Distinct().Order().ToArray();
    var kinds = result.Findings.Select(f => f.Kind.ToString()).Distinct().Order().ToArray();

    builder.AppendLine("<!doctype html><html lang=\"ru\"><head><meta charset=\"utf-8\">");
    builder.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
    builder.AppendLine("<title>Unused code report</title>");
    builder.AppendLine("<style>");
    builder.AppendLine("body{font-family:Segoe UI,Arial,sans-serif;margin:24px;color:#202124;background:#f7f8fa}main{max-width:1280px;margin:auto}h1{font-size:28px}label{font-weight:600}select,input,button{height:34px;margin:4px 12px 12px 0;padding:0 10px;border:1px solid #b8bdc7;border-radius:6px;background:white}.toolbar{position:sticky;top:0;background:#f7f8fa;padding:12px 0;border-bottom:1px solid #d8dce3}.item{background:white;border:1px solid #d8dce3;border-radius:8px;margin:10px 0;padding:12px}summary{cursor:pointer;font-weight:700}.meta{display:grid;grid-template-columns:120px 1fr;gap:4px 12px;margin-top:10px}.kind{font-size:12px;text-transform:uppercase;color:#5f6368}.reason{color:#7a3b00}.stats{white-space:pre;background:#111827;color:#f9fafb;border-radius:8px;padding:16px}.hidden{display:none}");
    builder.AppendLine("</style></head><body><main>");
    builder.AppendLine("<h1>Unused code report</h1>");
    builder.AppendLine($"<p>Generated: {encoder.Encode(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))}; Elapsed: {encoder.Encode(result.Elapsed.ToString())}</p>");
    builder.AppendLine("<section class=\"toolbar\">");
    builder.AppendLine("<label>Project</label><select id=\"project\"><option value=\"\">All</option>");
    foreach (var project in projects)
    {
      builder.AppendLine($"<option>{encoder.Encode(project)}</option>");
    }

    builder.AppendLine("</select><label>Type</label><select id=\"kind\"><option value=\"\">All</option>");
    foreach (var kind in kinds)
    {
      builder.AppendLine($"<option>{encoder.Encode(kind)}</option>");
    }

    builder.AppendLine("</select><label>Search</label><input id=\"search\" type=\"search\" placeholder=\"Symbol, file, namespace\">");
    builder.AppendLine("<button id=\"sort\">Sort by references</button></section>");

    builder.AppendLine("<section id=\"items\">");
    foreach (var finding in result.Findings.OrderBy(f => f.Project).ThenBy(f => f.Namespace).ThenBy(f => f.Kind).ThenBy(f => f.FullName))
    {
      builder.AppendLine(
        $"<details class=\"item\" data-project=\"{encoder.Encode(finding.Project)}\" data-kind=\"{finding.Kind}\" data-references=\"{finding.References}\" data-search=\"{encoder.Encode((finding.FullName + " " + finding.Namespace + " " + finding.File).ToLowerInvariant())}\">");
      builder.AppendLine($"<summary><span class=\"kind\">{finding.Kind}</span> {encoder.Encode(finding.FullName)}</summary>");
      builder.AppendLine("<div class=\"meta\">");
      builder.AppendLine($"<div>Project</div><div>{encoder.Encode(finding.Project)}</div>");
      builder.AppendLine($"<div>Namespace</div><div>{encoder.Encode(finding.Namespace)}</div>");
      builder.AppendLine($"<div>File</div><div>{encoder.Encode(finding.File)}:{finding.Line}</div>");
      builder.AppendLine($"<div>References</div><div>{finding.References}</div>");
      builder.AppendLine($"<div>Reason</div><div class=\"reason\">{encoder.Encode(finding.Reason)}</div>");
      builder.AppendLine("</div></details>");
    }

    builder.AppendLine("</section>");
    builder.AppendLine($"<pre class=\"stats\">{encoder.Encode(BuildStatistics(result))}</pre>");
    builder.AppendLine("<script>");
    builder.AppendLine("const project=document.getElementById('project'),kind=document.getElementById('kind'),search=document.getElementById('search'),items=document.getElementById('items');function apply(){const p=project.value,k=kind.value,q=search.value.toLowerCase();for(const item of items.children){const ok=(!p||item.dataset.project===p)&&(!k||item.dataset.kind===k)&&(!q||item.dataset.search.includes(q));item.classList.toggle('hidden',!ok)}}project.onchange=kind.onchange=search.oninput=apply;document.getElementById('sort').onclick=()=>{[...items.children].sort((a,b)=>Number(a.dataset.references)-Number(b.dataset.references)).forEach(x=>items.appendChild(x));apply()};");
    builder.AppendLine("</script></main></body></html>");

    await File.WriteAllTextAsync(filePath, builder.ToString(), Encoding.UTF8, cancellationToken)
      .ConfigureAwait(false);
  }

  private static string BuildStatistics(UnusedCodeAnalysisResult result)
  {
    return string.Join(Environment.NewLine, new[]
    {
      "==================================",
      $"Unused classes: {GetCount(result, UnusedSymbolKind.Class)}",
      $"Unused methods: {GetCount(result, UnusedSymbolKind.Method)}",
      $"Unused properties: {GetCount(result, UnusedSymbolKind.Property)}",
      $"Unused fields: {GetCount(result, UnusedSymbolKind.Field)}",
      $"Unused interfaces: {GetCount(result, UnusedSymbolKind.Interface)}",
      $"Unused enums: {GetCount(result, UnusedSymbolKind.Enum)}",
      $"Unused events: {GetCount(result, UnusedSymbolKind.Event)}",
      $"Total: {result.Findings.Count}",
      "=================================="
    });
  }

  private static int GetCount(UnusedCodeAnalysisResult result, UnusedSymbolKind kind)
  {
    return result.Counts.TryGetValue(kind, out var count) ? count : 0;
  }
}

/// <summary>
/// Builds a JSON report.
/// </summary>
internal sealed class JsonReportBuilder : IReportBuilder
{
  /// <inheritdoc />
  public string Extension => "json";

  /// <inheritdoc />
  public async Task BuildAsync(UnusedCodeAnalysisResult result, string filePath, CancellationToken cancellationToken)
  {
    var payload = new
    {
      generatedAt = DateTimeOffset.Now,
      elapsed = result.Elapsed,
      statistics = result.Counts.ToDictionary(pair => pair.Key.ToString(), pair => pair.Value),
      total = result.Findings.Count,
      findings = result.Findings
        .OrderBy(f => f.Project)
        .ThenBy(f => f.Namespace)
        .ThenBy(f => f.Kind)
        .ThenBy(f => f.FullName)
    };

    var options = new JsonSerializerOptions
    {
      WriteIndented = true
    };

    await using var stream = File.Create(filePath);
    await JsonSerializer.SerializeAsync(stream, payload, options, cancellationToken).ConfigureAwait(false);
  }
}
