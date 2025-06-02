using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppConfiguration.Base
{
  [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
  public class CommandInfoAttribute : Attribute
  {
    public string DisplayName { get; }
    public string Unit { get; }
    public double DefaultPercentage { get; }
    public double DefaultNumeric { get; }

    public CommandInfoAttribute(string displayName, string unit, double defaultPercentage = 0, double defaultNumeric = 0)
    {
      DisplayName = displayName;
      Unit = unit;
      DefaultPercentage = defaultPercentage;
      DefaultNumeric = defaultNumeric;
    }
  }

}
