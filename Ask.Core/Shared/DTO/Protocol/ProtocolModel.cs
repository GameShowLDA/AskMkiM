using System.Text;

using System.Text.RegularExpressions;

namespace Ask.Core.Shared.DTO.Protocol
{
  public class ProtocolModel
  {
    public enum ProtocolMessageKind
    {
      Error,
      Information
    }

    /// <summary>
    /// Дата протокола.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Обозначение сборочной единицы.
    /// </summary>
    public string Designation { get; set; }

    /// <summary>
    /// Наименование объекта контроля.
    /// </summary>
    public string ControlObjectName { get; set; }


    /// <summary>
    /// Номер сборочной единицы.
    /// </summary>
    public string Number { get; set; }

    /// <summary>
    /// Исполнитель.
    /// </summary>
    public string Executor { get; set; }

    /// <summary>
    /// Путь к программе контроля.
    /// </summary>
    public string ProgramPath { get; set; }

    /// <summary>
    /// Название программы контроля.
    /// </summary>
    public string ProgramName { get; set; }

    /// <summary>
    /// Представитель ОК.
    /// </summary>
    public string Agent { get; set; }

    /// <summary>
    /// Представитель заказчика(ВП).
    /// </summary>
    public string Customer { get; set; }

    /// <summary>
    /// Режим выполнения.
    /// </summary>
    public string Mode { get; set; }

    /// <summary>
    /// Единый список сообщений программы контроля по командам.
    /// </summary>
    public Dictionary<string, List<(ShowMessageModel Message, ProtocolMessageKind Kind)>> Messages { get; set; } = new();

    /// <summary>
    /// Время начала выполнения.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Время конца исполнения.
    /// </summary>
    public DateTime EndTime
    {
      get;
      set;
    }

    /// <summary>
    /// Общее время выполнения.
    /// </summary>
    public TimeSpan ExecutionTime
    {
      get
      {
        return EndTime - StartTime;
      }
    }

    private static string Template { get; set; } = string.Empty;
    private static string ErrorsTemplate { get; set; } = string.Empty;

    public int ErrorCount => Messages.Values.Sum(list => list.Count(item => item.Kind == ProtocolMessageKind.Error));

    public int InformationCount => Messages.Values.Sum(list => list.Count(item => item.Kind == ProtocolMessageKind.Information));

    public int TotalMessageCount => Messages.Values.Sum(list => list.Count);

    public bool HasErrors => ErrorCount > 0;

    public ProtocolModel()
    {
      StartTime = DateTime.Now;
    }

    static public void SetTemplate(string templatePath)
    {
      Template = templatePath;
    }

    static public void SetErrorsTemplate(string templatePath)
    {
      ErrorsTemplate = templatePath;
    }


    static public string GetProtocolText(ProtocolModel protocolModel)
    {
      string timeText = protocolModel.ExecutionTime.ToString(@"hh\:mm\:ss\:ff");
      string messagesText = BuildMessagesBlock(protocolModel);

      string formattedText = Template
          .Replace("$ДАТА", protocolModel.Date.ToString("dd.MM.yyyy"))
          .Replace("$ОБОЗНАЧЕНИЕ", protocolModel.Designation)
          .Replace("$РЕЖИМ", protocolModel.Mode)
          .Replace("$НОМЕР", protocolModel.Number.ToString())
          .Replace("$ПРОГРАММА", protocolModel.ProgramName)
          .Replace("$НАЧАЛО", protocolModel.StartTime.ToString("HH:mm:ss:ff"))
          .Replace("$КОНЕЦ", protocolModel.EndTime.ToString("HH:mm:ss:ff"))
          .Replace("$ВРЕМЯ", timeText + messagesText)
          .Replace("$ИСПОЛНИТЕЛЬ", protocolModel.Executor)
          .Replace("$ПРЕДСТАВИТЕЛЬ", protocolModel.Agent)
          .Replace("$ЗАКАЗЧИК", protocolModel.Customer);
      return formattedText;
    }

    public static string GetProtocolWithErrorsText(ProtocolModel protocolModel)
    {
      string messagesText = BuildMessagesBlock(protocolModel);

      const string marker = "$ПРОГРАММА";
      int markerIndex = ErrorsTemplate.IndexOf(marker);
      if (markerIndex == -1)
        throw new InvalidOperationException("В шаблоне не найден маркер $ПРОГРАММА.");

      string before = ErrorsTemplate.Substring(0, markerIndex + marker.Length);
      string after = ErrorsTemplate.Substring(markerIndex + marker.Length);

      before = before
          .Replace("$ДАТА", protocolModel.Date.ToString("dd.MM.yyyy"))
          .Replace("$ОБОЗНАЧЕНИЕ", protocolModel.Designation)
          .Replace("$РЕЖИМ", protocolModel.Mode)
          .Replace("$НОМЕР", protocolModel.Number.ToString())
          .Replace("$ПРОГРАММА", protocolModel.ProgramName);

      after = after
          .Replace("$ОБОЗНАЧЕНИЕ", protocolModel.Designation)
          .Replace("$НАИМЕНОВАНИЕ", protocolModel.ControlObjectName)
          .Replace("$НОМЕР", protocolModel.Number.ToString())
          .Replace("$БРАК(не )", "не ")
          .Replace("$ИСПОЛНИТЕЛЬ", protocolModel.Executor)
          .Replace("$ПРЕДСТАВИТЕЛЬ", protocolModel.Agent)
          .Replace("$ЗАКАЗЧИК", protocolModel.Customer);

      if (string.IsNullOrEmpty(messagesText))
      {
        return before + "\r\n" + after;
      }

      return before + messagesText + "\r\n" + after;
    }

    private static string BuildMessagesBlock(ProtocolModel protocolModel)
    {
      if (protocolModel.TotalMessageCount == 0)
        return string.Empty;

      var sb = new StringBuilder();

      sb.AppendLine().AppendLine().AppendLine();

      int sequenceIndex = 1;

      foreach (var (command, entries) in protocolModel.Messages)
      {
        var errorSignatures = entries
          .Where(item => item.Kind == ProtocolMessageKind.Error)
          .Select(item => BuildMessageSignature(item.Message))
          .ToHashSet(StringComparer.Ordinal);

        foreach (var (message, kind) in entries)
        {
          if (kind == ProtocolMessageKind.Information &&
              errorSignatures.Contains(BuildMessageSignature(message)))
          {
            continue;
          }

          var prefix = kind == ProtocolMessageKind.Error
            ? $"ERR{sequenceIndex++}"
            : $"DOC{sequenceIndex++}";

          sb.AppendLine($"{prefix} {command}: {message}");
        }
      }

      return sb.ToString();
    }

    private static string BuildMessageSignature(ShowMessageModel message)
    {
      return $"{NormalizeProtocolText(message?.Header)}|{NormalizeProtocolText(message?.Message)}";
    }

    private static string NormalizeProtocolText(string? value)
    {
      if (string.IsNullOrWhiteSpace(value))
      {
        return string.Empty;
      }

      var normalized = value.Trim();
      normalized = normalized.Replace("[БРАК]", string.Empty, StringComparison.Ordinal);
      normalized = normalized.Replace("[НОРМА]", string.Empty, StringComparison.Ordinal);
      normalized = Regex.Replace(normalized, @"\s+\(", "(");
      normalized = Regex.Replace(normalized, @"\s+", " ");
      return normalized.Trim();
    }

  }
}
