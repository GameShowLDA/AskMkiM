using Ask.Core.Services.Errors.Translation;
using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model.Ks;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.HelperParserParametr;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors.Ks
{
  /// <summary>
  /// Процессор параметров сопротивления для команды КС.
  /// Извлекает диапазон сопротивления из строки и применяет его к модели.
  /// </summary>
  internal class KsResistanceProcessor : IParameterProcessor<KsCommandModel>
  {
    /// <summary>
    /// Выполняет разбор параметров сопротивления и обновляет модель команды.
    /// </summary>
    /// <param name="model">Модель команды.</param>
    /// <param name="remainder">Оставшаяся часть строки команды.</param>
    /// <param name="ctx">Контекст парсинга параметров.</param>
    /// <returns>Строка без обработанных параметров.</returns>
    public string Process(KsCommandModel model, string remainder, ParameterContext ctx)
    {
      string lowerRaw, higherRaw, unit;

      (lowerRaw, higherRaw, unit, remainder) =
          CommonParameterParser.ResistanceParser.ParseResistanceRange(remainder);

      LogDebug($"После парсинга сопротивления: нижняя='{lowerRaw}', верхняя='{higherRaw}', единица='{unit}', remainder='{remainder}'");

      if (string.IsNullOrEmpty(lowerRaw) && string.IsNullOrEmpty(higherRaw))
      {
        model.Errors.Add(KsErrors.EmptyResistance(ctx.LineNumber, $"{ctx.CommandNumber} {ctx.Mnemonic}"));
        LogWarning($"Не указано сопротивление (строка {ctx.LineNumber}): {ctx.CommandNumber} {ctx.Mnemonic}");
        return remainder;
      }

      var meter = ctx.Fastmeter;
      if (meter == null)
      {
        LogError("Не найден быстрый измеритель.");
        model.Errors.Add(GeneralErrors.FastMeterNotFound(ctx.LineNumber, $"{ctx.CommandNumber} {ctx.Mnemonic}"));
        return remainder;
      }

      ResistanceManager.ProcessResistance(model, lowerRaw, higherRaw, unit, ctx.CommandNumber, ctx.Mnemonic, ctx.LineNumber);

      return remainder;
    }
  }
}
