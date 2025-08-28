using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ControlCommandAnalyser;
using ControlCommandAnalyser.Model;
using ControlCommandExecutor.Execution;

namespace ControlCommandExecutor.Executors
{
  public class UpCommandExecutor : ICommandExecutor
  {
    public string Mnemonic => "УП";


    public async Task ExecuteAsync(CommandExecutionContext context)
    {
      var up = (UpCommandModel)context.Command;

      if (CommandExecutionState.LastCuResult == MessageBoxResult.No)
      {
        context.JumpToCommandNumber?.Invoke(up.TargetLabel);
      }

      CommandExecutionState.LastCuResult = MessageBoxResult.None;
    }
  }
}
