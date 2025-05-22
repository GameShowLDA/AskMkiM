using System;
using System.Linq;
using ControlCommandAnalyser.Domain;
using ControlCommandAnalyser.Parsing;
using static Utilities.LoggerUtility;

namespace ControlCommandAnalyser.Translation
{
  /// <summary>
  /// Отвечает за трансляцию текста ПК в команды.
  /// </summary>
  public class TranslationManager
  {
    /// <summary>
    /// Выполняет трансляцию входного текста и выводит результат в лог (пока консоль).
    /// </summary>
    /// <param name="text">Весь текст ПК-файла.</param>
    public async Task Translate(string text)
    {
      if (string.IsNullOrWhiteSpace(text))
      {
        LogError("Пустой входной текст для трансляции.");
        return;
      }

      var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

      var splitter = new CommandSplitter();
      var blocks = splitter.Split(lines);

      var recognizer = new CommandRecognizer();

      await recognizer.RecognizeAsync(blocks);

      LogInformation($"✅ Трансляция завершена. Обработано команд: {blocks.Count}");
    }
  }
}
