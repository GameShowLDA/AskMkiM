using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;

namespace ControlCommandAnalyser.Model
{
  public class CommandsModel
  {
    public static List<BaseCommandModel> CommandModels = new();
    public static List<string> CheckCommandMnemonic = new List<string> { "ПР", "ПИ", "СИ", "ПЭ", "ЭТ" };
    /// <summary>
    /// Получает последнюю команду из списка команд проверки.
    /// </summary>
    /// <returns>Найденную команду или null.</returns>
    public static BaseCommandModel GetLastFromCheckCommands()
    {
      return CommandModels.Where(comand => CheckCommandMnemonic.Contains(comand.Mnemonic)).ToList().Last();
    }

    public static BaseCommandModel GetLast()
    {
      if (CommandModels.Count > 0)
      {
        return CommandModels[CommandModels.Count - 1];
      }
      else
      {
        return null;
      }
    }

    /// <summary>
    /// Получает модель команды по заданному номеру.
    /// </summary>
    /// <param name="comandNumber">Номер искомой команды.</param>
    /// <returns>Модель найденной команды или null.</returns>
    public static BaseCommandModel GetByComandNumber(string comandNumber)
    {
      return CommandModels.FirstOrDefault(x => x.CommandNumber == comandNumber);
    }

    /// <summary>
    /// Получает список моделей команд по указанной мнемонике.
    /// </summary>
    /// <param name="mnemonic">Мнемоника искомой команды.</param>
    /// <returns>Список найденных моделей команд.</returns>
    public static List<BaseCommandModel> GetByMnemonic(string mnemonic)
    {
      return CommandModels.Where(x => x.Mnemonic == mnemonic).ToList();
    }

    /// <summary>
    /// Статический метод для очистки списка моделей команд.
    /// </summary>
    public static void Clear()
    {
      CommandModels.Clear();
    }
  }
}
