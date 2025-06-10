using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppConfiguration.MeasurementError
{
  public static class ErrorProviderLocator
  {
    public static IMeasurementErrorProvider? Provider { get; set; }
  }
}
