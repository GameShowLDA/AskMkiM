using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlCommandAnalyser.Model;
using Utilities.Models;

namespace ControlCommandAnalyser.Parser
{
  internal class CommandPostAnalyzer
  {
    /// <summary>
    /// Выполняет проверку всех команд и при необходимости добавляет ошибки в модели.
    /// </summary>
    /// <param name="models">Список разобранных команд.</param>
    public static void Analyze(List<BaseCommandModel> models)
    {
      if (models.Count == 0)
        return;

      CheckStartAndEnd(models);
      CheckUniqueMnemonics(models);
    }

    /// <summary>
    /// Проверяет, что первая команда — ОК, а последняя — КЦ. В противном случае добавляет ошибки.
    /// </summary>
    /// <param name="models">Список разобранных команд.</param>
    private static void CheckStartAndEnd(List<BaseCommandModel> models)
    {
      var first = models[0];
      var last = models[^1];

      if (!string.Equals(first.Mnemonic, "ОК", System.StringComparison.OrdinalIgnoreCase))
      {
        first.Errors.Add(new ErrorItem
        {
          LineNumber = first.StartLineNumber,
          Command = $"{first.CommandNumber} {first.Mnemonic}",
          Description = "Первая команда должна быть ОК"
        });
      }

      if (!string.Equals(last.Mnemonic, "КЦ", System.StringComparison.OrdinalIgnoreCase))
      {
        last.Errors.Add(new ErrorItem
        {
          LineNumber = last.StartLineNumber,
          Command = $"{last.CommandNumber} {last.Mnemonic}",
          Description = "Последняя команда должна быть КЦ"
        });
      }
    }

    /// <summary>
    /// Проверяет, что указанные команды присутствуют строго по одному разу.
    /// </summary>
    private static void CheckUniqueMnemonics(List<BaseCommandModel> models)
    {
      // Мнемоники, которые должны быть строго один раз
      string[] uniqueMnemonics = { "ОК", "РМ", "СП", "КЦ" };

      foreach (var mnemonic in uniqueMnemonics)
      {
        var matches = models
          .Where(m => string.Equals(m.Mnemonic, mnemonic, StringComparison.OrdinalIgnoreCase))
          .ToList();

        if (matches.Count == 0)
        {
          var first = models[0];
          if (mnemonic != "СП")
          {
            first.Errors.Add(new ErrorItem
            {
              LineNumber = first.StartLineNumber,
              Command = $"{first.CommandNumber} {first.Mnemonic}",
              Description = $"Команда {mnemonic} должна присутствовать в программе"
            });
          }
        }
        else if (matches.Count > 1)
        {
          foreach (var duplicate in matches.Skip(1))
          {
            duplicate.Errors.Add(new ErrorItem
            {
              LineNumber = duplicate.StartLineNumber,
              Command = $"{duplicate.CommandNumber} {duplicate.Mnemonic}",
              Description = $"Команда {mnemonic} должна быть только одна"
            });
          }
        }
      }
    }


  }
}
