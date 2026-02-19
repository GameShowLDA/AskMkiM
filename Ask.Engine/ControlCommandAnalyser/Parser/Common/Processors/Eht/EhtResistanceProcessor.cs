using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.HelperParserParametr;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors.Eht
{
  /// <summary>
  /// Процессор параметров сопротивления для команды ЭХТ.
  /// Извлекает диапазон сопротивления и сопротивление кабеля,
  /// затем применяет их к модели.
  /// </summary>
  public class EhtResistanceProcessor : IParameterProcessor<EhtCommandModel>
  {
    /// <summary>
    /// Выполняет разбор параметров сопротивления из строки команды.
    /// </summary>
    /// <param name="model">Модель команды.</param>
    /// <param name="remainder">Оставшаяся часть строки команды.</param>
    /// <param name="ctx">Контекст парсинга параметров.</param>
    /// <returns>Строка без обработанных параметров.</returns>
    public string Process(EhtCommandModel model, string remainder, ParameterContext ctx)
    {
      string lower, higher, unit;
      string cabel, cabelUnit;

      (lower, higher, unit, remainder) = CommonParameterParser.ResistanceParser.ParseResistanceRangeWithR(remainder);

      (cabel, cabelUnit, remainder) = CommonParameterParser.ResistanceParser.ParseCabelResistance(remainder);

      ResistanceManager.ProcessResistance(model, lower, higher, cabel, unit, cabelUnit, ctx.CommandNumber, ctx.Mnemonic, ctx.LineNumber);

      return remainder;
    }
  }
}
