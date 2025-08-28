using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlCommandAnalyser.Model
{
  /// <summary>
  /// Модель команды УП (условный переход).
  /// </summary>
  public class UpCommandModel : BaseCommandModel
  {
    /// <summary>
    /// Номер перехода (метка, на которую надо перейти).
    /// </summary>
    public string TargetLabel { get; set; }
  }
}
