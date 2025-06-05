using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlCommandAnalyser.Parsing.Model
{
  /// <summary>
  /// Базовая модель любой команды.
  /// </summary>
  public class BaseCommandModel
  {
    /// <summary>Номер команды.</summary>
    public string CommandNumber { get; set; }

    /// <summary>Мнемоника (тип команды).</summary>
    public string Mnemonic { get; set; }

    /// <summary>Исходный текст (по желанию).</summary>
    public string SourceLine { get; set; }
  }
}
