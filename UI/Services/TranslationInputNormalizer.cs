using Ask.Core.Services.FileFormats;
using Ask.Engine.ControlCommandAnalyser;
using System.Text.RegularExpressions;

namespace UI.Services
{
  /// <summary>
  /// Подготавливает копию исходного текста для трансляции, не изменяя документ редактора.
  /// </summary>
  internal static class TranslationInputNormalizer
  {
    private static readonly Regex CommandHeaderRegex = new(@"^\s*\d+\s+\S+", RegexOptions.Compiled);

    /// <summary>
    /// Нормализует управляющие символы, мнемоники и отступы во входе транслятора.
    /// </summary>
    /// <param name="sourceText">Исходное содержимое документа.</param>
    /// <returns>Подготовленный текст для трансляции.</returns>
    internal static string Normalize(string? sourceText)
    {
      var text = TextSanitizer.RemoveLegacyControlChars(sourceText ?? string.Empty);
      text = CommandTranslationManager.NormalizeCommandMnemonics(text);
      return NormalizeIndentation(text);
    }

    private static string NormalizeIndentation(string sourceText)
    {
      if (string.IsNullOrEmpty(sourceText))
      {
        return sourceText;
      }

      var lines = sourceText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
      bool insideCommand = false;

      for (int i = 0; i < lines.Length; i++)
      {
        var line = lines[i].Replace('\t', ' ');
        if (string.IsNullOrWhiteSpace(line))
        {
          lines[i] = line;
          continue;
        }

        var trimmedStart = line.TrimStart();
        if (CommandHeaderRegex.IsMatch(line))
        {
          insideCommand = true;
          lines[i] = trimmedStart;
          continue;
        }

        if (!insideCommand || char.IsWhiteSpace(line[0]) || line.Trim() == "*")
        {
          lines[i] = line;
          continue;
        }

        lines[i] = $" {line}";
      }

      return string.Join("\r\n", lines);
    }
  }
}
