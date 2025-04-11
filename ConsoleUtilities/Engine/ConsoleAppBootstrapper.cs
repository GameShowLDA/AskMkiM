using System.Text;
using ConsoleUtilities.Commands;
using ConsoleUtilities.Core;
using ConsoleUtilities.Models;
using ConsoleUtilities.Services;
using Utilities;

namespace ConsoleUtilities.Engine
{
  /// <summary>
  /// Централизованная точка инициализации компонентов консольного приложения.
  /// </summary>
  public static class ConsoleAppBootstrapper
  {
    /// <summary>
    /// Собирает все зависимости и возвращает готовый экземпляр <see cref="ICommandHandler"/>.
    /// </summary>
    /// <returns>Инициализированный обработчик команд.</returns>
    public static ICommandHandler Build()
    {
      LoggerUtility.LogInformation("Logger initialized from bootstrapper");

      var logBuffer = new StringBuilder();
      Console.SetOut(new ConsoleWriter(Console.Out, logBuffer));

      IConsoleWriter console = new ConsoleWriterAdapter(logBuffer);
      var context = new CommandContext(console, logBuffer);

      // Список команд
      var commands = new List<ICommand>();

      // help нужно передать все команды, даже если позже
      var helpCommand = new HelpCommand(commands);
      commands.Add(helpCommand);

      // сюда добавляешь свои реальные команды
      commands.Add(new ExitCommand());
      commands.Add(new ClearCommand());
      commands.Add(new SaveLogCommand());
      commands.Add(new LogsCommand());
      commands.Add(new AdminCommand());
      commands.Add(new PowerSourceCalibrationEditorCommand());
      commands.Add(new DatabaseCommand());
      commands.Add(new SendCommand());
      // commands.Add(new ShowTableCommand());
      // и т.д.

      // Не забудь про UnknownCommand – он будет добавлен фабрикой по дефолту

      var factory = new CommandFactory(commands);

      return new CommandHandler(factory, context);
    }
  }
}
