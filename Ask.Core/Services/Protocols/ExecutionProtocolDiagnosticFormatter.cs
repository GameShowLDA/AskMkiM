using Ask.Core.Shared.DTO.Protocol;
using System.Text;
using System.Text.Json;

namespace Ask.Core.Services.Protocols;

/// <summary>
/// Сохраняет и отображает диагностические сведения записей протокола выполнения.
/// </summary>
public static class ExecutionProtocolDiagnosticFormatter
{
  private const string Marker = "#ASKM_DEBUG_V1#";
  private const string MessageMarker = "#ASKM_MESSAGE_V2#";
  private const string EnvironmentMarker = "#ASKM_ENV_V1#";
  private const string StatusMarker = "\u2063";
  private const string RootDebugMarker = "\u2063\u2063";
  private const string TimeMarker = "\u2063\u2062";

  /// <summary>
  /// Формирует скрытый диагностический снимок окружения выполнения.
  /// </summary>
  public static string FormatEnvironmentForStorage(ExecutionProtocolEnvironmentSnapshot snapshot)
  {
    ArgumentNullException.ThrowIfNull(snapshot);
    string json = JsonSerializer.Serialize(snapshot);
    return EnvironmentMarker + Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
  }

  /// <summary>
  /// Формирует пользовательскую строку и скрытую диагностическую запись для сохранения.
  /// </summary>
  public static IEnumerable<string> FormatForStorage(ShowMessageModel message)
  {
    ArgumentNullException.ThrowIfNull(message);

    string formattedLine = ExecutionProtocolLineFormatter.Format(message);
    yield return formattedLine;

    var snapshot = ExecutionProtocolMessageSnapshot.FromModel(message);
    string snapshotJson = JsonSerializer.Serialize(snapshot);
    yield return MessageMarker + Convert.ToBase64String(Encoding.UTF8.GetBytes(snapshotJson));

    int indentLength = Math.Max(0, message.IndentLevel) * 2;
    int? messageStart = !string.IsNullOrWhiteSpace(message.Message)
      ? indentLength + (!string.IsNullOrWhiteSpace(message.Header) ? message.Header.Length + 2 : 0)
      : null;
    int? timeStart = !string.IsNullOrWhiteSpace(message.Time)
      ? formattedLine.LastIndexOf(" | ", StringComparison.Ordinal)
      : null;

    var diagnostic = new DiagnosticEntry(
      message.DiagnosticSource ?? message.Debug ?? string.Empty,
      message.Status?.ToString() ?? string.Empty,
      message.IsDeviceMessage,
      message.ExecutionError,
      message.ExecutionErrorMessage,
      message.CanBeDeleted,
      message.IsControlProgramCommandHeader,
      message.IsStepModeCheckpoint,
      message.CommandExecutionHasErrors,
      message.IndentLevel,
      !string.IsNullOrWhiteSpace(message.Header),
      !string.IsNullOrWhiteSpace(message.Message),
      message.UseSuccessColorForEntireMessage,
      messageStart,
      timeStart);

    string json = JsonSerializer.Serialize(diagnostic);
    yield return Marker + Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
  }

  /// <summary>
  /// Удаляет служебные записи либо раскрывает их в читаемом виде для root.
  /// </summary>
  public static string PrepareForDisplay(string text, bool includeDiagnostics)
  {
    ArgumentNullException.ThrowIfNull(text);

    var result = new List<string>();
    string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

    foreach (string line in lines)
    {
      if (line.StartsWith(EnvironmentMarker, StringComparison.Ordinal))
      {
        if (includeDiagnostics && TryDecodeEnvironment(line, out var environment))
        {
          result.AddRange(FormatEnvironment(environment));
        }
        continue;
      }

      if (line.StartsWith(MessageMarker, StringComparison.Ordinal))
        continue;

      if (!line.StartsWith(Marker, StringComparison.Ordinal))
      {
        result.Add(line);
        continue;
      }

      if (TryDecode(line, out var diagnostic))
      {
        if (result.Count > 0)
        {
          result[^1] = AddStatusMarkers(result[^1], diagnostic);
        }
        if (includeDiagnostics)
        {
          result.Add(FormatDiagnostic(diagnostic));
        }
      }
    }

    return string.Join("\n", result);
  }

  /// <summary>
  /// Восстанавливает сообщения структурированного протокола выполнения.
  /// </summary>
  public static bool TryRestoreMessages(
    string text,
    bool includeDiagnostics,
    out IReadOnlyList<ShowMessageModel> messages)
  {
    ArgumentNullException.ThrowIfNull(text);
    var restored = new List<ShowMessageModel>();

    foreach (string line in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
    {
      if (includeDiagnostics
          && line.StartsWith(EnvironmentMarker, StringComparison.Ordinal)
          && TryDecodeEnvironment(line, out var environment))
      {
        restored.Add(new ShowMessageModel
        {
          Debug = string.Join(Environment.NewLine, FormatEnvironment(environment)),
          HeaderColor = System.Windows.Media.Colors.Transparent,
          MessageColor = System.Windows.Media.Colors.Transparent
        });
        continue;
      }

      if (!line.StartsWith(MessageMarker, StringComparison.Ordinal))
        continue;

      try
      {
        byte[] json = Convert.FromBase64String(line[MessageMarker.Length..]);
        var snapshot = JsonSerializer.Deserialize<ExecutionProtocolMessageSnapshot>(json);
        if (snapshot != null)
          restored.Add(snapshot.ToModel(includeDiagnostics));
      }
      catch (FormatException)
      {
        messages = Array.Empty<ShowMessageModel>();
        return false;
      }
      catch (JsonException)
      {
        messages = Array.Empty<ShowMessageModel>();
        return false;
      }
    }

    messages = restored;
    return restored.Count > 0;
  }

  private static string AddStatusMarkers(string line, DiagnosticEntry entry)
  {
    string marker = entry.Status switch
    {
      nameof(ShowMessageModel.MessageType.Command) => StatusMarker + "\u200B",
      nameof(ShowMessageModel.MessageType.CommandBlock) => StatusMarker + "\u200C",
      nameof(ShowMessageModel.MessageType.Success) => StatusMarker + "\u200D",
      nameof(ShowMessageModel.MessageType.Error) => StatusMarker + "\u2060",
      _ => StatusMarker + "\uFEFF"
    };

    if (entry.Status == nameof(ShowMessageModel.MessageType.Info))
      return InsertTimeMarker(line);

    if (entry.UseSuccessColorForEntireMessage)
      return marker + line;

    if (entry.Status == nameof(ShowMessageModel.MessageType.Command)
        || !entry.HasHeader)
      return marker + InsertTimeMarker(line);

    if (!entry.HasMessage)
      return InsertTimeMarker(line);

    int bodyIndex = entry.MessageStart ?? line.IndexOf(": ", StringComparison.Ordinal) + 2;
    return bodyIndex <= 1 || bodyIndex > line.Length
      ? marker + InsertTimeMarker(line)
      : InsertTimeMarker(line, entry.TimeStart).Insert(bodyIndex, marker);
  }

  private static string InsertTimeMarker(string line, int? storedTimeStart = null)
  {
    int timeStart = storedTimeStart ?? line.LastIndexOf(" | ", StringComparison.Ordinal);
    if (timeStart < 0 || timeStart > line.Length)
      return line;

    return line.Insert(timeStart, TimeMarker);
  }

  private static bool TryDecodeEnvironment(
    string line,
    out ExecutionProtocolEnvironmentSnapshot snapshot)
  {
    try
    {
      byte[] json = Convert.FromBase64String(line[EnvironmentMarker.Length..]);
      snapshot = JsonSerializer.Deserialize<ExecutionProtocolEnvironmentSnapshot>(json)!;
      return snapshot != null;
    }
    catch (Exception) when (line.StartsWith(EnvironmentMarker, StringComparison.Ordinal))
    {
      snapshot = null!;
      return false;
    }
  }

  private static IEnumerable<string> FormatEnvironment(ExecutionProtocolEnvironmentSnapshot snapshot)
  {
    yield return "================ ДИАГНОСТИКА ROOT ================";
    yield return $"Снимок: {snapshot.CapturedAt:yyyy-MM-dd HH:mm:ss.fff zzz}";
    yield return $"Версия приложения: {snapshot.ApplicationVersion}";
    yield return $"Роль при выполнении: {snapshot.Role}";
    yield return $"Процесс: {snapshot.ProcessName}; тип={snapshot.CheckType}; режим={snapshot.Mode}";
    yield return "Настройки:";
    foreach (var setting in snapshot.Settings)
      yield return $"  {setting.Key}: {setting.Value}";

    yield return $"Использованное оборудование ({snapshot.Equipment.Count}):";
    foreach (var device in snapshot.Equipment)
    {
      yield return $"  [{device.Type}] {device.Name}; Id={device.Id}; №={device.Number}; "
        + $"подключение={device.ConnectionDetails}; состояние={device.ConnectionState}";
      yield return $"    runtime={device.RuntimeClass}; configured={device.ConfiguredClass}; "
        + $"описание={device.Description}";
    }
    yield return "====================================================";
    yield return string.Empty;
  }

  private static bool TryDecode(string line, out DiagnosticEntry diagnostic)
  {
    try
    {
      byte[] json = Convert.FromBase64String(line[Marker.Length..]);
      diagnostic = JsonSerializer.Deserialize<DiagnosticEntry>(json)!;
      return diagnostic != null;
    }
    catch (Exception) when (line.StartsWith(Marker, StringComparison.Ordinal))
    {
      diagnostic = null!;
      return false;
    }
  }

  private static string FormatDiagnostic(DiagnosticEntry entry)
  {
    var attributes = new List<string>();
    Add(attributes, "тип", entry.Status);
    if (entry.IsDeviceMessage) attributes.Add("оборудование");
    if (entry.ExecutionError) attributes.Add("ошибка выполнения");
    if (entry.CanBeDeleted) attributes.Add("сокращаемая запись");
    if (entry.IsControlProgramCommandHeader) attributes.Add("заголовок команды ПК");
    if (entry.IsStepModeCheckpoint) attributes.Add("контрольная точка шага");
    if (entry.CommandExecutionHasErrors.HasValue)
      attributes.Add($"результат команды={(entry.CommandExecutionHasErrors.Value ? "БРАК" : "НОРМА")}");
    attributes.Add($"отступ={entry.IndentLevel}");
    Add(attributes, "ошибка для заключения", entry.ExecutionErrorMessage);

    string source = string.IsNullOrWhiteSpace(entry.Source) ? "источник не определён" : entry.Source;
    return $"{RootDebugMarker}    ↳ [ОТЛАДКА ROOT] {source}; {string.Join("; ", attributes)}";
  }

  private static void Add(ICollection<string> attributes, string name, string? value)
  {
    if (!string.IsNullOrWhiteSpace(value))
      attributes.Add($"{name}={value}");
  }

  private sealed record DiagnosticEntry(
    string Source,
    string Status,
    bool IsDeviceMessage,
    bool ExecutionError,
    string? ExecutionErrorMessage,
    bool CanBeDeleted,
    bool IsControlProgramCommandHeader,
    bool IsStepModeCheckpoint,
    bool? CommandExecutionHasErrors,
    int IndentLevel,
    bool HasHeader,
    bool HasMessage,
    bool UseSuccessColorForEntireMessage,
    int? MessageStart,
    int? TimeStart);
}
