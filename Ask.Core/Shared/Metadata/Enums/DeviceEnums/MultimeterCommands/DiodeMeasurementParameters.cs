using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Core.Shared.Metadata.Enums.DeviceEnums.MultimeterCommands
{
  public class DiodeMeasurementParameters
  {
    public string SetMode { get; init; } = "CONF:DIOD";

    public string CheckMode { get; init; } = "DIOD";

    public string GetMode { get; init; } = "FUNC?";

    public string Measure { get; init; } = "MEAS:DIOD?";

    /// <summary>
    /// Время ожидания ответа.
    /// </summary>
    public int Timeout { get; init; } = 1000;
  }
}
