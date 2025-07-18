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
  internal class CpCommandExecutor : ICommandExecutor
  {
    public string Mnemonic => "СП";

    public Task ExecuteAsync(CommandExecutionContext context)
    {
      var command = context.Command as CpCommandModel;
      context.TranslationControl.SetActiveLine(command.FormattedStartLineNumber);

      return Task.CompletedTask;
    }
  }
}
