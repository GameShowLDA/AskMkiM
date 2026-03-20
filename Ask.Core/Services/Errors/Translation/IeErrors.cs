using Ask.Core.Services.Errors.Models;
using Ask.Core.Shared.Interfaces.ErrorInterfaces;
using System.IO;
using System.Runtime.CompilerServices;

namespace Ask.Core.Services.Errors.Translation
{
  public class IeErrors : IPointError
  {
    /// <summary>
    /// Ошибка: команда ИЕ не содержит ни одной границы емкости.
    /// </summary>
    public static ErrorItem EmptyLowerCapacity(int startLineNumber, string command,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0) => new()
      {
        SourceLineNumber = startLineNumber,
        Command = command,
        Code = ErrorCode.Ie_EmptyLowerCapacity,
        DebugInfo = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})",
        Description = "Команда ИЕ должна содержать нижнюю границу емкости."
      };

    /// <summary>
    /// Ошибка: не удалось распознать параметры.
    /// </summary>
    public static ErrorItem CannotParseParameters(string parameters, int startLineNumber, string command,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0) => new()
      {
        SourceLineNumber = startLineNumber,
        Command = command,
        Code = ErrorCode.Ie_CannotParseParameters,
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
        Code = ErrorCode.Ie_EmptyPoints,
        DebugInfo = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})",
        Description = "Не указаны точки для измерения."
      };


    public ErrorItem PairError(string command, string pointFirst, string pointLast, int sourceLineNumber, int formaterLineNumber,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0) => new()
      {
        Command = command,
        Code = ErrorCode.Ie_PairError,
        DebugInfo = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})",
        SourceLineNumber = sourceLineNumber,
        FormattedLineNumber = formaterLineNumber,
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
        Code = ErrorCode.Ie_PairError,
        SourceLineNumber = sourceLineNumber,
        FormattedLineNumber = formaterLineNumber,
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
        Code = ErrorCode.Ie_ChainError,
        DebugInfo = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})",
        SourceLineNumber = sourceLineNumber,
        FormattedLineNumber = formaterLineNumber,
        Description = $"Замкнутая цепь {chain}"
      };

    public ErrorItem DisconnectChainError(string command, string chain, string measureResult, int sourceLineNumber, int formaterLineNumber,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0) => new()
      {
        Command = command,
        Code = ErrorCode.Ie_ChainError,
        Description = $"Разрыв в цепи {chain}",
        DebugInfo = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})",
        SourceLineNumber = sourceLineNumber,
        FormattedLineNumber = formaterLineNumber,
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
        Code = ErrorCode.Ie_NodeExecutePointError,
      };

      var str = string.Empty;
      foreach (var item in point)
      {
        str += $"#{item}";
      }

      error.Description = $"Ошибка при проверке цепи {str} при методе полного узла.";
      return error;
    }

    /// <summary>
    /// Ошибка: конфликт нижней границы электрической емкости и верхней границы.
    /// </summary>
    public static ErrorItem CapacityLimitsConflict(int startLineNumber, string command, string description,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0) => new()
      {
        SourceLineNumber = startLineNumber,
        Command = command,
        Code = ErrorCode.Ie_CapacityLimitsConflict,
        DebugInfo = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})",
        Description = description
      };
  }
}
