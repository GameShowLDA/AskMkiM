using UI.Components;
using UI.Controls.Protocol;

namespace Mode.Base
{
  /// <summary>
  /// Вспомогательный класс для безопасной работы с элементом InputFieldLightweight внутри ProtocolUI.
  /// </summary>
  public static class UIHelperLightweight
  {
    #region Методы получения значений

    /// <summary>
    /// Безопасно извлекает значения из InputFieldLightweight: проверяемый номер, номер проверяющего и диапазон.
    /// </summary>
    /// <param name="inputField">Экземпляр InputFieldLightweight.</param>
    /// <returns>Кортеж с тремя строками: TestedNumber, TesterNumber, TestRange.</returns>
    public static (string Tested, string Tester, string Range) GetInputFieldLightweightValuesSafe(this InputFieldLightweight inputField)
    {
      string tested = string.Empty;
      string tester = string.Empty;
      string range = string.Empty;

      // Локальный метод для чтения значений
      void ReadValues()
      {
        tested = inputField.TestedNumber;
        tester = inputField.TesterNumber;
        range = inputField.TestRange;
      }

      // Проверка, доступен ли диспетчер
      if (inputField.Dispatcher.CheckAccess())
      {
        ReadValues();
      }
      else
      {
        inputField.Dispatcher.Invoke(ReadValues);
      }

      return (tested, tester, range);
    }

    #endregion

    #region Методы работы с ProtocolUI

    /// <summary>
    /// Безопасно извлекает экземпляр InputFieldLightweight из ProtocolUI.ContentView.
    /// </summary>
    /// <param name="protocolUI">Экземпляр ProtocolUI.</param>
    /// <returns>InputFieldLightweight или null, если не найден.</returns>
    public static InputFieldLightweight? GetInputFieldLightweightSafe(this ProtocolUI protocolUI)
    {
      // Проверка на null для предотвращения возможных ошибок
      if (protocolUI == null)
        return null;

      InputFieldLightweight? result = null;

      // Локальный метод для извлечения значения
      void TryGet()
      {
        if (protocolUI.ContentView is InputFieldLightweight lw)
        {
          result = lw;
        }
      }

      // Проверка, доступен ли диспетчер
      if (protocolUI.Dispatcher.CheckAccess())
      {
        TryGet();
      }
      else
      {
        protocolUI.Dispatcher.Invoke(TryGet);
      }

      return result;
    }

    #endregion
  }
}