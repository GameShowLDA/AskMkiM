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
