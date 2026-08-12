using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using System.Globalization;

namespace Ask.Engine.Tests.Base
{
  /// <summary>
  /// Помощник для валидации данных из InputFieldLightweight внутри ProtocolUI.
  /// </summary>
  public static class UIValidationHelperLightweight
  {
    #region Методы валидации

    /// <summary>
    /// Извлекает и проверяет строки «тестируемого» и «тестирующего» номера и диапазона.
    /// </summary>
    /// <param name="message">Сервис вывода сообщений об ошибках проверки данных.</param>
    /// <param name="inputFieldProvider">Поставщик значений проверяемых полей.</param>
    /// <param name="inputHighlightService">Сервис подсветки полей с некорректными значениями.</param>
    /// <returns>
    /// Задача, возвращающая результат проверки, описание ошибки и нормализованные значения полей.
    /// </returns>
    public static async Task<(bool Success, string Message, string Tested, string Tester, string Range)>
        TryValidateAndParseInputAsync(
          IMessageOutputService message,
          IInputFieldProvider inputFieldProvider,
          IInputHighlightService inputHighlightService)
    {
      // 1) Достаём контрол
      IInputFieldAccessor? input = inputFieldProvider.GetInputFieldAccessor();
      if (input == null)
      {
        return (false, "Поле ввода не найдено.", "", "", "");
      }

      // 2) Безопасно читаем сырые строки
      var (tested, tester, range) = input.GetValues();

      // 3) Проверка «тестируемого» номера (формат a.b)
      var t1 = tested.Trim();
      var t1Parts = t1.Split('.');
      if (t1Parts.Length != 2
       || string.IsNullOrWhiteSpace(t1Parts[0])
       || string.IsNullOrWhiteSpace(t1Parts[1]))
      {
        await ValidationMessages.PublishInvalidTestedNumberAsync(message);
        inputHighlightService.HighlightTestedNumber();
        return (false, "Неверный формат 'Номер проверяемого'.", "", "", "");
      }

      // 4) Проверка «тестирующего» номера (формат a.b)
      var t2 = tester.Trim();
      var t2Parts = t2.Split('.');
      if (t2Parts.Length != 2
       || string.IsNullOrWhiteSpace(t2Parts[0])
       || string.IsNullOrWhiteSpace(t2Parts[1]))
      {
        await ValidationMessages.PublishInvalidTesterNumberAsync(message);
        inputHighlightService.HighlightTesterNumber();
        return (false, "Неверный формат 'Номер проверяющего'.", "", "", "");
      }

      // 5) Нельзя совпадать
      if (t1 == t2)
      {
        await ValidationMessages.PublishDuplicateNumbersAsync(message);
        inputHighlightService.HighlightTestedNumber();
        inputHighlightService.HighlightTesterNumber();
        return (false, "Повтор параметров.", "", "", "");
      }

      // 6) Проверка диапазона: не пустая строка и корректный формат
      var rg = range.Trim();
      if (string.IsNullOrEmpty(rg))
      {
        await ValidationMessages.PublishEmptyRangeAsync(message);
        inputHighlightService.HighlightTestRange();
        return (false, "Диапазон не задан.", "", "", "");
      }
      if (!ValidateRangeInput(rg, out var error))
      {
        await ValidationMessages.PublishInvalidRangeAsync(message, error);
        inputHighlightService.HighlightTestRange();
        return (false, "Неверный диапазон.", "", "", "");
      }

      // 7) Всё ок
      return (true, "OK", t1, t2, rg);
    }

    #endregion

    #region Проверка диапазона

    /// <summary>
    /// Проверяет строку диапазона вида "1,2-5,7" на корректность.
    /// </summary>
    /// <param name="rangeText">Входные значения в виде строки</param>
    /// <param name="errorMessage">Сообщение об ошибке при некорректной валидации</param>
    /// <returns>True, если не было найдено ошибок</returns>
    private static bool ValidateRangeInput(string rangeText, out string errorMessage, int maxValue = 350)
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

          // Проверка, чтобы значения диапазона не превышали 350
          if (end > maxValue)
          {
            errorMessage = $"Значение не может превышать {maxValue} (для '{s}')";
            return false;
          }
        }
        else
        {
          if (!int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
          {
            errorMessage = $"не число '{s}'";
            return false;
          }

          // Проверка, чтобы значение не превышало 350
          if (value > maxValue)
          {
            errorMessage = $"Значение не может превышать {maxValue} (для '{s}')";
            return false;
          }
        }
      }
      return true;
    }

    #endregion
  }
}
