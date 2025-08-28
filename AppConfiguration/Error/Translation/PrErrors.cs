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
    public ErrorItem ChainError(string command, string chain) => new()
    {
      Command = command,
      Code = ErrorCode.Pr_ChainError,
      Description = $"Замкнутая цепь {chain}"
    };

    /// <inheritdoc />
    public ErrorItem NodeExecutePointError(string command, string point, string resultMeasure) => new()
    {
      MeasureResult = resultMeasure,
      Command = command,
      Code = ErrorCode.Pr_NodeExecutePointError,
      Description = $"Ошибка при проверке точки {point}  при методе полного узла."
    };
  }
}
