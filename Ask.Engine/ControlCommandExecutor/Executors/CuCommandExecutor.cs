using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandExecutor.Execution;
using Ask.Engine.ControlCommandExecutor.Executors.Interface;
using Message;
using System.Windows;

namespace Ask.Engine.ControlCommandExecutor.Executors
{
  public class CuCommandExecutor : ICommandExecutor
  {
    public string Mnemonic => EnumExtensions.GetDisplayOrganizationalInfo(OrganizationalComands.CU).DisplayName;

    public async Task ExecuteAsync(CommandExecutionContext context, ProtocolModel protocolModel)
    {
      var command = (CuCommandModel)context.Command;
      if (command.CuType == CuCommandType.Information)
      {
        MessageBoxCustom.Show(command.MessageText, "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        CommandExecutionState.LastCuResult = MessageBoxResult.OK;
      }
      else if (command.CuType == CuCommandType.Question)
      {
        // Вопрос — вызываем с кнопками Yes/No/Esc (или Ok/Cancel если Run/Esc)
        var result = MessageBoxCustom.Show(
            command.MessageText,
            "Запрос оператору",
            MessageBoxButton.YesNo, MessageBoxImage.Question
        );
        CommandExecutionState.LastCuResult = result;
      }
    }
  }
}
