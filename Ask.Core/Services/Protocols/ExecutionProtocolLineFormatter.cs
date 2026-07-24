using Ask.Core.Shared.DTO.Protocol;

namespace Ask.Core.Services.Protocols;

/// <summary>
/// Форматирует записи протокола выполнения для сохранения и печати.
/// </summary>
public static class ExecutionProtocolLineFormatter
{
  /// <summary>
  /// Формирует строку протокола с отступом и временем выполнения.
  /// </summary>
  /// <param name="message">Запись протокола.</param>
  /// <returns>Текстовое представление записи протокола.</returns>
  public static string Format(ShowMessageModel message)
  {
    ArgumentNullException.ThrowIfNull(message);

    string header = message.Header?.TrimEnd() ?? string.Empty;
    string body = message.Message?.TrimEnd() ?? string.Empty;
    string content = FormatContent(header, body);

    if (!string.IsNullOrWhiteSpace(message.Time))
    {
      content = string.IsNullOrWhiteSpace(content)
        ? message.Time.Trim()
        : $"{content} | {message.Time.Trim()}";
    }

    string indent = new(' ', Math.Max(0, message.IndentLevel) * 2);
    return $"{indent}{content}";
  }

  private static string FormatContent(string header, string body)
  {
    bool hasHeader = !string.IsNullOrWhiteSpace(header);
    bool hasBody = !string.IsNullOrWhiteSpace(body);

    if (hasHeader && hasBody)
    {
      return $"{header}: {body}";
    }

    return hasHeader ? header : body;
  }
}
