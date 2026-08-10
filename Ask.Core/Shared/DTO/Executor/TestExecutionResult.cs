using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Core.Shared.DTO.Executor
{
  public class TestExecutionResult
  {
    /// <summary>
    /// Название теста.
    /// </summary>
    public required string TestName { get; init; }

    /// <summary>
    /// Ошибки, обнаруженные в тесте.
    /// </summary>
    public List<TestError> Errors { get; } = [];
  }
}
