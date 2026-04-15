using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace UI.Controls.Calendar
{
  internal sealed class CalendarNoteStore
  {
    public static event EventHandler? NotesChanged;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
      WriteIndented = true,
    };

    private readonly string _filePath = Path.Combine(
      AppContext.BaseDirectory,
      "Settings",
      "calendar-notes.json");

    public Dictionary<DateTime, string> Load()
    {
      try
      {
        if (!File.Exists(_filePath))
        {
          return new Dictionary<DateTime, string>();
        }

        var json = File.ReadAllText(_filePath);
        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions);
        if (data == null)
        {
          return new Dictionary<DateTime, string>();
        }

        return data
          .Where(static pair => DateTime.TryParseExact(pair.Key, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
          .ToDictionary(
            pair => DateTime.ParseExact(pair.Key, "yyyy-MM-dd", CultureInfo.InvariantCulture).Date,
            pair => pair.Value);
      }
      catch
      {
        return new Dictionary<DateTime, string>();
      }
    }

    public void Save(Dictionary<DateTime, string> notes)
    {
      Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

      var data = notes.ToDictionary(
        pair => pair.Key.ToString("yyyy-MM-dd"),
        pair => pair.Value);

      var json = JsonSerializer.Serialize(data, JsonOptions);
      File.WriteAllText(_filePath, json);
      NotesChanged?.Invoke(this, EventArgs.Empty);
    }
  }
}
