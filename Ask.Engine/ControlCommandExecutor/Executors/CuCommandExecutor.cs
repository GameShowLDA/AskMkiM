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
  internal class CuCommandExecutor : CommandExecutorBase, ICommandExecutor
  {
    public string Mnemonic => EnumExtensions.GetDisplayOrganizationalInfo(OrganizationalComands.CU).DisplayName;

    public async Task ExecuteAsync(CommandExecutionContext context, ProtocolModel protocolModel)
    {
      var command = GetRequiredCommand<CuCommandModel>(context);
      SetActiveLine(context, command);

      CommandExecutionState.LastCuResult = command.CuType switch
      {
        CuCommandType.Information => ShowInformation(command.MessageText),
        CuCommandType.Question => AskQuestion(command.MessageText),
        _ => MessageBoxResult.None
      };
    }

    private static MessageBoxResult ShowInformation(string message)
    {
      MessageBoxCustom.Show(message, "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
      return MessageBoxResult.OK;
    }

    private static MessageBoxResult AskQuestion(string message)
    {
      return MessageBoxCustom.Show(message, "Запрос оператору", MessageBoxButton.YesNo, MessageBoxImage.Question);
    }
  }
}
