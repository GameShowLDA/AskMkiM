using ControlCommandAnalyser.Model.Ok;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlCommandAnalyser.Model
{
  public class KscCommandModel : BaseCommandModel
  {
    public OkCommandModel OkCommandModel { get; set; }
    public override string Mnemonic => "КЦ";
  }
}