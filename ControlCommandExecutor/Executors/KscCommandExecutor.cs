using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlCommandAnalyser.Model;
using ControlCommandExecutor.Execution;
using NewCore.Base.Interface.Main;

namespace ControlCommandExecutor.Executors
{
  internal class KscCommandExecutor : ICommandExecutor
  {
    public string Mnemonic => "КЦ";

    public async Task ExecuteAsync(CommandExecutionContext context)
    {
      var command = context.Command as KscCommandModel;
      context.TranslationControl.SetActiveLine(command.FormattedStartLineNumber);
      await AppConfiguration.ServiceLocator.GetRequired<IBreakdownTester>().ConnectableManager.DisconnectAsync();

      if (!await AppConfiguration.Execution.ExecutionConfig.GetIsIdleModeEnabled())
      {
        await NewCore.Communication.DeviceCommandSender.ResetAllSystem();
      }
    }
  }
}
