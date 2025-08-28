using System;
using Utilities.Errors;
using Utilities.Models;

namespace AppConfiguration.Error.Translation
{
  /// <summary>
  /// Содержит шаблоны ошибок, связанных с ключами алгоритма команд.
  /// </summary>
  public static class KeyErrors
  {
    /// <summary>
    /// Ошибка: ключ не распознан как допустимый.
    /// </summary>
    /// <param name="key">Строковое представление ключа.</param>
    /// <param name="startLineNumber">Номер строки команды.</param>
    /// <param name="command">Мнемоника команды.</param>
    public static ErrorItem NotRecognized(string key, int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = ErrorCode.Key_NotRecognized,
      Description = $"Ключ '{key}' не распознан."
    };

    /// <summary>
    /// Ошибка: ключ недопустим для данной команды.
    /// </summary>
    /// <param name="key">Ключ, вызвавший ошибку.</param>
    /// <param name="startLineNumber">Номер строки команды.</param>
    /// <param name="command">Мнемоника команды.</param>
    public static ErrorItem NotAllowed(string key, int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = ErrorCode.Key_NotAllowedForCommand,
      Description = $"Ключ '{key}' недопустим для команды {command}."
    };

    /// <summary>
    /// Ошибка: конфликтующие ключи.
    /// </summary>
    /// <param name="key1">Первый конфликтующий ключ.</param>
    /// <param name="key2">Второй конфликтующий ключ.</param>
    /// <param name="startLineNumber">Номер строки команды.</param>
    /// <param name="command">Мнемоника команды.</param>
    public static ErrorItem Conflict(string key1, string key2, int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = ErrorCode.Key_ConflictPair,
      Description = $"Ключи '{key1}' и '{key2}' не могут использоваться одновременно."
    };

    /// <summary>
    /// Ошибка: команда не поддерживает ключи.
    /// </summary>
    /// <param name="startLineNumber">Номер строки команды.</param>
    /// <param name="command">Мнемоника команды.</param>
    public static ErrorItem NotExpected(int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = ErrorCode.Key_NotExpectedInThisCommand,
      Description = $"Команда {command} не поддерживает использование ключей алгоритма."
    };
  }
}
