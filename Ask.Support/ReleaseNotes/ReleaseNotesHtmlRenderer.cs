using System.Globalization;
using System.Net;
using System.Text;

namespace Ask.Support.ReleaseNotes;

/// <summary>
/// Формирует страницу истории выпусков для существующего окна справки.
/// </summary>
public sealed class ReleaseNotesHtmlRenderer
{
  /// <summary>Ключ раздела в дереве справки.</summary>
  public const string HelpPageKey = "ReleaseNotes";

  /// <summary>
  /// Формирует HTML с экранированным содержимым выпусков и группировкой изменений.
  /// </summary>
  /// <param name="releases">Проверенные описания выпусков.</param>
  /// <param name="role">Роль, открывшая страницу; null отключает регистрацию просмотра.</param>
  /// <returns>Полная HTML-страница; для пустого каталога — сообщение об отсутствии выпусков.</returns>
  public string Render(IReadOnlyList<ReleaseNotesModel> releases, string? role = null)
  {
    ArgumentNullException.ThrowIfNull(releases);
    var html = new StringBuilder("""
      <!DOCTYPE html>
      <html lang="ru"><head><meta charset="utf-8">
      <meta name="viewport" content="width=device-width, initial-scale=1">
      <title>Что нового</title><link rel="stylesheet" href="../../content.css">
      <style>
      .releases{max-width:1000px;margin:0 auto;padding:0 24px 32px}
      .release{border:1px solid #d1d5db;border-radius:10px;margin:16px 0;overflow:hidden}
      .release>summary{cursor:pointer;padding:16px;background:#f3f4f6;font-size:1.1rem}
      .release>summary:focus-visible{outline:2px solid #2563eb;outline-offset:-3px}
      .release time{font-size:.9rem;margin-left:12px;color:#4b5563}
      .release-body{padding:0 20px 16px}
      .release-description{white-space:pre-wrap}
      body.dark-theme .release{border-color:#475569}
      body.dark-theme .release>summary{background:#1f2937}
      body.dark-theme .release time{color:#cbd5e1}
      body.dark-theme .release a{color:#93c5fd}
      </style><script src="../../internal-links.js" defer></script></head>
      """);
    html.Append("<body data-role=\"").Append(Encode(role ?? string.Empty))
      .Append("\"><main class=\"page-content releases\"><h1>Что нового</h1>")
      .Append("<p>История изменений по версиям приложения. Выберите выпуск, чтобы раскрыть подробности.</p>");
    var ordered = releases.OrderByDescending(static release => Version.Parse(release.Version)).ToArray();
    if (ordered.Length == 0) html.Append("<p>История выпусков пока не заполнена.</p>");
    for (int index = 0; index < ordered.Length; index++)
    {
      var release = ordered[index];
      string date = release.ReleaseDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
      html.Append("<details class=\"release\" data-version=\"").Append(Encode(release.Version))
        .Append("\" id=\"release-").Append(Encode(release.Version)).Append('"');
      if (index == 0) html.Append(" open");
      html.Append("><summary><strong>Версия ").Append(Encode(release.Version))
        .Append("</strong><time datetime=\"").Append(date).Append("\">")
        .Append(release.ReleaseDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture))
        .Append("</time></summary><div class=\"release-body\">");
      if (!string.IsNullOrWhiteSpace(release.Title)) html.Append("<h2>").Append(Encode(release.Title)).Append("</h2>");
      if (!string.IsNullOrWhiteSpace(release.Summary)) html.Append("<p class=\"release-description\">")
        .Append(Encode(release.Summary)).Append("</p>");
      foreach (var category in Enum.GetValues<ReleaseChangeCategory>())
      {
        var changes = release.Changes.Where(change => change.Category == category).ToArray();
        if (changes.Length == 0) continue;
        html.Append("<section><h3>").Append(GetCategoryTitle(category)).Append("</h3><ul>");
        foreach (var change in changes)
        {
          html.Append("<li><strong>").Append(Encode(change.Title))
            .Append("</strong><p class=\"release-description\">").Append(Encode(change.Description)).Append("</p>");
          if (!string.IsNullOrWhiteSpace(change.HelpKey)) html.Append("<a href=\"#\" data-nav=\"")
            .Append(Encode(change.HelpKey)).Append("\">Открыть инструкцию</a>");
          html.Append("</li>");
        }
        html.Append("</ul></section>");
      }
      html.Append("</div></details>");
    }
    return html.Append("</main><script src=\"../../release-notes.js\"></script></body></html>").ToString();
  }

  private static string Encode(string text) => WebUtility.HtmlEncode(text);

  private static string GetCategoryTitle(ReleaseChangeCategory category) => category switch
  {
    ReleaseChangeCategory.Added => "Новое",
    ReleaseChangeCategory.Changed => "Изменено",
    ReleaseChangeCategory.Fixed => "Исправлено",
    ReleaseChangeCategory.Removed => "Удалено",
    ReleaseChangeCategory.Important => "Важно перед работой",
    ReleaseChangeCategory.KnownIssue => "Известные ограничения",
    _ => throw new ArgumentOutOfRangeException(nameof(category))
  };
}
