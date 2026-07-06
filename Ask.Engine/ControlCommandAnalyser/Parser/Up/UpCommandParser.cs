using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Up
{
  /// <summary>
  /// Парсер команды УП (условный переход).
  /// <para>
  /// Извлекает метку перехода из текста команды,
  /// формирует модель и выполняет базовую валидацию.
  /// </para>
  /// </summary>
  public class UpCommandParser : ICommandParser
  {
    /// <summary>
    /// Определяет, может ли данный парсер обработать команду
    /// с указанной мнемоникой.
    /// </summary>
    /// <param name="mnemonic">Идентификатор мнемоники команды.</param>
    /// <returns>
    /// true — если мнемоника соответствует команде УП;  
    /// false — если команда должна быть обработана другим парсером.
    /// </returns>
    public bool CanParse(MnemonicIdentifier mnemonic)
    => mnemonic.Mnemonic.MatchesEnum(OrganizationalComands.UP);

    /// <summary>
    /// Выполняет разбор команды условного перехода.
    /// <para>
    /// Алгоритм:
    /// <list type="number">
    /// <item><description>Определяет метку перехода из первой или второй строки.</description></item>
    /// <item><description>Создаёт модель команды.</description></item>
    /// <item><description>Удаляет комментарии и пустые строки.</description></item>
    /// <item><description>Проверяет корректность метки перехода.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <param name="commandNumber">Номер команды.</param>
    /// <param name="mnemonic">Мнемоника команды.</param>
    /// <param name="numberLine">Номер строки начала команды.</param>
    /// <param name="lines">Исходные строки команды.</param>
    /// <returns>
    /// Заполненная модель <see cref="UpCommandModel"/>,
    /// содержащая метку перехода и возможные ошибки.
    /// </returns>
    public BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines)
    {
      var firstLine = lines[0].Trim();

      var parts = firstLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

      string targetLabel = null;

      if (parts.Length >= 3)
      {
        targetLabel = parts[2];
      }
      else if (parts.Length == 2 && lines.Count > 1)
      {
        targetLabel = lines[1].Trim();
      }
      var model = new UpCommandModel();
      if (lines == null || lines.Count == 0)
      {
        model = new UpCommandModel
        {
          CommandNumber = commandNumber,
          StartLineNumber = numberLine,
          SourceLines = new List<string>()
        };

        model.Errors.Add(
            UpErrors.MissingOrInvalidLabel(
                numberLine,
                $"{commandNumber} {mnemonic}"));

        return model;
      }

      model = new UpCommandModel
      {
        CommandNumber = commandNumber,
        StartLineNumber = numberLine,
        SourceLines = new List<string>(lines),
        TargetLabel = targetLabel
      };

      List<string> processedLines = CommentsParser.ParseComments(lines, model);
      model.SourceLines = model.SourceLines
        .Where(l => !string.IsNullOrWhiteSpace(l))
        .ToList();

      // Валидация
      if (string.IsNullOrWhiteSpace(targetLabel))
      {
        model.Errors.Add(UpErrors.MissingOrInvalidLabel(numberLine, $"{commandNumber} {mnemonic}"));
      }

      // Валидация
      if (string.IsNullOrWhiteSpace(targetLabel))
      {
        model.Errors.Add(
          UpErrors.MissingOrInvalidLabel(
            numberLine,
            $"{commandNumber} {mnemonic}"));
      }
      else
      {
        if (!int.TryParse(targetLabel, out int targetCommand))
        {
          model.Errors.Add(
            UpErrors.MissingOrInvalidLabel(
              numberLine,
              $"{commandNumber} {mnemonic}"));
        }
        else if (targetLabel == commandNumber)
        {
          model.Errors.Add(
            UpErrors.SelfReferenceJump(
              numberLine,
              $"{commandNumber} {mnemonic}"));
        }
      }

      return model;
    }
  }
}
