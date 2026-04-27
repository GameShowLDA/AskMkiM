using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Enums.UiEnums;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Engine.ControlCommandAnalyser;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandExecutor.Execution;
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

      var nameCommand = $"{command.CommandNumber} {command.Mnemonic}";
      var sourceMessage = BuildSourceLinesMessage(command);
      await context.Console.ShowMessageAsync(ExecutorMessageBuilder.BuildCommandExecutionMessage(nameCommand, sourceMessage));

      var isQuestion = command.CuType == CuCommandType.Question ||
                       command.MessageText.TrimEnd().EndsWith("?");

      if (!isQuestion)
      {
        // CommandExecutionState.LastCuResult = ShowInformation(command.MessageText);
        CommandExecutionState.LastRejectFlag = false;
        return;
      }

      var nextCommand = context.CommandExecutionManager.GetNextCommand(command);
      var questionResult = await AskQuestionWithSingleDialogAsync(context.Console, command.MessageText);

      CommandExecutionState.LastCuResult = questionResult;
      CommandExecutionState.LastRejectFlag = nextCommand is UpCommandModel &&
                                             questionResult == MessageBoxResult.No;
    }

    private static MessageBoxResult ShowInformation(string message)
    {
      MessageBoxCustom.Show(message, "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
      return MessageBoxResult.OK;
    }

    private static async Task<MessageBoxResult> AskQuestionWithSingleDialogAsync(IUserInteractionService userInteractionService, string message)
    {
      while (true)
      {
        var result = MessageBoxCustom.Show(
          $"{message}\r\n\r\nКоманда ЦУ: Yes-Да No-Нет Esc-Временный останов ПК",
          "Запрос оператору",
          MessageBoxButton.YesNoCancel,
          MessageBoxImage.Question);

        if (result != MessageBoxResult.Cancel)
        {
          return result;
        }

        if (!await WaitForTemporaryResumeAsync(userInteractionService))
        {
          throw new OperationCanceledException("Выполнение остановлено оператором на команде ЦУ.");
        }
      }
    }

    private static async Task<bool> WaitForTemporaryResumeAsync(IUserInteractionService userInteractionService)
    {
      var action = await userInteractionService.WaitUserActionAsync(deviceTask: true);
      return action is UserAction.Continue or UserAction.Retry or UserAction.None;
    }
  }
}
