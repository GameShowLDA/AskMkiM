using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Engine.ControlCommandAnalyser.Model.Interface
{
  public interface IHasScheme
  {
    /// <summary>
    /// Схема измерений.
    /// </summary>
    SchemeModel Scheme { get; set; }
  }
}
