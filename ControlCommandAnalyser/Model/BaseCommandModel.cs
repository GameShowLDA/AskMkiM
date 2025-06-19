using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities.Models;

namespace ControlCommandAnalyser.Model
{
  /// <summary>
  /// Базовая модель любой команды после разбора.
  /// </summary>
  public abstract class BaseCommandModel
  {
    /// <summary>
    /// Номер команды.
    /// </summary>
    public string CommandNumber { get; set; }

    /// <summary>
    /// Мнемоника (тип команды).
    /// </summary>
    public string Mnemonic { get; set; }

    public List<string> SourceLines { get; set; } = new List<string>();

    public List<ErrorItem> Errors { get; set; } = new List<ErrorItem>();

    /// <summary>
    /// Номер строки, с которой начинается команда (в исходном тексте).
    /// </summary>
    public int StartLineNumber { get; set; }
  }
}
