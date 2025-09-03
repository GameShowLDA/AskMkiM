using System;
using Utilities.Errors;
using Utilities.Models;

namespace AppConfiguration.Error.Translation
{
  /// <summary>
  /// Содержит шаблоны ошибок, возникающих при парсинге выражений СИ-команд.
  /// </summary>
  public class SiErrors : IPointError
  {
    /// <summary>
    /// Ошибка: выражение не распознано.
    /// </summary>
    public static ErrorItem CannotParseExpression(string expr, int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = ErrorCode.Si_CannotParseExpression,
      Description = $"Не удалось распознать выражение: {expr}"
    };

    /// <summary>
    /// Ошибка: не удалось распознать параметры (напряжение, сопротивление, время).
    /// </summary>
    public static ErrorItem CannotParseParameters(string parameters, int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = ErrorCode.Si_CannotParseParameters,
      Description = $"Не удалось распознать параметры: {parameters}"
    };

    /// <summary>
    /// Ошибка: не указаны точки для измерения.
    /// </summary>
    public static ErrorItem EmptyPoints(int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = ErrorCode.Si_EmptyPoints,
      Description = "Не указаны точки для измерения."
    };

    /// <summary>
    /// Ошибка: команда СИ не содержит ни одного параметра.
    /// </summary>
    public static ErrorItem EmptyCommandBody(int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = ErrorCode.Si_EmptyCommandBody,
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
    public static ErrorItem WrongDigitCheckForGroupedMethod(string command, int step, int countStep, string resultMeasure) => new()
    {
      MeasureResult = resultMeasure,
      Command = command,
      Code = ErrorCode.Si_WrongDigitCheckForGroupedMethod,
      Description = $"Ошибка при проверке разряда {step} ({countStep}) при групповом методе."
    };

    /// <inheritdoc />
    public ErrorItem NodeExecutePointError(string command, List<PointModel> point, string resultMeasure)
    {
      var error = new ErrorItem()
      {
        MeasureResult = resultMeasure,
        Command = command,
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
    public ErrorItem ChainError(string command, string chain) => new()
    {
      Command = command,
      Code = ErrorCode.Si_ChainError,
      Description = $"Замкнутая цепь {chain}"
    };


    /// <inheritdoc />
    public ErrorItem PairError(string command, string pointFirst, string pointLast) => new()
    {
      Command = command,
      Code = ErrorCode.Si_PairError,
      Description = $"Замкнутая пара точек: {pointFirst}, {pointLast}"
    };

    /// <inheritdoc />
    public ErrorItem ChainPairError(string command, List<PointModel> pointFirst, List<PointModel> pointLast)
    {
      var eroror = new ErrorItem()
      {
        Command = command,
        Code = ErrorCode.Si_PairError,
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
  }
}
