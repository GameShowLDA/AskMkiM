using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Ask.Core.Services.Calendar;

/// <summary>Хранит общие заметки установки с атомарной заменой файла.</summary>
public sealed class CalendarNoteService
{
  private readonly string _path;
  private readonly string _mutexName;

  /// <summary>Создаёт хранилище в каталоге Settings приложения.</summary>
  public CalendarNoteService() : this(AppContext.BaseDirectory) { }

  /// <summary>Создаёт хранилище для заданного каталога приложения.</summary>
  /// <param name="applicationDirectory">Каталог приложения, содержащий общие настройки.</param>
  public CalendarNoteService(string applicationDirectory)
  {
    _path = Path.Combine(Path.GetFullPath(applicationDirectory), "Settings", "calendarNotes.json");
    _mutexName = "AskMkiM.CalendarNotes." + Convert.ToHexString(
      SHA256.HashData(Encoding.UTF8.GetBytes(_path.ToUpperInvariant())));
  }

  /// <summary>Загружает все заметки, не создавая отсутствующий файл.</summary>
  /// <returns>Заметки установки.</returns>
  public IReadOnlyList<CalendarNote> Load() => WithLock(Read);

  /// <summary>Создаёт или изменяет заметку, сохраняя остальные записи.</summary>
  /// <param name="note">Заметка с непустым текстом и датой.</param>
  public void Save(CalendarNote note)
  {
    ArgumentNullException.ThrowIfNull(note);
    if (note.Id == Guid.Empty || string.IsNullOrWhiteSpace(note.Text))
      throw new ArgumentException("Укажите текст заметки и её идентификатор.", nameof(note));
    WithLock(() =>
    {
      var notes = Read();
      notes.RemoveAll(item => item.Id == note.Id);
      notes.Add(note with { Date = note.Date.Date, Title = note.Title.Trim(), Text = note.Text.Trim() });
      Write(notes);
      return true;
    });
  }

  /// <summary>Удаляет заметку по идентификатору.</summary>
  /// <param name="id">Идентификатор удаляемой заметки.</param>
  public void Delete(Guid id) => WithLock(() =>
  {
    var notes = Read();
    if (notes.RemoveAll(item => item.Id == id) > 0) Write(notes);
    return true;
  });

  private List<CalendarNote> Read()
  {
    if (!File.Exists(_path)) return new();
    var notes = JsonSerializer.Deserialize<List<CalendarNote>>(File.ReadAllText(_path))
      ?? throw new InvalidDataException("Файл заметок не содержит списка записей.");
    if (notes.Any(note => note is null || note.Id == Guid.Empty || note.Title is null ||
        string.IsNullOrWhiteSpace(note.Text)) || notes.Select(note => note.Id).Distinct().Count() != notes.Count)
      throw new InvalidDataException("Файл заметок содержит некорректные записи.");
    return notes;
  }

  private void Write(List<CalendarNote> notes)
  {
    Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
    string temporary = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
    try
    {
      File.WriteAllText(temporary, JsonSerializer.Serialize(notes, new JsonSerializerOptions { WriteIndented = true }));
      File.Move(temporary, _path, overwrite: true);
    }
    finally
    {
      if (File.Exists(temporary)) File.Delete(temporary);
    }
  }

  private T WithLock<T>(Func<T> action)
  {
    using var mutex = new Mutex(false, _mutexName);
    bool acquired;
    try { acquired = mutex.WaitOne(TimeSpan.FromSeconds(5)); }
    catch (AbandonedMutexException) { acquired = true; }
    if (!acquired) throw new IOException("Хранилище заметок занято. Повторите операцию.");
    try { return action(); }
    finally { mutex.ReleaseMutex(); }
  }
}
