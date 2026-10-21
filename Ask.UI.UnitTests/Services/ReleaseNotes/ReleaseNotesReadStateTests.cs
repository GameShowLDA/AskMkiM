using Ask.Support.ReleaseNotes;
using System.IO;
using System.Text.Json;

namespace Ask.UI.UnitTests.Services.ReleaseNotes;

public sealed class ReleaseNotesReadStateTests : IDisposable
{
  private readonly string _directory = Path.Combine(Path.GetTempPath(), "AskMkiM-read-state-" + Guid.NewGuid().ToString("N"));
  private string StatePath => Path.Combine(_directory, "state.json");

  [Fact]
  public void ViewIsPersistedAndIsolatedByRoleAndVersion()
  {
    var store = new ReleaseNotesReadStateStore(StatePath);
    Assert.False(store.HasViewed("Administrator", "1.1.0"));
    int notifications = 0;
    store.Changed += () => notifications++;
    store.MarkViewed("Administrator", "1.1.0");
    store.MarkViewed("Administrator", "1.1.0");
    var reloaded = new ReleaseNotesReadStateStore(StatePath);
    Assert.True(reloaded.HasViewed("Administrator", "1.1.0"));
    Assert.False(reloaded.HasViewed("Root", "1.1.0"));
    Assert.False(reloaded.HasViewed("Administrator", "1.2.0"));
    Assert.Equal(1, notifications);
    reloaded.MarkViewed("Root", "1.0.0");
    Assert.True(store.HasViewed("Administrator", "1.1.0"));
    Assert.True(store.HasViewed("Root", "1.0.0"));
  }

  [Fact]
  public async Task ConcurrentStoresPreserveBothMarks()
  {
    await Task.WhenAll(
      Task.Run(() => new ReleaseNotesReadStateStore(StatePath).MarkViewed("Root", "1.1.0")),
      Task.Run(() => new ReleaseNotesReadStateStore(StatePath).MarkViewed("Developer", "1.1.0")));
    var store = new ReleaseNotesReadStateStore(StatePath);
    Assert.True(store.HasViewed("Root", "1.1.0"));
    Assert.True(store.HasViewed("Developer", "1.1.0"));
  }

  [Fact]
  public void ResetPersistsAndPreservesOtherRolesAndVersions()
  {
    var store = new ReleaseNotesReadStateStore(StatePath);
    store.MarkViewed("Root", "1.1.0");
    store.MarkViewed("Root", "1.0.0");
    store.MarkViewed("Administrator", "1.1.0");
    int notifications = 0;
    store.Changed += () => notifications++;
    store.ResetViewed("Root", "1.1.0");
    store.ResetViewed("Root", "1.1.0");
    var reloaded = new ReleaseNotesReadStateStore(StatePath);
    Assert.False(reloaded.HasViewed("Root", "1.1.0"));
    Assert.True(reloaded.HasViewed("Root", "1.0.0"));
    Assert.True(reloaded.HasViewed("Administrator", "1.1.0"));
    Assert.Equal(1, notifications);
    store.MarkViewed("Root", "1.1.0");
    Assert.True(reloaded.HasViewed("Root", "1.1.0"));
  }

  [Fact]
  public void ResetAbsentStateDoesNotCreateFile()
  {
    new ReleaseNotesReadStateStore(StatePath).ResetViewed("Root", "1.1.0");
    Assert.False(File.Exists(StatePath));
  }

  [Fact]
  public void DamagedStateIsNotOverwritten()
  {
    Directory.CreateDirectory(_directory);
    File.WriteAllText(StatePath, "broken");
    Assert.Throws<JsonException>(() => new ReleaseNotesReadStateStore(StatePath).MarkViewed("Root", "1.1.0"));
    Assert.Equal("broken", File.ReadAllText(StatePath));
  }

  [Fact]
  public void RendererProvidesRoleSnapshotAndLoadedPageScript()
  {
    string html = new ReleaseNotesHtmlRenderer().Render([], "Administrator");
    Assert.Contains("data-role=\"Administrator\"", html);
    Assert.Contains("../../release-notes.js", html);
    Assert.DoesNotContain("/api/release-notes/viewed", html);
  }

  public void Dispose()
  {
    if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
  }
}
