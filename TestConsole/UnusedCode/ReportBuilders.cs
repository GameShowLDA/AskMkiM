using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Security.Cryptography;

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
            builder.AppendLine($"Owner: {finding.OwnerName}");
            builder.AppendLine($"Member: {finding.MemberName}");
            builder.AppendLine($"Project: {finding.Project}");
            builder.AppendLine($"File: {MakeRelative(finding.File)}:{finding.Line}");
            builder.AppendLine($"References: {finding.References}");
            builder.AppendLine($"Reason: {finding.Reason}");
          }
        }
      }
    }

    if (result.EmptyFolders.Count > 0)
    {
      builder.AppendLine();
      builder.AppendLine("## Empty folders");
      foreach (var projectGroup in result.EmptyFolders.OrderBy(f => f.Project).GroupBy(f => f.Project))
      {
        builder.AppendLine($"### Project: {projectGroup.Key}");
        foreach (var folder in projectGroup.OrderBy(f => f.Path))
        {
          builder.AppendLine();
          builder.AppendLine("**EmptyFolder**");
          builder.AppendLine();
          builder.AppendLine($"Project: {folder.Project}");
          builder.AppendLine($"Folder: {MakeRelative(folder.Path)}");
          builder.AppendLine($"Reason: {folder.Reason}");
        }
      }
    }

    if (result.DuplicateTypes.Count > 0)
    {
      builder.AppendLine();
      builder.AppendLine("## Duplicate types");
      foreach (var duplicate in result.DuplicateTypes.OrderBy(f => f.FullName))
      {
        builder.AppendLine();
        builder.AppendLine($"**DuplicateType: {duplicate.Kind}**");
        builder.AppendLine();
        builder.AppendLine(duplicate.FullName);
        builder.AppendLine($"Namespace: {duplicate.Namespace}");
        builder.AppendLine($"Occurrences: {duplicate.Occurrences.Count}");
        builder.AppendLine($"Reason: {duplicate.Reason}");
        foreach (var occurrence in duplicate.Occurrences)
        {
          builder.AppendLine($"- {occurrence.Project}: {MakeRelative(occurrence.File)}:{occurrence.Line}");
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
    builder.AppendLine($"Unused constructors: {GetCount(result, UnusedSymbolKind.Constructor)}");
    builder.AppendLine($"Unused properties: {GetCount(result, UnusedSymbolKind.Property)}");
    builder.AppendLine($"Unused fields: {GetCount(result, UnusedSymbolKind.Field)}");
    builder.AppendLine($"Unused interfaces: {GetCount(result, UnusedSymbolKind.Interface)}");
    builder.AppendLine($"Unused enums: {GetCount(result, UnusedSymbolKind.Enum)}");
    builder.AppendLine($"Unused events: {GetCount(result, UnusedSymbolKind.Event)}");
    builder.AppendLine($"Empty folders: {result.EmptyFolders.Count}");
    builder.AppendLine($"Duplicate types: {result.DuplicateTypes.Count}");
    builder.AppendLine($"Total: {result.Findings.Count + result.EmptyFolders.Count + result.DuplicateTypes.Count}");
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
    var projects = result.Findings.Select(f => f.Project)
      .Concat(result.EmptyFolders.Select(f => f.Project))
      .Concat(result.DuplicateTypes.SelectMany(f => f.Occurrences.Select(o => o.Project)))
      .Distinct()
      .Order()
      .ToArray();
    var kinds = result.Findings.Select(f => f.Kind.ToString())
      .Append("EmptyFolder")
      .Append("DuplicateType")
      .Distinct()
      .Order()
      .ToArray();

    builder.AppendLine("<!doctype html><html lang=\"ru\"><head><meta charset=\"utf-8\">");
    builder.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
    builder.AppendLine("<title>Unused code report</title>");
    builder.AppendLine("<style>");
    builder.AppendLine("body{font-family:Segoe UI,Arial,sans-serif;margin:24px;color:#202124;background:#f7f8fa}main{max-width:1280px;margin:auto}h1{font-size:28px}h2{font-size:20px;margin-top:28px}label{font-weight:600}select,input,button{height:34px;margin:4px 12px 12px 0;padding:0 10px;border:1px solid #b8bdc7;border-radius:6px;background:white}button{cursor:pointer}.toolbar{position:sticky;top:0;background:#f7f8fa;padding:12px 0;border-bottom:1px solid #d8dce3;z-index:2}.toolbar-row{display:flex;align-items:center;flex-wrap:wrap;gap:0 8px}.ignore-input{min-width:420px;flex:1}.ignore-list{display:flex;flex-wrap:wrap;gap:8px;margin:0 0 12px 0}.ignore-rule{display:inline-flex;align-items:center;gap:6px;max-width:100%;padding:6px 8px;border:1px solid #cfd4dc;border-radius:6px;background:#fff}.ignore-rule.disabled{opacity:.55}.ignore-rule input{height:auto;margin:0}.ignore-rule span{overflow:hidden;text-overflow:ellipsis;white-space:nowrap;max-width:720px}.ignore-rule button{height:24px;margin:0;padding:0 8px}.item{background:white;border:1px solid #d8dce3;border-radius:8px;margin:10px 0;padding:12px;transition:opacity .18s ease,background .18s ease,border-color .18s ease}summary{cursor:pointer;font-weight:700;list-style:none}summary::-webkit-details-marker{display:none}.summary-row{display:flex;align-items:center;gap:10px}.done-check{width:18px;height:18px;margin:0;flex:0 0 auto;accent-color:#2f7d32}.symbol-title{line-height:1.35;min-width:0}.owner-name{color:#374151}.member-name{font-weight:800;color:#111827}.meta{display:grid;grid-template-columns:120px 1fr;gap:4px 12px;margin-top:10px}.kind{font-size:12px;text-transform:uppercase;color:#5f6368}.reason{color:#7a3b00}.item.done{background:#f1f3f4;border-color:#c8d3c8;opacity:.72}.item.done .symbol-title{text-decoration:line-through;text-decoration-thickness:2px;color:#5f6368}.item.done .reason{text-decoration:line-through;color:#6b7280}.stats{white-space:pre;background:#111827;color:#f9fafb;border-radius:8px;padding:16px}.hidden{display:none}");
    builder.AppendLine("</style></head><body><main>");
    builder.AppendLine("<h1>Unused code report</h1>");
    builder.AppendLine($"<p>Generated: {encoder.Encode(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))}; Elapsed: {encoder.Encode(result.Elapsed.ToString())}</p>");
    builder.AppendLine("<section class=\"toolbar\"><div class=\"toolbar-row\">");
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
    builder.AppendLine("<button id=\"sort\">Sort by references</button></div>");
    builder.AppendLine("<div class=\"toolbar-row\"><label>Ignored folders</label><input id=\"ignoredFolderInput\" class=\"ignore-input\" type=\"text\" placeholder=\"Folder path or part of path\"><button id=\"addIgnoredFolder\">Add</button><button id=\"clearIgnoredFolders\">Clear ignored</button></div><div id=\"ignoredFolderList\" class=\"ignore-list\"></div></section>");

    builder.AppendLine("<section id=\"items\">");
    var order = 0;
    foreach (var finding in result.Findings.OrderBy(f => f.Project).ThenBy(f => f.Namespace).ThenBy(f => f.Kind).ThenBy(f => f.FullName))
    {
      var id = BuildFindingId(finding);
      var folder = Path.GetDirectoryName(finding.File) ?? string.Empty;
      builder.AppendLine(
        $"<details class=\"item\" data-id=\"{id}\" data-order=\"{order++}\" data-project=\"{encoder.Encode(finding.Project)}\" data-kind=\"{finding.Kind}\" data-references=\"{finding.References}\" data-folder=\"{encoder.Encode(folder)}\" data-file=\"{encoder.Encode(finding.File)}\" data-search=\"{encoder.Encode((finding.FullName + " " + finding.OwnerName + " " + finding.MemberName + " " + finding.Namespace + " " + finding.File).ToLowerInvariant())}\">");
      builder.AppendLine("<summary>");
      builder.AppendLine("<span class=\"summary-row\">");
      builder.AppendLine($"<input class=\"done-check\" type=\"checkbox\" aria-label=\"Done\" data-id=\"{id}\">");
      builder.AppendLine($"<span class=\"symbol-title\"><span class=\"kind\">{finding.Kind}</span> <span class=\"owner-name\">{encoder.Encode(finding.OwnerName)}</span><span class=\"member-name\">.{encoder.Encode(finding.MemberName)}</span></span>");
      builder.AppendLine("</span>");
      builder.AppendLine("</summary>");
      builder.AppendLine("<div class=\"meta\">");
      builder.AppendLine($"<div>Project</div><div>{encoder.Encode(finding.Project)}</div>");
      builder.AppendLine($"<div>Namespace</div><div>{encoder.Encode(finding.Namespace)}</div>");
      builder.AppendLine($"<div>Owner</div><div>{encoder.Encode(finding.OwnerName)}</div>");
      builder.AppendLine($"<div>Member</div><div>{encoder.Encode(finding.MemberName)}</div>");
      builder.AppendLine($"<div>Full name</div><div>{encoder.Encode(finding.FullName)}</div>");
      builder.AppendLine($"<div>File</div><div>{encoder.Encode(finding.File)}:{finding.Line}</div>");
      builder.AppendLine($"<div>References</div><div>{finding.References}</div>");
      builder.AppendLine($"<div>Reason</div><div class=\"reason\">{encoder.Encode(finding.Reason)}</div>");
      builder.AppendLine($"<div>Folder</div><div>{encoder.Encode(folder)} <button class=\"ignore-folder\" type=\"button\">Ignore folder</button></div>");
      builder.AppendLine("</div></details>");
    }

    foreach (var folder in result.EmptyFolders.OrderBy(f => f.Project).ThenBy(f => f.Path))
    {
      var id = BuildFolderId(folder);
      builder.AppendLine(
        $"<details class=\"item empty-folder\" data-id=\"{id}\" data-order=\"{order++}\" data-project=\"{encoder.Encode(folder.Project)}\" data-kind=\"EmptyFolder\" data-references=\"0\" data-folder=\"{encoder.Encode(folder.Path)}\" data-file=\"{encoder.Encode(folder.Path)}\" data-search=\"{encoder.Encode((folder.Project + " " + folder.Path).ToLowerInvariant())}\">");
      builder.AppendLine("<summary>");
      builder.AppendLine("<span class=\"summary-row\">");
      builder.AppendLine($"<input class=\"done-check\" type=\"checkbox\" aria-label=\"Done\" data-id=\"{id}\">");
      builder.AppendLine($"<span class=\"symbol-title\"><span class=\"kind\">EmptyFolder</span> {encoder.Encode(folder.Path)}</span>");
      builder.AppendLine("</span>");
      builder.AppendLine("</summary>");
      builder.AppendLine("<div class=\"meta\">");
      builder.AppendLine($"<div>Project</div><div>{encoder.Encode(folder.Project)}</div>");
      builder.AppendLine($"<div>Folder</div><div>{encoder.Encode(folder.Path)} <button class=\"ignore-folder\" type=\"button\">Ignore folder</button></div>");
      builder.AppendLine("<div>References</div><div>0</div>");
      builder.AppendLine($"<div>Reason</div><div class=\"reason\">{encoder.Encode(folder.Reason)}</div>");
      builder.AppendLine("</div></details>");
    }

    foreach (var duplicate in result.DuplicateTypes.OrderBy(f => f.FullName))
    {
      var id = BuildDuplicateTypeId(duplicate);
      var projectsValue = string.Join("|", duplicate.Occurrences.Select(o => o.Project).Distinct().Order());
      var filesValue = string.Join(" ", duplicate.Occurrences.Select(o => o.File));
      var folderValue = string.Join(" ", duplicate.Occurrences.Select(o => Path.GetDirectoryName(o.File) ?? string.Empty));
      builder.AppendLine(
        $"<details class=\"item duplicate-type\" data-id=\"{id}\" data-order=\"{order++}\" data-project=\"{encoder.Encode(projectsValue)}\" data-kind=\"DuplicateType\" data-references=\"0\" data-folder=\"{encoder.Encode(folderValue)}\" data-file=\"{encoder.Encode(filesValue)}\" data-search=\"{encoder.Encode((duplicate.FullName + " " + duplicate.Namespace + " " + filesValue).ToLowerInvariant())}\">");
      builder.AppendLine("<summary>");
      builder.AppendLine("<span class=\"summary-row\">");
      builder.AppendLine($"<input class=\"done-check\" type=\"checkbox\" aria-label=\"Done\" data-id=\"{id}\">");
      builder.AppendLine($"<span class=\"symbol-title\"><span class=\"kind\">DuplicateType</span> {encoder.Encode(duplicate.FullName)}</span>");
      builder.AppendLine("</span>");
      builder.AppendLine("</summary>");
      builder.AppendLine("<div class=\"meta\">");
      builder.AppendLine($"<div>Kind</div><div>{duplicate.Kind}</div>");
      builder.AppendLine($"<div>Namespace</div><div>{encoder.Encode(duplicate.Namespace)}</div>");
      builder.AppendLine($"<div>Occurrences</div><div>{duplicate.Occurrences.Count}</div>");
      builder.AppendLine($"<div>Reason</div><div class=\"reason\">{encoder.Encode(duplicate.Reason)}</div>");
      builder.AppendLine("<div>Files</div><div>");
      foreach (var occurrence in duplicate.Occurrences)
      {
        var occurrenceFolder = Path.GetDirectoryName(occurrence.File) ?? string.Empty;
        builder.AppendLine($"<div>{encoder.Encode(occurrence.Project)}: {encoder.Encode(occurrence.File)}:{occurrence.Line} <button class=\"ignore-folder\" type=\"button\" data-folder=\"{encoder.Encode(occurrenceFolder)}\">Ignore folder</button></div>");
      }

      builder.AppendLine("</div>");
      builder.AppendLine("</div></details>");
    }

    builder.AppendLine("</section>");
    builder.AppendLine($"<pre class=\"stats\">{encoder.Encode(BuildStatistics(result))}</pre>");
    builder.AppendLine("<script>");
    builder.AppendLine("const project=document.getElementById('project'),kind=document.getElementById('kind'),search=document.getElementById('search'),items=document.getElementById('items'),ignoredFolderInput=document.getElementById('ignoredFolderInput'),ignoredFolderList=document.getElementById('ignoredFolderList'),storePrefix='unused-code-done:',ignoredKey='unused-code-ignored-folders';let sortByRefs=false;function readIgnored(){const raw=localStorage.getItem(ignoredKey)||'';if(!raw)return[];try{const parsed=JSON.parse(raw);if(Array.isArray(parsed))return parsed.map(x=>({path:String(x.path||'').trim(),enabled:x.enabled!==false})).filter(x=>x.path)}catch{}return raw.split(';').map(x=>x.trim()).filter(Boolean).map(path=>({path,enabled:true}))}function writeIgnored(values){const normalized=[];for(const value of values){const path=String(value.path||value).trim();if(path&&!normalized.some(x=>x.path.toLowerCase()===path.toLowerCase()))normalized.push({path,enabled:value.enabled!==false})}localStorage.setItem(ignoredKey,JSON.stringify(normalized));renderIgnored();apply()}function renderIgnored(){ignoredFolderList.innerHTML='';for(const rule of readIgnored()){const item=document.createElement('label');item.className='ignore-rule'+(rule.enabled?'':' disabled');const check=document.createElement('input');check.type='checkbox';check.checked=rule.enabled;check.onchange=()=>{writeIgnored(readIgnored().map(x=>x.path===rule.path?{path:x.path,enabled:check.checked}:x))};const text=document.createElement('span');text.textContent=rule.path;const remove=document.createElement('button');remove.type='button';remove.textContent='Remove';remove.onclick=event=>{event.preventDefault();writeIgnored(readIgnored().filter(x=>x.path!==rule.path))};item.append(check,text,remove);ignoredFolderList.appendChild(item)}}function addIgnored(path){if(path)writeIgnored([...readIgnored(),{path,enabled:true}]);ignoredFolderInput.value=''}function activeIgnored(){return readIgnored().filter(x=>x.enabled).map(x=>x.path.toLowerCase())}function isIgnored(item){const file=(item.dataset.file||'').toLowerCase(),folder=(item.dataset.folder||'').toLowerCase();return activeIgnored().some(x=>file.includes(x)||folder.includes(x))}function isDone(item){return localStorage.getItem(storePrefix+item.dataset.id)==='1'}function syncDone(item){const done=isDone(item);item.classList.toggle('done',done);const check=item.querySelector('.done-check');if(check)check.checked=done}function matchesProject(item,p){return !p||item.dataset.project.split('|').includes(p)}function reorder(){[...items.children].sort((a,b)=>{const done=Number(isDone(a))-Number(isDone(b));if(done!==0)return done;if(sortByRefs){const refs=Number(a.dataset.references)-Number(b.dataset.references);if(refs!==0)return refs}return Number(a.dataset.order)-Number(b.dataset.order)}).forEach(x=>items.appendChild(x))}function apply(){const p=project.value,k=kind.value,q=search.value.toLowerCase();for(const item of items.children){syncDone(item);const ok=matchesProject(item,p)&&(!k||item.dataset.kind===k)&&(!q||item.dataset.search.includes(q))&&!isIgnored(item);item.classList.toggle('hidden',!ok)}reorder()}for(const check of document.querySelectorAll('.done-check')){check.addEventListener('click',event=>event.stopPropagation());check.addEventListener('change',event=>{const id=event.target.dataset.id;if(event.target.checked)localStorage.setItem(storePrefix+id,'1');else localStorage.removeItem(storePrefix+id);apply()})}for(const button of document.querySelectorAll('.ignore-folder')){button.addEventListener('click',event=>{event.preventDefault();event.stopPropagation();const folder=event.target.dataset.folder||event.target.closest('.item').dataset.folder;if(folder)addIgnored(folder)})}ignoredFolderInput.addEventListener('keydown',event=>{if(event.key==='Enter'){event.preventDefault();addIgnored(ignoredFolderInput.value)}});document.getElementById('addIgnoredFolder').onclick=()=>addIgnored(ignoredFolderInput.value);document.getElementById('clearIgnoredFolders').onclick=()=>writeIgnored([]);project.onchange=kind.onchange=search.oninput=apply;document.getElementById('sort').onclick=()=>{sortByRefs=!sortByRefs;apply()};renderIgnored();apply();");
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
      $"Unused constructors: {GetCount(result, UnusedSymbolKind.Constructor)}",
      $"Unused properties: {GetCount(result, UnusedSymbolKind.Property)}",
      $"Unused fields: {GetCount(result, UnusedSymbolKind.Field)}",
      $"Unused interfaces: {GetCount(result, UnusedSymbolKind.Interface)}",
      $"Unused enums: {GetCount(result, UnusedSymbolKind.Enum)}",
      $"Unused events: {GetCount(result, UnusedSymbolKind.Event)}",
      $"Empty folders: {result.EmptyFolders.Count}",
      $"Duplicate types: {result.DuplicateTypes.Count}",
      $"Total: {result.Findings.Count + result.EmptyFolders.Count + result.DuplicateTypes.Count}",
      "=================================="
    });
  }

  private static int GetCount(UnusedCodeAnalysisResult result, UnusedSymbolKind kind)
  {
    return result.Counts.TryGetValue(kind, out var count) ? count : 0;
  }

  private static string BuildFindingId(UnusedCodeFinding finding)
  {
    var source = $"{finding.Project}|{finding.Kind}|{finding.FullName}|{finding.File}|{finding.Line}";
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(source));
    return Convert.ToHexString(bytes);
  }

  private static string BuildFolderId(EmptyFolderFinding finding)
  {
    var source = $"{finding.Project}|EmptyFolder|{finding.Path}";
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(source));
    return Convert.ToHexString(bytes);
  }

  private static string BuildDuplicateTypeId(DuplicateTypeFinding finding)
  {
    var source = $"{finding.Kind}|DuplicateType|{finding.FullName}|{string.Join("|", finding.Occurrences.Select(o => o.File))}";
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(source));
    return Convert.ToHexString(bytes);
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
      emptyFoldersCount = result.EmptyFolders.Count,
      duplicateTypesCount = result.DuplicateTypes.Count,
      total = result.Findings.Count + result.EmptyFolders.Count + result.DuplicateTypes.Count,
      findings = result.Findings
        .OrderBy(f => f.Project)
        .ThenBy(f => f.Namespace)
        .ThenBy(f => f.Kind)
        .ThenBy(f => f.FullName),
      emptyFolders = result.EmptyFolders
        .OrderBy(f => f.Project)
        .ThenBy(f => f.Path),
      duplicateTypes = result.DuplicateTypes
        .OrderBy(f => f.FullName)
    };

    var options = new JsonSerializerOptions
    {
      WriteIndented = true
    };

    await using var stream = File.Create(filePath);
    await JsonSerializer.SerializeAsync(stream, payload, options, cancellationToken).ConfigureAwait(false);
  }
}

