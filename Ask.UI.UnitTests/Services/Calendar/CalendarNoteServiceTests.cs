using Ask.Core.Services.Calendar;
using System.IO;
using System.Text.Json;

namespace Ask.UI.UnitTests.Services.Calendar;

public sealed class CalendarNoteServiceTests : IDisposable
{
  private readonly string _directory = Path.Combine(Path.GetTempPath(), "ask-calendar-" + Guid.NewGuid().ToString("N"));
  private CalendarNoteService Service => new(_directory);
  private string FilePath => Path.Combine(_directory, "Settings", "calendarNotes.json");

  [Fact]
  public void MissingFile_DoesNotCreateStorage()
  {
    Assert.Empty(Service.Load());
    Assert.False(Directory.Exists(_directory));
  }

  [Fact]
  public void CreateEditMoveAndDelete_PersistAcrossServiceInstances()
  {
    var first = new CalendarNote { Id = Guid.NewGuid(), Date = new DateTime(2026, 10, 3, 14, 5, 0), Title = " Проверка ", Text = " Кабель\nX2 " };
    var other = new CalendarNote { Id = Guid.NewGuid(), Date = first.Date, Text = "Вторая" };
    Service.Save(first);
    Service.Save(other);
    var stored = Service.Load().Single(note => note.Id == first.Id);
    Assert.Equal(first.Date.Date, stored.Date);
    Assert.Equal("Проверка", stored.Title);
    Assert.Equal("Кабель\nX2", stored.Text);
    Service.Save(stored with { Date = stored.Date.AddDays(1), Text = "Изменено" });
    Assert.Equal(2, Service.Load().Count);
    Assert.Equal(first.Date.Date.AddDays(1), Service.Load().Single(note => note.Id == first.Id).Date);
    Service.Delete(first.Id);
    Assert.Equal(other.Id, Assert.Single(Service.Load()).Id);
    Service.Delete(first.Id);
    Assert.Single(Service.Load());
    Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(FilePath)!, "*.tmp"));
  }

  [Fact]
  public void EmptyText_IsRejectedWithoutChangingStorage()
  {
    var note = new CalendarNote { Id = Guid.NewGuid(), Date = DateTime.Today, Text = "Запись" };
    Service.Save(note);
    string original = File.ReadAllText(FilePath);
    Assert.Throws<ArgumentException>(() => Service.Save(note with { Text = " \n " }));
    Assert.Equal(original, File.ReadAllText(FilePath));
  }

  [Fact]
  public void CorruptFile_IsNotOverwrittenBySaveOrDelete()
  {
    Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
    File.WriteAllText(FilePath, "broken json");
    Assert.Throws<JsonException>(() => Service.Load());
    Assert.Throws<JsonException>(() => Service.Save(new CalendarNote { Id = Guid.NewGuid(), Date = DateTime.Today, Text = "Запись" }));
    Assert.Throws<JsonException>(() => Service.Delete(Guid.NewGuid()));
    Assert.Equal("broken json", File.ReadAllText(FilePath));
  }

  [Fact]
  public void InvalidRecords_AreNotOverwritten()
  {
    Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
    File.WriteAllText(FilePath, "[null]");
    Assert.Throws<InvalidDataException>(() => Service.Load());
    Assert.Throws<InvalidDataException>(() => Service.Save(new CalendarNote { Id = Guid.NewGuid(), Date = DateTime.Today, Text = "Запись" }));
    Assert.Equal("[null]", File.ReadAllText(FilePath));
  }

  [Fact]
  public async Task ParallelWriters_PreserveAllNotes()
  {
    await Task.WhenAll(Enumerable.Range(0, 20).Select(i => Task.Run(() => Service.Save(
      new CalendarNote { Id = Guid.NewGuid(), Date = DateTime.Today, Text = i.ToString() }))));
    Assert.Equal(20, Service.Load().Count);
  }

  public void Dispose()
  {
    if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
  }
}
