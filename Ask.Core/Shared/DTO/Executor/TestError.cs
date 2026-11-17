using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Core.Shared.DTO.Executor
{
  public class TestError
  {
    /// <summary>
    /// Признак отсутствия ответа устройства вместо выявленной неисправности.
    /// </summary>
    public bool IsNoResponse { get; init; }
    /// <summary>
    /// Описание ошибки.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Значение измерения (если есть).
    /// </summary>
    public string? Value { get; init; }

    /// <summary>
    /// Единица измерения.
    /// </summary>
    public string? Unit { get; init; }
  }
}
