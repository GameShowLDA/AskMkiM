using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Core.Shared.Metadata.Enums.DeviceEnums.MultimeterCommands
{
  public class CapacitanceMeasurementParameters
  {
    public string SetMode { get; init; } = "CONF:CAP";

    public string CheckMode { get; init; } = "CAP";

    public string GetMode { get; init; } = "FUNC?";

    public string Measure { get; init; } = "MEAS:CAP?";

    /// <summary>
    /// Время ожидания ответа.
    /// </summary>
    public int Timeout { get; init; } = 1000;
  }
}
