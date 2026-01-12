using Ask.Core.Shared.DTO.Protocol;
using Ask.Engine.ControlCommandExecutor.Execution;

namespace Ask.Engine.ControlCommandExecutor.Executors.Interface
{
  /// <summary>
  /// Интерфейс исполнителя команды контроля.
  /// </summary>
  public interface ICommandExecutor
  {
    string Mnemonic { get; }

    /// <summary>
    /// Выполняет команду на основе предоставленного контекста.
    /// </summary>
    /// <param name="context">Контекст выполнения команды.</param>
    /// <returns>Задача выполнения.</returns>
    Task ExecuteAsync(CommandExecutionContext context, ProtocolModel protocolModel);
  }
}
