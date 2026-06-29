using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.HelperParserParametr;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors.Si
{
  /// <summary>
  /// Процессор параметра сопротивления для команды СИ.
  /// Извлекает значение сопротивления, выполняет проверку диапазона
  /// и записывает результат в модель.
  /// </summary>
  internal class SiResistanceProcessor : IParameterProcessor<SiCommandModel>
  {
    /// <summary>
    /// Выполняет разбор сопротивления и обновляет модель команды.
    /// </summary>
    /// <param name="model">Модель команды.</param>
    /// <param name="remainder">Оставшаяся часть строки команды.</param>
    /// <param name="ctx">Контекст парсинга параметров.</param>
    /// <returns>Строка без обработанного параметра.</returns>
    public string Process(SiCommandModel model, string remainder, ParameterContext ctx)
    {
      var info = EnumExtensions.GetDisplayInfo(MeasurementTypeCommand.SI);

      var (resistance, unit, rest) =
          CommonParameterParser.ResistanceParser.ParseResistance(remainder);

      double value;

      if (string.IsNullOrWhiteSpace(resistance))
      {
        value = 100;
        model.Warnings.Add(
            GeneralWarnings.DefaultResistainceLowLimit(
                model.StartLineNumber,
                $"{ctx.CommandNumber} {ctx.Mnemonic}",
                "100 МОм"));
      }
      else
      {
        value = UnitsConvertor.ConvertToMOhms(
            CommonParameterParser.ParseToDouble(resistance),
            unit);
      }

      if (value > info.UpperLimit || value < info.LowerLimit)
      {
        model.Errors.Add(
            SiErrors.ResistanceLimitsConflict(
                ctx.LineNumber,
                $"{ctx.CommandNumber} {ctx.Mnemonic}",
                "Сопротивление вне диапазона"));
      }

      model.Resistance = value;
      model.ResistanceUnit = "МОм";
      model.ResistanceSource = $"{MeasurementSourceValueFormatter.FormatValue(value)}<МОм";

      return rest;
    }
  }
}
