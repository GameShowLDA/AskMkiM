using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities.Errors;
using Utilities.Models;

namespace AppConfiguration.Error.Translation
{
  public class PrErrors : IPointError
  {
    /// <inheritdoc />

    public ErrorItem PairError(string command, string pointFirst, string pointLast) => new()
    {
      Command = command,
      Code = ErrorCode.Pr_PairError,
      Description = $"Замкнутая пара точек: {pointFirst}, {pointLast}"
    };

    /// <inheritdoc />
    public ErrorItem ChainPairError(string command, List<PointModel> pointFirst, List<PointModel> pointLast)
    {
      var eroror = new ErrorItem()
      {
        Command = command,
        Code = ErrorCode.Pr_PairError,
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
    public ErrorItem DisconnectChainError(string command, string chain) => new()
    {
      Command = command,
      Code = ErrorCode.Pr_ChainError,
      Description = $"Разрыв в цепи {chain}"
    };

    /// <inheritdoc />
    public ErrorItem ChainError(string command, string chain) => new()
    {
      Command = command,
      Code = ErrorCode.Pr_ChainError,
      Description = $"Замкнутая цепь {chain}"
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

    /// <summary>
    /// Ошибка: команда ПР не содержит ни одного параметра.
    /// </summary>
    public static ErrorItem EmptyCommandBody(int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = ErrorCode.Pr_EmptyCommandBody,
      Description = "Команда ПР должна содержать хотя бы один параметр. Тело команды не может быть пустым."
    };

    /// <summary>
    /// Ошибка: не указаны точки для измерения.
    /// </summary>
    public static ErrorItem EmptyPoints(int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = ErrorCode.Pr_EmptyPoints,
      Description = "Не указаны точки для измерения."
    };

    /// <summary>
    /// Ошибка: не указано сопротивление.
    /// </summary>
    public static ErrorItem EmptyResistance(int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = ErrorCode.Pr_EmptyResistance,
      Description = "Не указано сопротивление."
    };


  }
}
