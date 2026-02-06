using Ask.Core.Shared.DTO.Protocol;

namespace Ask.Engine.ControlCommandExecutor.Execution
{
  /// <summary>
  /// Интерфейс исполнителя команды контроля.
  /// </summary>
  public interface ICommandExecutor
  {
    /// <summary>
    /// Мнемоника команды, которую обрабатывает данный исполнитель.
    /// Используется для сопоставления команды с соответствующим исполнителем.
    /// </summary>
    string Mnemonic { get; }

    /// <summary>
    /// Выполняет команду на основе предоставленного контекста.
    /// </summary>
    /// <param name="context">Контекст выполнения команды.</param>
    /// <returns>Задача выполнения.</returns>
    Task ExecuteAsync(CommandExecutionContext context, ProtocolModel protocolModel);
  }
}
