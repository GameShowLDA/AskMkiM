using ConsoleUtilities.Core;
using ConsoleUtilities.Models;
using ConsoleUtilities.Engine;

namespace ConsoleUtilities.Engine
{
  /// <summary>
  /// Обрабатывает текстовый ввод пользователя и выполняет соответствующую команду.
  /// </summary>
  public class CommandHandler : ICommandHandler
  {
    private readonly CommandFactory _factory;
    private readonly CommandContext _context;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="CommandHandler"/>.
    /// </summary>
    /// <param name="factory">Фабрика команд для разрешения по имени.</param>
    /// <param name="context">Контекст выполнения команды.</param>
    public CommandHandler(CommandFactory factory, CommandContext context)
    {
      _factory = factory;
      _context = context;
    }

    /// <inheritdoc />
    public async Task HandleInputAsync(string input)
    {
      if (string.IsNullOrWhiteSpace(input))
        return;

      string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
      string commandName = parts[0];
      string[] args = parts.Skip(1).ToArray();

      ICommand command = _factory.GetCommand(commandName);
      await command.ExecuteAsync(args, _context);
    }
  }
}
