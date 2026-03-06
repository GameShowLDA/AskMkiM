using Ask.Core.Services.Errors.Models;
using Ask.Core.Shared.Interfaces.ErrorInterfaces;
using System.IO;
using System.Runtime.CompilerServices;

namespace Ask.Core.Services.Errors.Translation
{
  /// <summary>
  /// Содержит шаблоны ошибок, возникающих при парсинге выражений КС-команд.
  /// </summary>
  public class KsErrors : IPointError
  {
    /// <summary>
    /// Ошибка: команда КС не содержит ни одной границы для сопротивления.
    /// </summary>
    public static ErrorItem EmptyResistance(int startLineNumber, string command,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0) => new()
      {
        SourceLineNumber = startLineNumber,
        Command = command,
        Code = ErrorCode.Ks_EmptyResistance,
        DebugInfo = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})",
        Description = "Команда КС должна содержать хотя бы одну из границ сопротивления. Сопротивление не может быть не задано."
      };
    /// <summary>
    /// Ошибка: нижняя граница сопротивления больше верхней границы сопротивления.
    /// </summary>
    public static ErrorItem ResistanceLimitsConflict(int startLineNumber, string command, string description,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0) => new()
      {
        SourceLineNumber = startLineNumber,
        Command = command,
        Code = ErrorCode.Ks_CapacityLimitsConflict,
        DebugInfo = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})",
        Description = description
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
        Code = ErrorCode.Ks_CannotParseParameters,
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
        Code = ErrorCode.Ks_EmptyPoints,
        DebugInfo = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})",
        Description = "Не указаны точки для измерения."
      };


    /// <summary>
    /// Ошибка: команда КС не содержит ни одного параметра.
    /// </summary>
    public static ErrorItem EmptyCommandBody(int startLineNumber, string command,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0) => new()
      {
        SourceLineNumber = startLineNumber,
        Command = command,
        Code = ErrorCode.Ks_EmptyCommandBody,
        DebugInfo = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})",
        Description = "Команда КС должна содержать хотя бы один параметр. Тело команды не может быть пустым."
      };

    public ErrorItem PairError(string command, string pointFirst, string pointLast, int sourceLineNumber, int formaterLineNumber,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0) => new()
      {
        Command = command,
        Code = ErrorCode.Ks_PairError,
        SourceLineNumber = sourceLineNumber,
        FormattedLineNumber = formaterLineNumber,
        DebugInfo = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})",
        Description = $"Замкнутая пара точек: {pointFirst}, {pointLast}"
      };

    public ErrorItem ChainPairError(string command, List<string> pointFirst, List<string> pointLast, string value, int sourceLineNumber, int formaterLineNumber,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0)
    {
      var eroror = new ErrorItem()
      {
        Command = command,
        Code = ErrorCode.Ks_PairError,
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

    public ErrorItem ChainError(string command, string chain, int sourceLineNumber, int formaterLineNumber,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0) => new()
      {
        Command = command,
        Code = ErrorCode.Ks_ChainError,
        SourceLineNumber = sourceLineNumber,
        FormattedLineNumber = formaterLineNumber,
        DebugInfo = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})",
        Description = $"Замкнутая цепь {chain}"
      };

    public ErrorItem DisconnectChainError(string command, string chain, string measureResult, int sourceLineNumber, int formaterLineNumber,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0) => new()
      {
        Command = command,
        Code = ErrorCode.Ks_ChainError,
        SourceLineNumber = sourceLineNumber,
        FormattedLineNumber = formaterLineNumber,
        DebugInfo = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})",
        Description = $"Разрыв в цепи {chain}",
        MeasureResult = measureResult
      };

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
        Code = ErrorCode.Ks_NodeExecutePointError,
      };

      var str = string.Empty;
      foreach (var item in point)
      {
        str += $"#{item}";
      }

      error.Description = $"Ошибка при проверке цепи {str} при методе полного узла.";
      return error;
    }
  }
}
