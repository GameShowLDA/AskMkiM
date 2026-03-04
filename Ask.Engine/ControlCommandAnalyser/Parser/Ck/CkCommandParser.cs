using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Common;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers;


namespace Ask.Engine.ControlCommandAnalyser.Parser.Ck
{
  /// <summary>
  /// Парсер организационной команды CK.
  /// </summary>
  internal class CkCommandParser : CommandParserBase<CkCommandModel>
  {
    /// <summary>
    /// Проверяет, поддерживает ли парсер указанную мнемонику.
    /// </summary>
    public override bool CanParse(MnemonicIdentifier mnemonic)
      => mnemonic.Mnemonic.MatchesEnum(OrganizationalComands.CK);

    /// <summary>
    /// Создаёт и инициализирует модель команды для разбора CK.
    /// </summary>
    protected override CkCommandModel CreateModel(string commandNumber, int numberLine, List<string> lines) => new()
    {
      CommandNumber = commandNumber,
      SourceLines = lines is null ? new List<string>() : new List<string>(lines),
      StartLineNumber = numberLine,
    };

    /// <summary>
    /// Разбирает параметры CK и извлекает ключи алгоритма из остатка строки.
    /// </summary>
    protected override string ParseParameters(CkCommandModel model, string remainder, ParameterContext ctx, List<string> lines)
      => KeyParser.ParseKeys(model.StartLineNumber, model, remainder);

    /// <summary>
    /// Разбирает структуру шин CK из оставшегося текста команды.
    /// </summary>
    protected override void ParseStructure(
      CkCommandModel model,
      RmCommandModel rmCommandModel,
      string commandNumber,
      string mnemonic,
      int numberLine,
      List<string> lines,
      ref string remainder)
      => model.BusList = SchemeManager.GetBusList(model, rmCommandModel, numberLine, ref remainder);

    /// <summary>
    /// Обрабатывает неразобранный хвост как ошибки валидации для CK.
    /// </summary>
    protected override void HandleUnparsed(CkCommandModel model, int numberLine, string remainder)
      => UnparsedParametersManager.HandleUnparsedParameters(model, numberLine, remainder);
  }
}
