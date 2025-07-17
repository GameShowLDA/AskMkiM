using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlCommandExecutor.Execution;

namespace ControlCommandExecutor.Executors
{
  internal class CpCommandExecutor : ICommandExecutor
  {
    public string Mnemonic => "СП";

    public Task ExecuteAsync(CommandExecutionContext context)
    {
      return Task.CompletedTask;
    }
  }
}
