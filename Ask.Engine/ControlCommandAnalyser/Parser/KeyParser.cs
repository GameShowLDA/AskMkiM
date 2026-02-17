using Ask.Core.Services.Errors.Translation;
using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using Ask.Engine.ControlCommandAnalyser.Model;
using System.Text.RegularExpressions;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser
{
  public static class KeyParser
  {
    public static string ParseKeys(int numberLine, BaseCommandModel model, string remainder)
    {
      var result = AlgorithmKeyParser.ExtractKeysWithTrailingCommaCheck(remainder, model);

      foreach (var (key, hasError) in result)
      {
        if (hasError)
        {
          model.Errors.Add(GeneralErrors.WrongKey(numberLine, model.Mnemonic, $"{model.CommandNumber} {model.Mnemonic}", key));
        }
        else
        {
          var type = model.GetType();
          var attribute = type.GetCustomAttributes(typeof(AllowedKeysAttribute), false)
              .FirstOrDefault() as AllowedKeysAttribute;
          if (!model.AlgorithmKey.Contains(key))
          {
            if (attribute.Keys.Where(item => item.ToString() == key).Count() == 1)
            {
              model.AlgorithmKey.Add(key);
              LogDebug($"Найден ключ алгоритма: {key}");
            }
            else
            {
              model.Errors.Add(GeneralErrors.WrongKey(numberLine, model.Mnemonic, $"{model.CommandNumber} {model.Mnemonic}", key));
            }
          }
          else
          {
            model.Warnings.Add(GeneralWarnings.DuplicateKey(numberLine, $"{model.CommandNumber} {model.Mnemonic}", key));
          }
        }
      }

      foreach (var (key, hasError) in result)
      {
        remainder = Regex.Replace(
        remainder,
        $@"\b{Regex.Escape(key)}\s*,?",
        "",
        RegexOptions.IgnoreCase);
      }

      return remainder;
    }
  }
}
