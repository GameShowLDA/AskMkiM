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
  public void Completion_FiltersDateAndPreservesLatestContent()
  {
    var today = new DateTime(2026, 10, 5);
    var note = new CalendarNote { Id = Guid.NewGuid(), Date = today, Text = "Первый текст" };
    Service.Save(note);
    Service.Save(new CalendarNote { Id = Guid.NewGuid(), Date = today.AddDays(1), Text = "Завтра" });
    Service.Save(note with { Text = "Обновлено" });
    Service.SetCompleted(note.Id, true);
    Assert.Empty(Service.LoadPending(today));
    var completed = Service.Load().Single(item => item.Id == note.Id);
    Assert.True(completed.IsCompleted);
    Assert.Equal("Обновлено", completed.Text);
    Service.Save(completed with { Title = "Редактирование" });
    Assert.True(Service.Load().Single(item => item.Id == note.Id).IsCompleted);
    Service.SetCompleted(note.Id, false);
    Assert.Equal(note.Id, Assert.Single(Service.LoadPending(today.AddHours(12))).Id);
    Assert.Single(Service.LoadPending(today.AddDays(1)));
  }

  [Fact]
  public void LegacyNote_WithoutCompletion_IsPending()
  {
    Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
    var id = Guid.NewGuid();
    File.WriteAllText(FilePath, JsonSerializer.Serialize(new[] { new { Id = id, Date = new DateTime(2026, 10, 5), Title = "Старая", Text = "Запись" } }));
    Assert.False(Assert.Single(Service.LoadPending(new DateTime(2026, 10, 5))).IsCompleted);
    Service.SetCompleted(Guid.NewGuid(), true);
    Assert.Equal(id, Assert.Single(Service.Load()).Id);
  }

  [Fact]
  public void DeleteAndRestore_ReturnsLatestSnapshotAndPreservesOtherRecords()
  {
    var note = new CalendarNote { Id = Guid.NewGuid(), Date = new DateTime(2026, 10, 5), Title = "Заголовок", Text = "Исходный" };
    var other = note with { Id = Guid.NewGuid(), Text = "Другая" };
    Service.Save(note);
    Service.Save(other);
    var latest = note with { Text = "Изменён другим окном", IsCompleted = true };
    Service.Save(latest);
    var deleted = Service.DeleteAndGet(note.Id);
    Assert.Equal(latest, deleted);
    Assert.Equal(other.Id, Assert.Single(Service.Load()).Id);
    Assert.Null(Service.DeleteAndGet(note.Id));
    Assert.True(Service.Restore(deleted!));
    Assert.Equal(latest, Service.Load().Single(item => item.Id == note.Id));
    Assert.Equal(2, Service.Load().Count);
  }

  [Fact]
  public void Restore_DoesNotOverwriteExistingRecordOrCorruptFile()
  {
    var note = new CalendarNote { Id = Guid.NewGuid(), Date = DateTime.Today, Text = "Удалённая" };
    Service.Save(note);
    var deleted = Service.DeleteAndGet(note.Id)!;
    var newer = note with { Text = "Новая запись", Date = note.Date.AddDays(1) };
    Service.Save(newer);
    Assert.False(Service.Restore(deleted));
    Assert.Equal(newer, Assert.Single(Service.Load()));
    File.WriteAllText(FilePath, "broken json");
    Assert.Throws<JsonException>(() => Service.Restore(deleted));
    Assert.Equal("broken json", File.ReadAllText(FilePath));
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
