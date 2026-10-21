using Ask.Support;
using Ask.Support.ReleaseNotes;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;

namespace Ask.UI.UnitTests.Services.ReleaseNotes;

public sealed class ReleaseNotesHtmlTests
{
  [Fact]
  public void RendersVersionsNewestFirstAndEscapesContent()
  {
    var html = new ReleaseNotesHtmlRenderer().Render(new[] { Create("1.9.0"), Create("1.10.0") });
    Assert.True(html.IndexOf("release-1.10.0", StringComparison.Ordinal)
      < html.IndexOf("release-1.9.0", StringComparison.Ordinal));
    Assert.Contains("id=\"release-1.10.0\" open", html);
    Assert.DoesNotContain("id=\"release-1.9.0\" open", html);
    Assert.Contains("&lt;script&gt;", html);
    Assert.DoesNotContain("<script>alert", html);
    Assert.Contains("data-nav=\"&quot; onclick=&quot;bad\"", html);
    Assert.Contains("Исправлено", html);
    Assert.DoesNotContain("<h3>Новое</h3>", html);
    Assert.Contains("21.10.2026", html);
  }

  [Fact]
  public void EmptyCatalogHasExplicitMessage()
  {
    Assert.Contains("История выпусков пока не заполнена", new ReleaseNotesHtmlRenderer().Render([]));
  }

  [Fact]
  public async Task HelpServerServesGeneratedHistoryAndNavigation()
  {
    try
    {
      HelpServer.EnsureStarted();
      Assert.NotNull(HelpServer.BaseUrl);
      using var client = new HttpClient { BaseAddress = HelpServer.BaseUrl };
      using var response = await client.GetAsync("ru/Content/ReleaseNotes.html");
      string html = await response.Content.ReadAsStringAsync();
      Assert.True(response.StatusCode == HttpStatusCode.OK,
        $"Страница истории вернула {response.StatusCode}. Проверьте поставку AppHelp/releases/*.json. Ответ: {html}");
      Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
      Assert.True(response.Headers.CacheControl?.NoStore);
      Assert.Contains("Версия 1.1.0", html);
      Assert.Contains("Сравнение файлов", html);
      string index = await client.GetStringAsync("ru/index.html");
      Assert.Contains("id=\"ReleaseNotes\" data-src=\"Content/ReleaseNotes.html\"", index);
      using var css = await client.GetAsync("content.css");
      Assert.Equal(HttpStatusCode.OK, css.StatusCode);
      using var script = await client.GetAsync("release-notes.js");
      Assert.Equal(HttpStatusCode.OK, script.StatusCode);
      using var wrongRole = await client.PostAsJsonAsync("api/release-notes/viewed", new
      {
        role = "!not-an-authenticated-role",
        version = "1.1.0"
      });
      Assert.Equal(HttpStatusCode.Conflict, wrongRole.StatusCode);
    }
    finally
    {
      HelpServer.Stop();
    }
  }

  private static ReleaseNotesModel Create(string version) => new()
  {
    SchemaVersion = 1,
    Version = version,
    ReleaseDate = new DateOnly(2026, 10, 21),
    Title = "<script>alert('title')</script>",
    Changes = new[]
    {
      new ReleaseChangeModel
      {
        Id = "test",
        Category = ReleaseChangeCategory.Fixed,
        Title = "<script>alert('change')</script>",
        Description = "Описание & <текст>",
        HelpKey = "\" onclick=\"bad"
      }
    }
  };
}
