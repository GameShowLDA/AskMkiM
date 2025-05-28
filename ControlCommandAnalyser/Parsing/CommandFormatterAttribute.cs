using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlCommandAnalyser.Parsing
{
  /// <summary>
  /// Атрибут для указания, к какой мнемонике команды относится форматтер.
  /// </summary>
  [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
  public class CommandFormatterAttribute : Attribute
  {
    /// <summary>
    /// Мнемоника команды, для которой применяется форматтер (например, "СИ", "ОК", "ПР").
    /// </summary>
    public string Mnemonic { get; }

    public CommandFormatterAttribute(string mnemonic)
    {
      Mnemonic = mnemonic.ToUpperInvariant();
    }
  }
}
