using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataBaseConfiguration.Models.MeasurementError
{
  [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
  public class CommandInfoAttribute : Attribute
  {
    public string DisplayName { get; }
    public string Unit { get; }

    public CommandInfoAttribute(string displayName, string unit)
    {
      DisplayName = displayName;
      Unit = unit;
    }
  }
}
