using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppConfiguration.Error.Translation;

namespace ControlCommandAnalyser.Model
{
  internal interface IError
  {
    IPointError PointErrors { get; }
  }
}
