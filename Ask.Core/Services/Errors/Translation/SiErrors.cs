using Ask.Core.Services.Errors.Models;
using Ask.Core.Shared.Interfaces.ErrorInterfaces;
using System.IO;
using System.Runtime.CompilerServices;

namespace Ask.Core.Services.Errors.Translation
{
  /// <summary>
  /// Содержит шаблоны ошибок, возникающих при парсинге выражений СИ-команд.
  /// </summary>
  public class SiErrors : IPointError
  {
    /// <summary>
    /// Ошибка: выражение не распознано.
    /// </summary>
    public static ErrorItem CannotParseExpression(string expr, int startLineNumber, string command,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0) => new()
      {
        SourceLineNumber = startLineNumber,
        Command = command,
        Code = ErrorCode.Si_CannotParseExpression,
        DebugInfo = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})",
        Description = $"Не удалось распознать выражение: {expr}"
      };

    /// <summary>
    /// Ошибка: не удалось распознать параметры (напряжение, сопротивление, время).
    /// </summary>
    public static ErrorItem CannotParseParameters(string parameters, int startLineNumber, string command,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0) => new()
      {
        SourceLineNumber = startLineNumber,
        Command = command,
        Code = ErrorCode.Si_CannotParseParameters,
        DebugInfo = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})",
        Description = $"Не удалось распознать параметры: {parameters}"
      };

    /// <summary>
    /// Ошибка: не указаны точки для измерения.
    /// </summary>
    public static ErrorItem EmptyPoints(int startLineNumber, string command,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0) => new()
      {
        SourceLineNumber = startLineNumber,
        Command = command,
        Code = ErrorCode.Si_EmptyPoints,
        DebugInfo = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})",
        Description = "Не указаны точки для измерения."
      };

    /// <summary>
    /// Ошибка: не указано напряжение для измерения.
    /// </summary>
    public static ErrorItem EmptyVoltage(int startLineNumber, string command,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0) => new()
      {
        SourceLineNumber = startLineNumber,
        Command = command,
        Code = ErrorCode.Si_EmptyVoltage,
        DebugInfo = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})",
        Description = "Не указано напряжение для измерения."
      };

    /// <summary>
    /// Ошибка: команда СИ не содержит ни одного параметра.
    /// </summary>
    public static ErrorItem EmptyCommandBody(int startLineNumber, string command,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0) => new()
      {
        SourceLineNumber = startLineNumber,
        Command = command,
        Code = ErrorCode.Si_EmptyCommandBody,
        DebugInfo = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})",
        Description = "Команда СИ должна содержать хотя бы один параметр. Тело команды не может быть пустым."
      };

    /// <summary>
    /// Ошибка: Ошибка при проверке одно из разряда в групповом методе.
    /// </summary>
    /// <param name="command">Номер команды и мнемоника.</param>
    /// <param name="step">Номер разряда.</param>
    /// <param name="countStep">Кол-во разрядов.</param>
    /// <param name="resultMeasure">Результат измерения.</param>
    /// <returns></returns>
    public static ErrorItem WrongDigitCheckForGroupedMethod(string command, int step, int countStep, string resultMeasure,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0) => new()
      {
        MeasureResult = resultMeasure,
        Command = command,
        Code = ErrorCode.Si_WrongDigitCheckForGroupedMethod,
        DebugInfo = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})",
        Description = $"Ошибка при проверке разряда {step} ({countStep}) при групповом методе."
      };

    /// <inheritdoc />
    public ErrorItem NodeExecutePointError(string command, List<string> point, string resultMeasure, int sourceLineNumber, int formaterLineNumber,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0)
    {
      var error = new ErrorItem()
      {
        MeasureResult = resultMeasure,
        Command = command,
        SourceLineNumber = sourceLineNumber,
        FormattedLineNumber = formaterLineNumber,
        DebugInfo = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})",
        Code = ErrorCode.Pr_NodeExecutePointError,
      };

      var str = string.Empty;
      foreach (var item in point)
      {
        str += $"#{item}";
      }

      error.Description = $"Ошибка при проверке цепи {str} при методе полного узла.";
      return error;
    }

    /// <inheritdoc />
    public ErrorItem ChainError(string command, string chain, int sourceLineNumber, int formaterLineNumber,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0) => new()
      {
        Command = command,
        Code = ErrorCode.Si_ChainError,
        SourceLineNumber = sourceLineNumber,
        FormattedLineNumber = formaterLineNumber,
        DebugInfo = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})",
        Description = $"Замкнутая цепь {chain}"
      };

    /// <inheritdoc />
    public ErrorItem DisconnectChainError(string command, string chain, string measureResult, int sourceLineNumber, int formaterLineNumber,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0) => new()
      {
        Command = command,
        Code = ErrorCode.Si_ChainError,
        Description = $"Разрыв в цепи {chain}",
        SourceLineNumber = sourceLineNumber,
        FormattedLineNumber = formaterLineNumber,
        DebugInfo = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})",
        MeasureResult = measureResult
      };

    /// <inheritdoc />
    public ErrorItem PairError(string command, string pointFirst, string pointLast, int sourceLineNumber, int formaterLineNumber,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0) => new()
      {
        Command = command,
        Code = ErrorCode.Si_PairError,
        SourceLineNumber = sourceLineNumber,
        FormattedLineNumber = formaterLineNumber,
        DebugInfo = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})",
        Description = $"Замкнутая пара точек: {pointFirst}, {pointLast}"
      };

    /// <inheritdoc />
    public ErrorItem ChainPairError(string command, List<string> pointFirst, List<string> pointLast, string value, int sourceLineNumber, int formaterLineNumber,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0)
    {
      var eroror = new ErrorItem()
      {
        Command = command,
        Code = ErrorCode.Si_PairError,
        SourceLineNumber = sourceLineNumber,
        FormattedLineNumber = formaterLineNumber,
        DebugInfo = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})",
        MeasureResult = value,
      };

      var firstChain = string.Empty;
      foreach (var point in pointFirst)
      {
        firstChain += $"#{point.ToString()}";
      }

      var secondChain = string.Empty;
      foreach (var point in pointLast)
      {
        secondChain += $"#{point.ToString()}";
      }

      eroror.Description = $"Замкнутая пара цепей: {firstChain} и {secondChain}";
      return eroror;
    }

    /// <summary>
    /// Ошибка: конфликт границ сопротивления.
    /// </summary>
    public static ErrorItem ResistanceLimitsConflict(int startLineNumber, string command, string description,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0) => new()
      {
        SourceLineNumber = startLineNumber,
        Command = command,
        Code = ErrorCode.Si_ResistanceLimitsConflict,
        DebugInfo = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})",
        Description = description
      };
    /// <summary>
    /// Ошибка: предыдущая коамнда не имеет точек для проверки.
    /// </summary>
    public static ErrorItem PreviousCommandHasNoPoints(int startLineNumber, string command,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0) => new()
      {
        SourceLineNumber = startLineNumber,
        Command = command,
        Code = ErrorCode.Si_PreviousCommandHasNoPoints,
        DebugInfo = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})",
        Description = $"В команде, предшествующей команде {command} не указаны точки для измерения"
      };
  }
}
