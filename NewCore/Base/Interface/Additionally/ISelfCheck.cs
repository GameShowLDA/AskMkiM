using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppConfiguration.Interface;

namespace NewCore.Base.Interface.Additionally
{
  public interface ISelfCheck
  {
    Task StartSelfCheck(IUserMessageService userMessageService);
  }
}
