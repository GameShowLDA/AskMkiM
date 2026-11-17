using Ask.Core.Shared.DTO.Protocol;

namespace Ask.Engine.ControlCommandExecutor.Execution
{
  /// <summary>
  /// Выполняет завершающее действие после отметки команды как завершённой.
  /// </summary>
  internal interface ICommandCompletionHandler
  {
    /// <summary>
    /// Выполняет завершающее действие команды.
    /// </summary>
    /// <param name="context">Контекст завершённой команды.</param>
    /// <param name="protocolModel">Модель итогового протокола.</param>
    /// <returns>Задача завершающего действия.</returns>
    Task OnCommandCompletedAsync(CommandExecutionContext context, ProtocolModel protocolModel);
  }
}
