using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandExecutor.Execution
{
  /// <summary>
  /// Базовый класс исполнителей команд.
  /// Содержит вспомогательные методы, используемые конкретными исполнителями команд при выполнении управляющей программы.
  /// </summary>
  internal abstract class CommandExecutorBase
  {
    /// <summary>
    /// Возвращает команду требуемого типа из контекста выполнения.
    /// </summary>
    /// <typeparam name="TCommand">
    /// Ожидаемый тип команды.
    /// </typeparam>
    /// <param name="context">
    /// Контекст выполнения команды.
    /// </param>
    /// <returns>
    /// Команда, приведённая к требуемому типу.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Выбрасывается, если тип команды в контексте
    /// не соответствует ожидаемому типу.
    /// </exception>
    protected static TCommand GetRequiredCommand<TCommand>(CommandExecutionContext context) where TCommand : BaseCommandModel
    {
      return context.Command as TCommand
       ?? throw new InvalidOperationException(
           $"Ожидалась команда типа {typeof(TCommand).Name}, " +
           $"но получена команда типа {context.Command.GetType().Name}.");
    }

    /// <summary>
    /// Устанавливает активную строку в редакторе для указанной команды.
    /// Используется для визуального отображения текущей выполняемой команды.
    /// </summary>
    /// <param name="context">
    /// Контекст выполнения команды.
    /// </param>
    /// <param name="command">
    /// Команда, строка которой должна быть выделена.
    /// </param>
    protected static void SetActiveLine(CommandExecutionContext context, BaseCommandModel command)
    {
      context.TranslationControl.SetActiveLine(command.FormattedStartLineNumber);
    }

    /// <summary>
    /// Завершает текущий заголовок команды в протоколе
    /// по факту наличия ошибок у конкретной команды.
    /// </summary>
    protected static async Task CompleteProtocolCommandAsync(
      CommandExecutionContext context,
      ProtocolModel protocolModel,
      string commandKey)
    {
      bool hasErrors =
        protocolModel.Errors.TryGetValue(commandKey, out var errors) &&
        errors is { Count: > 0 };

      await context.Console.CompleteCommandAsync(hasErrors);
    }
  }
}
