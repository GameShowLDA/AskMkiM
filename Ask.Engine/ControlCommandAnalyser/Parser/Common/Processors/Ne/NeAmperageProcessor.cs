using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.HelperParserParametr;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors.Ne
{
  /// <summary>
  /// Процессор параметра силы тока для команды НЭ.
  /// Извлекает значение тока из строки и передаёт остаток дальше по конвейеру.
  /// </summary>
  internal class NeAmperageProcessor : IParameterProcessor<NeCommandModel>
  {
    /// <summary>
    /// Выполняет разбор параметра силы тока из строки команды.
    /// </summary>
    /// <param name="model">Модель команды.</param>
    /// <param name="remainder">Оставшаяся часть строки команды.</param>
    /// <param name="ctx">Контекст парсинга параметров.</param>
    /// <returns>Строка без обработанного параметра.</returns>
    public string Process(NeCommandModel model, string remainder, ParameterContext ctx)
    {
      var (amperageRaw, unitRaw, rest) =
          CommonParameterParser.AmperageParser.ParseAmperage(remainder);

      if (!string.IsNullOrWhiteSpace(amperageRaw))
      {
        double value = CommonParameterParser.ParseToDouble(amperageRaw);

        //model.Amperage = value;
        //model.AmperageSource = $"{amperageRaw}{unitRaw}";

        //LogDebug($"Распознана сила тока: {model.AmperageSource}");
        LogDebug($"Распознана сила тока: {amperageRaw}{unitRaw}");
      }

      return rest;
    }
  }
}
