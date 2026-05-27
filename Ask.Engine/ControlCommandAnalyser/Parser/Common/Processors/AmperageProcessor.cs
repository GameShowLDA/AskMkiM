using Ask.Core.Services.Errors.Translation;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.HelperParserParametr;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors
{
  /// <summary>
  /// Процессор параметра силы тока.
  /// Извлекает значение тока из строки и передаёт остаток дальше по конвейеру.
  /// </summary>
  internal class AmperageProcessor<TModel> : IParameterProcessor<TModel>
    where TModel : IHasAmperage, IError
  {
    /// <summary>
    /// Выполняет разбор параметра силы тока из строки команды.
    /// </summary>
    /// <param name="model">Модель команды.</param>
    /// <param name="remainder">Оставшаяся часть строки команды.</param>
    /// <param name="ctx">Контекст парсинга параметров.</param>
    /// <returns>Строка без обработанного параметра.</returns>
    public string Process(TModel model, string remainder, ParameterContext ctx)
    {
      var (amperageRaw, unitRaw, rest) =
          CommonParameterParser.AmperageParser.ParseAmperage(remainder);

      if (!string.IsNullOrWhiteSpace(amperageRaw))
      {
        double value = CommonParameterParser.ParseToDouble(amperageRaw);

        if (model.HasAmperage)
        {
          model.Amperage = value;
          model.AmperageUnit = unitRaw;
          model.AmperageSource = $"{amperageRaw} {unitRaw}";

          LogDebug($"Распознана сила тока: {model.AmperageSource}");
        }
        else
        {
          if (model is BaseCommandModel baseCommand)
          {
            baseCommand.Warnings.Add(GeneralWarnings.IgnoreAmperage(baseCommand.StartLineNumber, $"{baseCommand.CommandNumber} {baseCommand.Mnemonic}"));
          }
          LogDebug($"Распознана сила тока: {amperageRaw}{unitRaw}");
        }
      }

      return rest;
    }
  }
}
