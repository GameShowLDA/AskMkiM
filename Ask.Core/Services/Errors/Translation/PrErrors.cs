using Ask.Core.Services.Errors.Models;
using Ask.Core.Shared.Interfaces.ErrorInterfaces;
using System.IO;
using System.Runtime.CompilerServices;

namespace Ask.Core.Services.Errors.Translation
{
  public class PrErrors : IPointError
  {
    /// <inheritdoc />

    public ErrorItem PairError(string command, string pointFirst, string pointLast, int sourceLineNumber, int formaterLineNumber,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0) => new()
      {
        Command = command,
        Code = ErrorCode.Pr_PairError,
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
        Code = ErrorCode.Pr_PairError,
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

    /// <inheritdoc />
    public ErrorItem DisconnectChainError(string command, string chain, string measureResult, int sourceLineNumber, int formaterLineNumber,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0) => new()
      {
        Command = command,
        Code = ErrorCode.Pr_ChainError,
        Description = $"Разрыв в цепи {chain}",
        SourceLineNumber = sourceLineNumber,
        FormattedLineNumber = formaterLineNumber,
        DebugInfo = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})",
        MeasureResult = measureResult
      };

    /// <inheritdoc />
    public ErrorItem ChainError(string command, string chain, int sourceLineNumber, int formaterLineNumber,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0) => new()
      {
        Command = command,
        Code = ErrorCode.Pr_ChainError,
        SourceLineNumber = sourceLineNumber,
        FormattedLineNumber = formaterLineNumber,
        DebugInfo = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})",
        Description = $"Замкнутая цепь {chain}"
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

    /// <summary>
    /// Ошибка: команда ПР не содержит ни одного параметра.
    /// </summary>
    public static ErrorItem EmptyCommandBody(int startLineNumber, string command,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0) => new()
      {
        SourceLineNumber = startLineNumber,
        Command = command,
        Code = ErrorCode.Pr_EmptyCommandBody,
        DebugInfo = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})",
        Description = "Команда ПР должна содержать хотя бы один параметр. Тело команды не может быть пустым."
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
        Code = ErrorCode.Pr_EmptyPoints,
        DebugInfo = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})",
        Description = "Не указаны точки для измерения."
      };

    /// <summary>
    /// Ошибка: не указано сопротивление.
    /// </summary>
    public static ErrorItem EmptyResistance(int startLineNumber, string command,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0) => new()
      {
        SourceLineNumber = startLineNumber,
        Command = command,
        Code = ErrorCode.Pr_EmptyResistance,
        DebugInfo = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})",
        Description = "Не указано сопротивление."
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
        Code = ErrorCode.Pr_ResistanceLimitsConflict,
        DebugInfo = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})",
        Description = description
      };

    /// <summary>
    /// Ошибка: одна из границ сопротивления больше максимально измеряемой мультиметром границы сопротивления или ниже минимально измеряемой. 
    /// </summary>
    public static ErrorItem EquipmentOutOfRange(int startLineNumber, string command, string description,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0) => new()
      {
        SourceLineNumber = startLineNumber,
        Command = command,
        Code = ErrorCode.Pr_EquipmentOutOfRange,
        DebugInfo = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})",
        Description = description
      };

    /// <summary>
    /// Ошибка: верхняя граница сопротивления больше максимально допустимой границы сопротивления.
    /// </summary>
    public static ErrorItem ResistanceMaxLimitsConflict(int startLineNumber, string command, double? maxResistance, string unit,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0) => new()
      {
        SourceLineNumber = startLineNumber,
        Command = command,
        Code = ErrorCode.Pr_ResistanceMaxLimitsConflict,
        DebugInfo = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})",
        Description = $"Верхняя граница сопротивления больше максимально допустимой границы сопротивления({maxResistance} {unit})."
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
        Code = ErrorCode.Pr_CannotParseParameters,
        DebugInfo = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})",
        Description = $"Не удалось распознать параметры: {parameters}"
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
        Code = ErrorCode.Pr_PreviousCommandHasNoPoints,
        DebugInfo = $"{Path.GetFileName(callerFile)} → {callerName} (строка {callerLine})",
        Description = $"В команде, предшествующей команде {command} не указаны точки для измерения"
      };
  }
}
