using System;
using System.Collections.Generic;
using System.Linq;
using ControlCommandAnalyser.Model;
using Utilities.Errors;
using Utilities.Models;
using AppConfiguration.Error.Translation;

namespace ControlCommandAnalyser
{
  public  enum AlgorithmKey
  {
    ЗР, ЗС, С, П, И, Г, Т1, Ш, Т, Б, К, Н, Д
  }

  [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
  public sealed class AllowedKeysAttribute : Attribute
  {
    public AlgorithmKey[] Keys { get; }

    public AllowedKeysAttribute(params AlgorithmKey[] keys)
    {
      Keys = keys;
    }

    /// <summary>
    /// Проверяет ключи алгоритма, указанные в команде, и при ошибках записывает их в Errors.
    /// </summary>
    /// <param name="command">Команда, для которой нужно проверить ключи.</param>
    public static void ValidateKeysAndAttachErrors(BaseCommandModel command)
    {
      var type = command.GetType();
      var attribute = type.GetCustomAttributes(typeof(AllowedKeysAttribute), false)
                          .FirstOrDefault() as AllowedKeysAttribute;

      if (attribute == null)
      {
        if (command.AlgorithmKey.Any())
        {
          command.Errors.Add(KeyErrors.NotExpected(command.StartLineNumber, command.Mnemonic));
        }
        return;
      }

      var allowed = attribute.Keys;

      foreach (var keyStr in command.AlgorithmKey)
      {
        if (!Enum.TryParse<AlgorithmKey>(keyStr, out var parsedKey))
        {
          command.Errors.Add(KeyErrors.NotRecognized(keyStr, command.StartLineNumber, command.Mnemonic));
          continue;
        }

        if (!allowed.Contains(parsedKey))
        {
          command.Errors.Add(KeyErrors.NotAllowed(keyStr, command.StartLineNumber, command.Mnemonic));
        }
      }

      // Пример проверки конфликта: ЗР и ЗС не могут быть вместе
      if (ContainsKeys(command.AlgorithmKey, "ЗР", "ЗС"))
      {
        command.Errors.Add(KeyErrors.Conflict("ЗР", "ЗС", command.StartLineNumber, command.Mnemonic));
      }
    }

    private static bool ContainsKeys(List<string> keys, string key1, string key2)
    {
      return keys.Contains(key1) && keys.Contains(key2);
    }
  }
}
