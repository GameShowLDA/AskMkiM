using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities.Models;

namespace ControlCommandAnalyser.Model
{
  public class RmPairModel
  {
    public string OkPoint { get; set; }
    public string? Synonym { get; set; }
    public string AskInput { get; set; }
    public override string ToString()
    {
      return Synonym != null
          ? $"{OkPoint}({Synonym}) => {AskInput}"
          : $"{OkPoint} => {AskInput}";
    }
  }

}
