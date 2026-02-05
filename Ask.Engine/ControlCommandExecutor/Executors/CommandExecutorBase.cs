using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandExecutor.Execution;

namespace Ask.Engine.ControlCommandExecutor.Executors
{
  internal abstract class CommandExecutorBase
  {
    protected static TCommand GetRequiredCommand<TCommand>(CommandExecutionContext context) where TCommand : BaseCommandModel
    {
      return context.Command as TCommand
             ?? throw new InvalidOperationException($"Expected command type {typeof(TCommand).Name}, but got {context.Command.GetType().Name}.");
    }

    protected static void SetActiveLine(CommandExecutionContext context, BaseCommandModel command)
    {
      context.TranslationControl.SetActiveLine(command.FormattedStartLineNumber);
    }

    protected static string BuildSourceLinesMessage(BaseCommandModel command)
    {
      return command.SourceLines.Count == 0
          ? string.Empty
          : "\r\n  " + string.Join("\r\n  ", command.SourceLines);
    }
  }
}
