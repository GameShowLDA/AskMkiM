using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;

namespace Ask.Core.Services.Translator
{
  public class MeasurementTypeCommandExtensions
  {
    /// <summary>
    /// Возвращает тип команды ПИ в зависимости от знака тока в теле команды.
    /// </summary>
    public static MeasurementTypeCommand ResolvePiBySign(bool isDcw)
    {
      return isDcw
          ? MeasurementTypeCommand.PI_DCW
          : MeasurementTypeCommand.PI_ACW;
    }
  }
}
