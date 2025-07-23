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
  public class CuCommandExecutor : ICommandExecutor
  {
    public string Mnemonic => "ЦУ";

    public async Task ExecuteAsync(CommandExecutionContext context)
    {
      var cu = (CuCommandModel)context.Command;
      if (cu.CuType == CuCommandType.Information)
      {
        // Просто выводим сообщение (информация)
        MessageBox.Show(cu.MessageText, "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        CommandExecutionState.LastCuResult = MessageBoxResult.OK;
      }
      else if (cu.CuType == CuCommandType.Question)
      {
        // Вопрос — вызываем с кнопками Yes/No/Esc (или Ok/Cancel если Run/Esc)
        var result = MessageBox.Show(
            cu.MessageText,
            "Вопрос",
            MessageBoxButton.YesNoCancel, MessageBoxImage.Question
        );
        CommandExecutionState.LastCuResult = result;
      }
    }
  }
}
