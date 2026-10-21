using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Ask.Support.ReleaseNotes;

/// <summary>Сохраняет просмотренные версии отдельно для каждой роли текущего пользователя Windows.</summary>
public sealed class ReleaseNotesReadStateStore
{
  private readonly string _path;
  private readonly string _mutexName;

  /// <summary>Общее хранилище отметок просмотра приложения.</summary>
  public static ReleaseNotesReadStateStore Default { get; } = new(Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AskMkiM", "ReleaseNotes", "state.json"));

  /// <summary>Возникает после сохранения или сброса отметки просмотра.</summary>
  public event Action? Changed;

  /// <summary>Создаёт хранилище с заданным путём файла состояния.</summary>
  /// <param name="filePath">Полный путь к JSON-файлу отметок просмотра.</param>
  public ReleaseNotesReadStateStore(string filePath)
  {
    _path = Path.GetFullPath(filePath);
    _mutexName = "AskMkiM.ReleaseNotes." + Convert.ToHexString(
      SHA256.HashData(Encoding.UTF8.GetBytes(_path.ToUpperInvariant())));
  }

  /// <summary>Проверяет просмотр версии выбранной ролью.</summary>
  /// <param name="role">Стабильное имя роли приложения.</param>
  /// <param name="version">Номер выпуска.</param>
  /// <returns>Признак ранее сохранённого просмотра.</returns>
  public bool HasViewed(string role, string version) => WithLock(() =>
    Read().TryGetValue(role, out var versions) && versions.Contains(version));

  /// <summary>Атомарно сохраняет просмотр версии, сохраняя отметки других ролей и выпусков.</summary>
  /// <param name="role">Стабильное имя роли приложения.</param>
  /// <param name="version">Номер выпуска.</param>
  public void MarkViewed(string role, string version)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(role);
    ArgumentException.ThrowIfNullOrWhiteSpace(version);
    bool changed = WithLock(() =>
    {
      var state = Read();
      if (!state.TryGetValue(role, out var versions)) state[role] = versions = [];
      if (!versions.Add(version)) return false;
      Write(state);
      return true;
    });
    if (changed) Changed?.Invoke();
  }

  /// <summary>Сбрасывает просмотр одной версии выбранной ролью, сохраняя остальные отметки.</summary>
  /// <param name="role">Стабильное имя роли приложения.</param>
  /// <param name="version">Номер выпуска для повторного просмотра.</param>
  public void ResetViewed(string role, string version)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(role);
    ArgumentException.ThrowIfNullOrWhiteSpace(version);
    bool changed = WithLock(() =>
    {
      var state = Read();
      if (!state.TryGetValue(role, out var versions) || !versions.Remove(version)) return false;
      if (versions.Count == 0) state.Remove(role);
      Write(state);
      return true;
    });
    if (changed) Changed?.Invoke();
  }

  private void Write(Dictionary<string, HashSet<string>> state)
  {
    Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
    string temporaryPath = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
    try
    {
      File.WriteAllText(temporaryPath, JsonSerializer.Serialize(state));
      File.Move(temporaryPath, _path, overwrite: true);
    }
    finally
    {
      if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
    }
  }

  private Dictionary<string, HashSet<string>> Read()
  {
    if (!File.Exists(_path)) return [];
    var state = JsonSerializer.Deserialize<Dictionary<string, HashSet<string>>>(File.ReadAllText(_path));
    if (state is null || state.Any(pair => pair.Value is null))
      throw new InvalidDataException("Некорректный файл отметок просмотра истории выпусков.");
    return state;
  }

  private T WithLock<T>(Func<T> action)
  {
    using var mutex = new Mutex(false, _mutexName);
    bool acquired;
    try { acquired = mutex.WaitOne(TimeSpan.FromSeconds(5)); }
    catch (AbandonedMutexException) { acquired = true; }
    if (!acquired) throw new IOException("История просмотра занята другим процессом.");
    try { return action(); }
    finally { mutex.ReleaseMutex(); }
  }
}
