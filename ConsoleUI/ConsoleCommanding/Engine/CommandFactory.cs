using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleUI.ConsoleCommanding.Commands;
using ConsoleUI.ConsoleCommanding.Core;

namespace ConsoleUI.ConsoleCommanding.Engine
{
  public class CommandFactory
  {
    public List<ICommand> CreateAll(IConsoleWriter writer)
    {
      var commands = new List<ICommand>();

      // Создаём команды с writer (если им нужно)
      var context = new CommandContext(writer);

      commands.Add(new AdminCommand());
      commands.Add(new AddLogCommand());
      commands.Add(new ClearCommand());
      commands.Add(new DelAdminCommand());
      commands.Add(new EchoCommand());
      commands.Add(new ExitCommand());
      commands.Add(new LogsCommand());
      commands.Add(new UnknownCommand());

      // Help — получает список всех команд
      commands.Add(new HelpCommand(commands));

      return commands;
    }
  }
}
