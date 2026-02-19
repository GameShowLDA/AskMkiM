using Ask.Core.Services.Errors.Translation;
using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model.Ie;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.HelperParserParametr;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors.Ie
{
  /// <summary>
  /// Процессор параметров электрической ёмкости для команды IE.
  /// Извлекает диапазон ёмкости из строки и применяет его к модели.
  /// </summary>
  internal class IeCapacityProcessor : IParameterProcessor<IeCommandModel>
  {
    /// <summary>
    /// Выполняет разбор параметров ёмкости и обновляет модель команды.
    /// </summary>
    /// <param name="model">Модель команды.</param>
    /// <param name="remainder">Оставшаяся часть строки команды.</param>
    /// <param name="ctx">Контекст парсинга параметров.</param>
    /// <returns>Строка без обработанных параметров.</returns>
    public string Process(IeCommandModel model, string remainder, ParameterContext ctx)
    {
      string lower, higher, unit;

      (lower, higher, unit, remainder) = CommonParameterParser.CapacityParser.ParseCapacityRange(remainder);

      LogDebug(
            $"После парсинга электрической ёмкости: нижняя='{lower}', " +
            $"верхняя='{higher}', единица='{unit}', remainder='{remainder}'");

      if (string.IsNullOrEmpty(lower))
      {
        model.Errors.Add(
            IeErrors.EmptyLowerCapacity(
                ctx.LineNumber,
                $"{ctx.CommandNumber} {ctx.Mnemonic}"));

        LogWarning(
            $"Не указана нижняя граница электрической емкости (строка {ctx.LineNumber}): " +
            $"{ctx.CommandNumber} {ctx.Mnemonic}");

        return remainder;
      }

      CapacityManager.ProcessCapacity(model, lower, higher, unit, ctx.LineNumber, ctx.CommandNumber, ctx.Mnemonic);

      return remainder;
    }
  }
}
