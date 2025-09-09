using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities.Errors;
using Utilities.Models;

namespace AppConfiguration.Error.Translation
{
  /// <summary>
  /// Содержит шаблоны ошибок, возникающих при парсинге выражений ПИ-команд.
  /// </summary>
  public class PiErrors : IPointError
  {
    /// <summary>
    /// Ошибка: выражение не распознано.
    /// </summary>
    public static ErrorItem CannotParseExpression(string expr, int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = ErrorCode.Pi_CannotParseExpression,
      Description = $"Не удалось распознать выражение: {expr}"
    };

    /// <summary>
    /// Ошибка: не удалось распознать параметры (напряжение, пороговое сопротивление, время).
    /// </summary>
    public static ErrorItem CannotParseParameters(string parameters, int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = ErrorCode.Pi_CannotParseParameters,
      Description = $"Не удалось распознать параметры: {parameters}"
    };

    /// <summary>
    /// Ошибка: не указаны точки для измерения.
    /// </summary>
    public static ErrorItem EmptyPoints(int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = ErrorCode.Pi_EmptyPoints,
      Description = "Не указаны точки для измерения."
    };

    /// <summary>
    /// Ошибка: команда ПИ не содержит ни одного параметра.
    /// </summary>
    public static ErrorItem EmptyCommandBody(int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = ErrorCode.Pi_EmptyCommandBody,
      Description = "Команда ПИ должна содержать хотя бы один параметр. Тело команды не может быть пустым."
    };

    /// <summary>
    /// Ошибка: команда ПИ не может содержать ключ Г, если для команды СИ присвоен ключ Т1.
    /// </summary>
    public static ErrorItem KeysConflict(int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = ErrorCode.Pi_KeysConflict,
      Description = "Команда ПИ не может содержать ключ Г, если для команды СИ присвоен ключ Т1."
    };

    public ErrorItem PairError(string command, string pointFirst, string pointLast) => new()
    {
      Command = command,
      Code = ErrorCode.Pi_PairError,
      Description = $"Замыкание точек: {pointFirst}, {pointLast}"
    };

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

      eroror.Description = $"Замыкание цепей: {firstChain} и {secondChain}";
      return eroror;
    }

    public ErrorItem ChainError(string command, string chain) => new()
    {
      Command = command,
      Code = ErrorCode.Pi_ChainError,
      Description = $"Замыкание цепи {chain}"
    };

    public ErrorItem DisconnectChainError(string command, string chain) => new()
    {
      Command = command,
      Code = ErrorCode.Pi_ChainError,
      Description = $"Замыкание в цепи {chain}"
    };

    public ErrorItem NodeExecutePointError(string command, List<PointModel> point, string resultMeasure)
    {
      var error = new ErrorItem()
      {
        MeasureResult = resultMeasure,
        Command = command,
        Code = ErrorCode.Pi_NodeExecutePointError,
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
