using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Mode.Models;
using Mode.Metrology.MeasurementSystem;
using UI.Controls.Protocol;
using AppConfig.DataBase.Services;
using Utilities.Events;

namespace Mode.Base
{
  /// <summary>
  /// Предоставляет методы для безопасной валидации пользовательского ввода из элемента управления ProtocolUI.
  /// </summary>
  public static class UIValidationHelper
  {
    /// <summary>
    /// Выполняет валидацию данных из InputField, включая проверку оборудования, и возвращает готовые объекты.
    /// </summary>
    /// <typeparam name="T">Тип измерения, наследуемый от BaseMeasurement.</typeparam>
    /// <param name="protocolUI">Экземпляр ProtocolUI.</param>
    /// <param name="messageOnSuccess">Показывать ли сообщение при успешной валидации.</param>
    /// <returns>Кортеж: успешность, сообщение, первая точка, вторая точка, параметр.</returns>
    public static async Task<(bool Success, string Message, PointModel First, PointModel Second, double Parameter)>
        TryValidateAndParseInputWithEquipmentAsync<T>(ProtocolUI protocolUI, bool messageOnSuccess = true)
        where T : BaseMeasurement, new()
    {
      var (success, message, first, second, parameter) = await TryValidateAndParseInputAsync<T>(protocolUI, messageOnSuccess);
      if (!success)
      {
        return (false, message, null, null, 0);
      }

      var equipmentValidation = CheckEquipmentExists(first, second);
      if (!equipmentValidation.Success)
      {
        return (false, equipmentValidation.Message, null, null, 0);
      }

      var unique = CheckPointsAreUnique(first, second);
      if (!unique.Success)
      { 
        return (false, unique.Error, null, null, 0);
      }

      return (true, "OK", first, second, parameter);
    }

    /// <summary>
    /// Выполняет валидацию данных из InputField, а при успехе — возвращает разобранные значения.
    /// </summary>
    /// <typeparam name="T">Тип измерения, наследуемый от BaseMeasurement.</typeparam>
    /// <param name="protocolUI">Экземпляр ProtocolUI.</param>
    /// <param name="messageOnSuccess">Показывать ли сообщение при успешной валидации.</param>
    /// <returns>
    /// Кортеж с результатом: успешность, сообщение, первая точка, вторая точка, электрический параметр.
    /// </returns>
    private static async Task<(bool Success, string Message, PointModel First, PointModel Second, double Parameter)>
     TryValidateAndParseInputAsync<T>(ProtocolUI protocolUI, bool messageOnSuccess = true)
     where T : BaseMeasurement, new()
    {
      var inputField = protocolUI.GetInputFieldSafe();
      if (inputField == null)
      {
        InputValidationEvents.TriggerInvalidFirstPoint = true;
        return (false, "Элемент ввода не найден.", null, null, 0);
      }

      var (point1, point2, parameterStr) = inputField.GetInputFieldValuesSafe();

      var first = PointModel.ParsePointString(point1);
      var second = PointModel.ParsePointString(point2);

      if (first == null)
      {
        InputValidationEvents.TriggerInvalidFirstPoint = true;
        return (false, "Неверный формат первой точки.", null, null, 0);
      }

      if (second == null)
      {
        InputValidationEvents.TriggerInvalidSecondPoint = true;
        return (false, "Неверный формат второй точки.", null, null, 0);
      }

      if (!double.TryParse(parameterStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double parameter))
      {
        InputValidationEvents.TriggerInvalidParameter = true;
        return (false, "Электрический параметр должен быть числом.", null, null, 0);
      }

      return (true, "OK", first, second, parameter);
    }

    /// <summary>
    /// Проверяет наличие оборудования для двух точек.
    /// </summary>
    /// <param name="first">Первая точка.</param>
    /// <param name="second">Вторая точка.</param>
    /// <returns>Успешность проверки и сообщение об ошибке (если есть).</returns>
    private static (bool Success, string Message) CheckEquipmentExists(PointModel first, PointModel second)
    {
      var resultFirst = IsValidPointExists(first);
      if (!resultFirst.Success)
      {
        InputValidationEvents.TriggerInvalidFirstPoint = true;
        return (false, $"Ошибка для первой точки: {resultFirst.Error}");
      }

      var resultSecond = IsValidPointExists(second);
      if (!resultSecond.Success)
      {
        InputValidationEvents.TriggerInvalidSecondPoint = true;
        return (false, $"Ошибка для второй точки: {resultSecond.Error}");
      }

      return (true, null);
    }

    private static PointValidationResult IsValidPointExists(PointModel point)
    {
      // Проверяем шасси
      var chassisExists = new ChassisManagerServices().GetEntityById(point.DeviceNumber) != null;
      if (!chassisExists)
      {
        return new PointValidationResult
        {
          Success = false,
          Error = $"Шасси с номером {point.DeviceNumber} не найдено.",
        };
      }

      // Проверяем модуль коммутации
      var modules = new RelaySwitchModuleServices().GetEntitiesByNumberChassis(point.DeviceNumber);
      var module = modules.FirstOrDefault(m => m.Number == point.ModuleNumber);

      if (module == null)
      {
        return new PointValidationResult
        {
          Success = false,
          Error = $"Модуль {point.ModuleNumber} в шасси {point.DeviceNumber} не найден.",
        };
      }

      // Проверяем диапазон точки
      if (point.PointNumber < 0 || point.PointNumber >= module.PointCount)
      {
        return new PointValidationResult
        {
          Success = false,
          Error = $"Точка {point.PointNumber} в модуле {point.ModuleNumber} выходит за пределы диапазона (0-{module.PointCount - 1}).",
        };
      }

      return new PointValidationResult
      {
        Success = true,
      };
    }

    /// <summary>
    /// Проверяет, уникальны ли две точки (не совпадают).
    /// </summary>
    /// <param name="first">Первая точка.</param>
    /// <param name="second">Вторая точка.</param>
    /// <returns>True, если точки уникальны; иначе — false.</returns>
    private static PointValidationResult CheckPointsAreUnique(PointModel first, PointModel second)
    {
      var result = first.ValidateUnique(second);
      if (!result)
      {
        InputValidationEvents.TriggerInvalidSecondPoint = true;

        return new PointValidationResult
        {
          Success = result,
          Error = $"Точка {second.ToString()} не уникальна",
        };
      }

      return new PointValidationResult
      {
        Success = result,
      };
    }
  }

  /// <summary>
  /// Результат проверки точки на наличие оборудования.
  /// </summary>
  public class PointValidationResult
  {
    /// <summary>
    /// Успешна ли проверка.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Сообщение об ошибке (если есть).
    /// </summary>
    public string Error { get; set; }
  }
}
