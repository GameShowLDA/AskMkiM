using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.Controls.ProtocolController
{
  public class StepBlockToken
  {
    public bool IsInsideBlock { get; private set; }
    public bool IsStepOverActive { get; private set; }

    public void EnterBlock()
    {
      IsInsideBlock = true;
    }

    public void ExitBlock()
    {
      IsInsideBlock = false;
      IsStepOverActive = false;
    }

    public void ActivateStepOver()
    {
      IsStepOverActive = true;
    }
  }

}
