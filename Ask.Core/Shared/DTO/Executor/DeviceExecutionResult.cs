using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Core.Shared.DTO.Executor
{
  public class DeviceExecutionResult
  {
    /// <summary>
    /// Название устройства.
    /// </summary>
    public required string DeviceName { get; init; }

    /// <summary>
    /// Выполненные тесты.
    /// </summary>
    public List<TestExecutionResult> Tests { get; } = [];
  }
}
