using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlCommandAnalyser.Model;
using ControlCommandAnalyser.Model.Ok;
using ControlCommandExecutor.Execution;

namespace ControlCommandExecutor.Executors
{
  /// <summary>
  /// Исполнитель команды "ОК".
  /// </summary>
  public class OkCommandExecutor : ICommandExecutor
  {
    public string Mnemonic => "ОК";

    public async Task ExecuteAsync(CommandExecutionContext context)
    {
      if (!await AppConfiguration.Execution.ExecutionConfig.GetIsIdleModeEnabled())
      {
        await NewCore.Communication.DeviceCommandSender.ResetAllSystem();
      }

      Utilities.ResultProtocol.ProtocolModel protocolModel = new Utilities.ResultProtocol.ProtocolModel();

      context.CommandExecutionManager.ClearErrorsMethod();

      var command = context.Command as OkCommandModel;
      context.TranslationControl.SetActiveLine(command.FormattedStartLineNumber);

      await context.Console.ShowMessageAsync(new Utilities.Models.ShowMessageModel($"Выполнение программы контроля для \"{command.ObjectName}({command.ObjectCode})\""), IsBlockStart: true);
    }
  }
}
