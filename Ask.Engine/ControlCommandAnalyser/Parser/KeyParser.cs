using Ask.Core.Services.Errors.Translation;
using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using Ask.Engine.ControlCommandAnalyser.Model;
using System.Text.RegularExpressions;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser
{
  /// <summary>
  /// Выполняет извлечение и обработку ключей алгоритма из текста команды.
  /// <para>
  /// Ключи проверяются на допустимость, дубликаты и корректность записи.
  /// При необходимости добавляются ошибки и предупрежeдения в модель команды.
  /// </para>
  /// </summary>
  public static class KeyParser
  {
    /// <summary>
    /// Извлекает ключи алгоритма из строки и обновляет модель команды.
    /// </summary>
    /// <param name="numberLine">Номер строки, в которой выполняется парсинг.</param>
    /// <param name="model">Модель команды, в которую добавляются найденные ключи.</param>
    /// <param name="remainder">Оставшаяся часть строки команды.</param>
    /// <returns>
    /// Строка без найденных ключей алгоритма.
    /// </returns>
    /// <remarks>
    /// Выполняет:
    /// <list type="number">
    /// <item><description>получение ключей через <see cref="AlgorithmKeyParser"/>;</description></item>
    /// <item><description>проверку допустимости по атрибуту <see cref="AllowedKeysAttribute"/>;</description></item>
    /// <item><description>добавление ошибок для недопустимых ключей;</description></item>
    /// <item><description>добавление предупреждений для дубликатов;</description></item>
    /// <item><description>перенос ключей из вложенной команды СИ в ПИ (если требуется);</description></item>
    /// <item><description>удаление ключей из текста команды.</description></item>
    /// </list>
    /// </remarks>
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
      if (model is PiCommandModel piCommandModel)
      {
        if (piCommandModel.AlgorithmKey.Count == 0
            && piCommandModel.SiCommand.AlgorithmKey.Count != 0
            && piCommandModel.AlgorithmKey != null
            && piCommandModel.SiCommand.AlgorithmKey != null)
        {
          var type = piCommandModel.GetType();
          var attribute = type.GetCustomAttributes(typeof(AllowedKeysAttribute), false)
                          .FirstOrDefault() as AllowedKeysAttribute;
          foreach (var key in piCommandModel.SiCommand.AlgorithmKey)
          {
            if (!piCommandModel.AlgorithmKey.Contains(key) && attribute.Keys.Where(item => item.ToString() == key).Count() == 1)
            {
              piCommandModel.AlgorithmKey.Add(key);
            }
          }
          model = piCommandModel;
        }
      }

      foreach (var (key, hasError) in result)
      {
        remainder = Regex.Replace(
        remainder,
        $@"(?<![\p{{L}}\p{{N}}/]){Regex.Escape(key)}(?![\p{{L}}\p{{N}}/])\s*,?",
        "",
        RegexOptions.IgnoreCase);
      }

      return remainder;
    }
  }
}
