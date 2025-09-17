using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppConfiguration.Base;
using ControlCommandAnalyser.Model;
using ControlCommandExecutor.Execution;
using NewCore.Base.Interface.Main;
using Utilities.ResultProtocol;

namespace ControlCommandExecutor.Executors
{
  internal class KscCommandExecutor : ICommandExecutor
  {
    public string Mnemonic => "КЦ";


    public async Task ExecuteAsync(CommandExecutionContext context)
    {
      var command = context.Command as KscCommandModel;
      context.TranslationControl.SetActiveLine(command.FormattedStartLineNumber);

      if (!await AppConfiguration.Execution.ExecutionConfig.GetIsIdleModeEnabled())
      {
        await NewCore.Communication.DeviceCommandSender.ResetAllSystem();
      }

      command.OkCommandModel.ProtocolModel.EndTime = DateTime.Now;
      var fullFilePath = ProtocolModel.GetPathProtocol(command.OkCommandModel.ProtocolModel);
      EventAggregator.RaiseViewProtocol(fullFilePath);
    }
  }
}
