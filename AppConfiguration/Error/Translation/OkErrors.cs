using System;
using Utilities.Models;

namespace AppConfiguration.Error.Translation
{
  /// <summary>
  /// Содержит шаблоны ошибок, возникающих при парсинге выражений ОК-команды.
  /// </summary>
  public static class OkErrors
  {
    /// <summary>
    /// Ошибка: не удалось разобрать первую строку команды ОК.
    /// </summary>
    public static ErrorItem CannotParseFirstLine(int startLineNumber, string command, string line) => new()
    {
      LineNumber = startLineNumber,
      Command = command,
      Description = $"Не удалось разобрать первую строку команды ОК"
    };

    /// <summary>
    /// Ошибка: не указано обозначение объекта контроля.
    /// </summary>
    public static ErrorItem MissingObjectCode(int startLineNumber, string command) => new()
    {
      LineNumber = startLineNumber,
      Command = command,
      Description = "Не указано обозначение объекта контроля (обязательное поле)."
    };

    /// <summary>
    /// Ошибка: не указано наименование объекта контроля.
    /// </summary>
    public static ErrorItem MissingObjectName(int startLineNumber, string command) => new()
    {
      LineNumber = startLineNumber,
      Command = command,
      Description = "Не указано наименование объекта контроля (после *)."
    };

    /// <summary>
    /// Ошибка: команда ОК не содержит ни одной строки.
    /// </summary>
    public static ErrorItem EmptyCommandBody(int startLineNumber, string command) => new()
    {
      LineNumber = startLineNumber,
      Command = command,
      Description = "Команда ОК должна содержать хотя бы одну строку. Тело команды не может быть пустым."
    };

    /// <summary>
    /// Ошибка: параметр не соответствует формату КЛЮЧ=ЗНАЧЕНИЕ.
    /// </summary>
    public static ErrorItem CannotParseParameter(int lineNumber, string command, string line) => new()
    {
      LineNumber = lineNumber,
      Command = command,
      Description = $"Не удалось разобрать параметр: {line}"
    };

    /// <summary>
    /// Ошибка: длина идентификатора параметра превышает 39 символов.
    /// </summary>
    public static ErrorItem ParameterKeyTooLong(int lineNumber, string command, string key) => new()
    {
      LineNumber = lineNumber,
      Command = command,
      Description = $"Длина идентификатора параметра '{key}' превышает 39 символов."
    };

    /// <summary>
    /// Ошибка: длина значения параметра превышает максимально допустимую.
    /// </summary>
    public static ErrorItem ParameterValueTooLong(int lineNumber, string command, string key, int maxLength) => new()
    {
      LineNumber = lineNumber,
      Command = command,
      Description = $"Значение параметра '{key}' превышает максимально допустимую длину {maxLength} символов."
    };

    /// <summary>
    /// Ошибка: повтор идентификатора параметра, не допускается для данного ключа.
    /// </summary>
    public static ErrorItem DuplicateParameterKey(int lineNumber, string command, string key) => new()
    {
      LineNumber = lineNumber,
      Command = command,
      Description = $"Повтор идентификатора параметра '{key}'. Допускается только для КД, ЦЕХ и ПРИМ/ПРИМЕЧ(АНИЕ)."
    };

    /// <summary>
    /// Ошибка: обозначение объекта контроля превышает 39 символов.
    /// </summary>
    public static ErrorItem ObjectCodeTooLong(int startLineNumber, string command) => new()
    {
      LineNumber = startLineNumber,
      Command = command,
      Description = "Обозначение объекта контроля превышает 39 символов."
    };

    /// <summary>
    /// Ошибка: наименование объекта контроля превышает 39 символов.
    /// </summary>
    public static ErrorItem ObjectNameTooLong(int startLineNumber, string command) => new()
    {
      LineNumber = startLineNumber,
      Command = command,
      Description = "Наименование объекта контроля превышает 39 символов."
    };
  }
}
