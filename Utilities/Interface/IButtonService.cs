using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utilities.Interface
{
  public interface IButtonService
  {
    public void SetNonVisibleAllButton();
    public void ShowOnlyStartButton();
    public void ShowOnlyStopAndFinishButtons();
    public void ShowOnlyExitButton();
    public void ShowButtonsOnPause();
  }
}
