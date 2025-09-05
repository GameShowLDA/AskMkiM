using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlCommandAnalyser.Model;
using ControlCommandExecutor.Execution;

namespace ControlCommandExecutor.Executors
{
  internal class KscCommandExecutor : ICommandExecutor
  {
    public string Mnemonic => "КЦ";

    public async Task ExecuteAsync(CommandExecutionContext context)
    {
      var command = context.Command as KscCommandModel;
      context.TranslationControl.SetActiveLine(command.FormattedStartLineNumber);
      var breakDown = await EquipmentService.GetBreakdownTesterOrThrow(context.Console);
      await breakDown.ConnectableManager.DisconnectAsync();

      if (!await AppConfiguration.Execution.ExecutionConfig.GetIsIdleModeEnabled())
      {
        await NewCore.Communication.DeviceCommandSender.ResetAllSystem();
      }
    }
  }
}
