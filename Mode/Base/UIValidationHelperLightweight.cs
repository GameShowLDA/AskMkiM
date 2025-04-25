using Mode.Models;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using UI.Components;
using UI.Controls.Protocol;
using Utilities.Events;
using Utilities.Models;

namespace Mode.Base
{
  /// <summary>
  /// Помощник для валидации данных из InputFieldLightweight внутри ProtocolUI.
  /// </summary>
  public static class UIValidationHelperLightweight
  {
    static InputFieldLightweight? input;

    /// <summary>
    /// Извлекает и проверяет строки «тестируемого» и «тестирующего» номера и диапазона.
    /// </summary>
    /// <param name="protocolUI">Экземпляр ProtocolUI, в котором лежит InputFieldLightweight.</param>
    /// <returns>
    /// Кортеж: 
    /// Success – результат проверки; 
    /// Message – текст ошибки или "OK"; 
    /// Tested, Tester, Range – строковые значения полей (пустые при ошибке).
    /// </returns>
    public static (bool Success, string Message, string Tested, string Tester, string Range)
        TryValidateAndParseInput(ProtocolUI protocolUI)
    {
      // 1) Достаём контрол
      input = protocolUI.GetInputFieldLightweightSafe();
      if (input == null)
      {
        return (false, "Поле ввода не найдено.", "", "", "");
      }

      // 2) Безопасно читаем сырые строки
      var (tested, tester, range) = input.GetInputFieldLightweightValuesSafe();

      // 3) Проверка «тестируемого» номера (формат a.b)
      var t1 = tested.Trim();
      var t1Parts = t1.Split('.');
      if (t1Parts.Length != 2
       || string.IsNullOrWhiteSpace(t1Parts[0])
       || string.IsNullOrWhiteSpace(t1Parts[1]))
      {
        _ = protocolUI.ShowMessageAsync(
            new ShowMessageModel("Поле 'Номер проверяемого' заполнено некорректно!", ShowMessageModel.ErrorMessage.TitleColor));
        input.HighlightTestedNumber();
        return (false, "Неверный формат 'Номер проверяемого'.", "", "", "");
      }

      // 4) Проверка «тестирующего» номера (формат a.b)
      var t2 = tester.Trim();
      var t2Parts = t2.Split('.');
      if (t2Parts.Length != 2
       || string.IsNullOrWhiteSpace(t2Parts[0])
       || string.IsNullOrWhiteSpace(t2Parts[1]))
      {
        _ = protocolUI.ShowMessageAsync(
            new ShowMessageModel("Поле 'Номер проверяющего' заполнено некорректно!", ShowMessageModel.ErrorMessage.TitleColor));
        input.HighlightTesterNumber();
        return (false, "Неверный формат 'Номер проверяющего'.", "", "", "");
      }

      // 5) Нельзя совпадать
      if (t1 == t2)
      {
        _ = protocolUI.ShowMessageAsync(
            new ShowMessageModel("Номера не должны совпадать!", ShowMessageModel.ErrorMessage.TitleColor));
        input.HighlightTestedNumber();
        input.HighlightTesterNumber();
        return (false, "Повтор параметров.", "", "", "");
      }

      // 6) Проверка диапазона: не пустая строка и корректный формат
      var rg = range.Trim();
      if (string.IsNullOrEmpty(rg))
      {
        _ = protocolUI.ShowMessageAsync(
            new ShowMessageModel("Поле 'Диапазон проверки' не заполнено!", ShowMessageModel.ErrorMessage.TitleColor));
        input.HighlightTestRange();
        return (false, "Диапазон не задан.", "", "", "");
      }
      if (!ValidateRangeInput(rg, out var error))
      {
        _ = protocolUI.ShowMessageAsync(
            new ShowMessageModel($"Неверный диапазон: {error}", ShowMessageModel.ErrorMessage.TitleColor));
        input.HighlightTestRange();
        return (false, "Неверный диапазон.", "", "", "");
      }

      // 7) Всё ок
      return (true, "OK", t1, t2, rg);
    }

    /// <summary>
    /// Проверяет строку диапазона вида "1,2-5,7" на корректность.
    /// </summary>
    private static bool ValidateRangeInput(string rangeText, out string errorMessage)
    {
      errorMessage = "";
      var segments = rangeText.Split(',');
      foreach (var seg in segments)
      {
        var s = seg.Trim();
        if (string.IsNullOrEmpty(s))
        {
          errorMessage = "пустой элемент";
          return false;
        }

        if (s.Contains('-'))
        {
          var bounds = s.Split('-');
          if (bounds.Length != 2
           || !int.TryParse(bounds[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var start)
           || !int.TryParse(bounds[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var end))
          {
            errorMessage = $"формат '{s}'";
            return false;
          }
          if (start >= end)
          {
            errorMessage = $"неверный диапазон '{s}'";
            return false;
          }
        }
        else
        {
          if (!int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
          {
            errorMessage = $"не число '{s}'";
            return false;
          }
        }
      }
      return true;
    }
  }
}
