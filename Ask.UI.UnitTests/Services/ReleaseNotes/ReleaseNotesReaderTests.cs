using Ask.Support.ReleaseNotes;
using System.IO;

namespace Ask.UI.UnitTests.Services.ReleaseNotes;

public sealed class ReleaseNotesReaderTests : IDisposable
{
  private readonly string _directory = Path.Combine(Path.GetTempPath(), $"AskMkiM-release-notes-{Guid.NewGuid():N}");

  public ReleaseNotesReaderTests() => Directory.CreateDirectory(_directory);

  [Fact]
  public async Task ReadsOptionalFieldsAndSortsVersionsNumerically()
  {
    Write("1.9.0");
    Write("1.10.0");
    var releases = await new ReleaseNotesReader().ReadDirectoryAsync(_directory);
    Assert.Equal(new[] { "1.10.0", "1.9.0" }, releases.Select(release => release.Version));
    Assert.Equal(new DateOnly(2026, 10, 21), releases[0].ReleaseDate);
    Assert.Null(releases[0].Summary);
    Assert.Null(releases[0].Changes[0].HelpKey);
    Assert.Equal(ReleaseChangeCategory.Added, releases[0].Changes[0].Category);
    Assert.Equal("Описание", releases[0].Changes[0].Description);
  }

  [Theory]
  [InlineData("\"schemaVersion\":1", "\"schemaVersion\":2")]
  [InlineData("\"category\":\"added\"", "\"category\":\"unknown\"")]
  [InlineData("\"category\":\"added\"", "\"category\":0")]
  [InlineData("\"description\":\"Описание\"", "\"description\":\" \"")]
  [InlineData("\"description\":\"Описание\"", "\"description\":null")]
  [InlineData("\"releaseDate\":\"2026-10-21\"", "\"releaseDate\":\"21.10.2026\"")]
  [InlineData("\"releaseDate\":\"2026-10-21\",", "")]
  [InlineData("\"version\":\"1.0.0\"", "\"version\":\"01.0.0\"")]
  [InlineData("\"version\":\"1.0.0\"", "\"version\":null")]
  [InlineData("\"schemaVersion\":1", "\"schemaVersion\":1,\"typo\":true")]
  public async Task RejectsInvalidFieldsWithSourcePath(string original, string replacement)
  {
    string path = Write("1.0.0");
    File.WriteAllText(path, File.ReadAllText(path).Replace(original, replacement));
    var exception = await Assert.ThrowsAsync<InvalidDataException>(() =>
      new ReleaseNotesReader().ReadDirectoryAsync(_directory));
    Assert.Contains(path, exception.Message);
  }

  [Theory]
  [InlineData("[]")]
  [InlineData("null")]
  [InlineData("[null]")]
  [InlineData("[{\"id\":\"same\",\"category\":\"fixed\",\"title\":\"A\",\"description\":\"B\"},"
    + "{\"id\":\"same\",\"category\":\"fixed\",\"title\":\"C\",\"description\":\"D\"}]")]
  public async Task RejectsInvalidChanges(string changes)
  {
    File.WriteAllText(Path.Combine(_directory, "1.0.0.json"),
      "{\"schemaVersion\":1,\"version\":\"1.0.0\",\"releaseDate\":\"2026-10-21\",\"changes\":" + changes + "}");
    await Assert.ThrowsAsync<InvalidDataException>(() => new ReleaseNotesReader().ReadDirectoryAsync(_directory));
  }

  [Fact]
  public async Task RejectsMismatchedFileName()
  {
    File.Move(Write("1.0.0"), Path.Combine(_directory, "2.0.0.json"));
    await Assert.ThrowsAsync<InvalidDataException>(() => new ReleaseNotesReader().ReadDirectoryAsync(_directory));
  }

  [Fact]
  public async Task EmptyDirectoryReturnsEmptyCatalog()
  {
    Assert.Empty(await new ReleaseNotesReader().ReadDirectoryAsync(_directory));
  }

  [Fact]
  public async Task CancellationPropagates()
  {
    Write("1.0.0");
    using var cancellation = new CancellationTokenSource();
    cancellation.Cancel();
    await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
      new ReleaseNotesReader().ReadDirectoryAsync(_directory, cancellation.Token));
  }

  private string Write(string version)
  {
    string path = Path.Combine(_directory, version + ".json");
    File.WriteAllText(path, $$"""
      {"schemaVersion":1,"version":"{{version}}","releaseDate":"2026-10-21",
       "changes":[{"id":"first","category":"added","title":"Заголовок","description":"Описание"}]}
      """);
    return path;
  }

  public void Dispose() => Directory.Delete(_directory, recursive: true);
}
