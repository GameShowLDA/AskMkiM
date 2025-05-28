using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlCommandAnalyser.Parsing
{
  /// <summary>
  /// Атрибут для указания команд, к которым относится парсер.
  /// </summary>
  [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
  public class CommandSyntaxAttribute : Attribute
  {
    /// <summary>
    /// Мнемоника команды (например, "СИ", "ОК", "ПР").
    /// </summary>
    public string Mnemonic { get; }

    public CommandSyntaxAttribute(string mnemonic)
    {
      Mnemonic = mnemonic.ToUpperInvariant();
    }
  }
}
