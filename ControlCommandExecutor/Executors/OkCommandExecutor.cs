using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
      var ok = context.Command as OkCommandModel;
      await context.Console.ShowMessageAsync(new Utilities.Models.ShowMessageModel($"Выполнение программы контроля для \"{ok.ObjectName}({ok.ObjectCode})\""));
    }
  }
}
