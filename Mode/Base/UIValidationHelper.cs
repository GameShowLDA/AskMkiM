using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mode.Metrology.MeasurementSystem;
using Mode.Models;
using UI.Controls.Protocol;
using Utilities.Models;

namespace Mode.Base
{
  /// <summary>
  /// Предоставляет методы для безопасной валидации пользовательского ввода из элемента управления ProtocolUI.
  /// </summary>
  public static class UIValidationHelper
  {
    /// <summary>
    /// Выполняет валидацию данных из InputField, а при успехе — возвращает разобранные значения.
    /// </summary>
    /// <typeparam name="T">Тип измерения, наследуемый от BaseMeasurement.</typeparam>
    /// <param name="protocolUI">Экземпляр ProtocolUI.</param>
    /// <param name="messageOnSuccess">Показывать ли сообщение при успешной валидации.</param>
    /// <returns>
    /// Кортеж с результатом: успешность, сообщение, первая точка, вторая точка, электрический параметр.
    /// </returns>
    public static async Task<(bool Success, string Message, PointModel First, PointModel Second, double Parameter)>
      TryValidateAndParseInputAsync<T>(ProtocolUI protocolUI, bool messageOnSuccess = true)
      where T : BaseMeasurement, new()
    {
      var (success, message) = await TryValidateInputAsync<T>(protocolUI, messageOnSuccess);
      if (!success)
      {
        return (false, message, null, null, 0);
      }

      var inputField = protocolUI.GetInputFieldSafe();
      var (point1, point2, parameterStr) = inputField.GetInputFieldValuesSafe();

      var first = PointModel.ParsePointString(point1);
      var second = PointModel.ParsePointString(point2);

      if (first == null || second == null)
      {
        return (false, "Ошибка: Неверный формат точки. Ожидается A.B.C", null, null, 0);
      }

      if (!double.TryParse(parameterStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double parameter))
      {
        return (false, "Ошибка: Электрический параметр должен быть числом.", null, null, 0);
      }

      return (true, "OK", first, second, parameter);
    }

    /// <summary>
    /// Выполняет валидацию данных из InputField, отображает сообщение и возвращает результат.
    /// </summary>
    /// <typeparam name="T">Класс измерения, наследуемый от BaseMeasurement.</typeparam>
    /// <param name="protocolUI">Экземпляр ProtocolUI.</param>
    /// <param name="messageOnSuccess">Показать ли сообщение при успешной валидации.</param>
    /// <returns>(успешность, сообщение).</returns>
    private static async Task<(bool Success, string Message)> TryValidateInputAsync<T>(
        ProtocolUI protocolUI,
        bool messageOnSuccess = true) where T : BaseMeasurement, new()
    {
      var inputField = protocolUI.GetInputFieldSafe();
      if (inputField == null)
      {
        const string msg = "Ошибка: Элемент ввода не найден.";
        return (false, msg);
      }
      
      var (point1, point2, parameter) = inputField.GetInputFieldValuesSafe();

      try
      {
        var measurement = new T();
        measurement.ValidateInput(point1, point2, parameter);
        return (true, ShowMessageModel.SuccessMessage.Item1);
      }
      catch (Exception ex)
      {
        return (false, ex.Message);
      }
    }
  }
}
