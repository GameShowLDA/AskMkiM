using System.Globalization;
using DataBaseConfiguration.Services.Device;
using Mode.Models;
using UI.Components;
using UI.Controls.ProtocolNew;
using Utilities.Events;
using Utilities.Models;
using static NewCore.Enum.DeviceEnum;

namespace Mode.Base
{
  /// <summary>
  /// Предоставляет методы для безопасной валидации пользовательского ввода из элемента управления ProtocolUI.
  /// </summary>
  public static class UIValidationHelper
  {
    static InputField? inputField;

    /// <summary>
    /// Выполняет валидацию данных из InputField, включая проверку оборудования, и возвращает готовые объекты.
    /// </summary>
    /// <typeparam name="T">Тип измерения, наследуемый от BaseMeasurement.</typeparam>
    /// <param name="protocolUI">Экземпляр ProtocolUI.</param>
    /// <param name="messageOnSuccess">Показывать ли сообщение при успешной валидации.</param>
    /// <param name="timeCheck">Проверять ли заданное время для выполнения режимов (ППУ).</param>
    /// <param name="voltageCheck">Проверять ли заданное напряжение для выполнения режимов (ППУ).</param>
    /// <param name="timeRampCheck">Проверять ли заданное время нарасстания для выполнения режимов (ППУ).</param>
    /// <param name="busCheck">Проверять ли заданную шину.</param>
    /// <returns>Кортеж: успешность, сообщение, первая точка, вторая точка, параметр.</returns>
    public static (bool Success, string Message, DataModel DataModel) TryValidateAndParseInputWithEquipment(
      ProtocolUI protocolUI,
      bool messageOnSuccess = true,
      bool timeCheck = false,
      bool voltageCheck = false,
      bool timeRampCheck = false,
      bool busCheck = false)
    {
      var (success, message, first, second, parameter) = TryValidateAndParseInput(protocolUI, messageOnSuccess);
      if (!success)
      {
        return (false, message, new DataModel());
      }

      var equipmentValidation = CheckEquipmentExists(first, second);
      if (!equipmentValidation.Success)
      {
        return (false, equipmentValidation.Message, new DataModel());
      }

      var unique = CheckPointsAreUnique(first, second);
      if (!unique.Success)
      {
        return (false, unique.Error, new DataModel());
      }

      double time = -1;
      if (timeCheck)
      {
        var timeData = CheckTime();
        if (!timeData.Success)
        {
          time = 1;
        }

        time = timeData.Value;
      }

      double voltage = -1;
      if (voltageCheck)
      {
        var voltageData = CheckVoltage();
        if (!voltageData.Success)
        {
          voltage = 500;
        }

        voltage = voltageData.Value;
      }

      double timeRampResult = -1;
      if (timeRampCheck)
      {
        var timeRampData = CheckTimeRamp();
        if (!timeRampData.Success)
        {
          timeRampResult = 1;
        }

        timeRampResult = timeRampData.Value;
      }

      DataModel dataModel = new DataModel(first, second, parameter)
      {
        Time = time,
        Voltage = voltage,
        RampTime = timeRampResult,
      };

      if (busCheck)
      {
        var dataBus = CheckBus();
        if (dataBus.Success)
        {
          dataModel.ActiveBus = dataBus.Value;
        }
        else
        {
          dataModel.ActiveBus = BusPoint.A;
        }
      }

      return (true, "OK", dataModel);
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
    private static (bool Success, string Message, PointModel First, PointModel Second, double Parameter) TryValidateAndParseInput(ProtocolUI protocolUI, bool messageOnSuccess = true)
    {
      inputField = protocolUI.GetInputFieldSafe();
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
      if (point.PointNumber < 1 || point.PointNumber > module.PointCount)
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

    private static (bool Success, string Message, double Value) CheckTime()
    {
      if (inputField == null)
      {
        InputValidationEvents.TriggerInvalidFirstPoint = true;
        return (false, "Элемент ввода не найден.", -1);
      }

      var timeString = inputField.GetInputFieldTimeValuesSafe();

      if (double.TryParse(timeString, out double result))
      {
        return (true, string.Empty, result);
      }
      else
      {
        return (false, "Время выполнения должно быть дробным числом вида : x.y", -1);
      }
    }

    private static (bool Success, string Message, double Value) CheckTimeRamp()
    {
      if (inputField == null)
      {
        InputValidationEvents.TriggerInvalidFirstPoint = true;
        return (false, "Элемент ввода не найден.", -1);
      }

      var timeString = inputField.GetInputFieldTimeRampValuesSafe();

      if (double.TryParse(timeString, out double result))
      {
        return (true, string.Empty, result);
      }
      else
      {
        return (false, "Время выполнения должно быть дробным числом вида : x.y", -1);
      }
    }

    private static (bool Success, string Message, BusPoint Value) CheckBus()
    {
      if (inputField == null)
      {
        InputValidationEvents.TriggerInvalidFirstPoint = true;
        return (false, "Элемент ввода не найден.", default);
      }

      var timeString = inputField.GetInputFieldBusValuesSafe();

      if (timeString != default)
      {
        return (true, string.Empty, timeString);
      }
      else
      {
        return (false, "Шина для подключения некорректна", default);
      }
    }

    private static (bool Success, string Message, double Value) CheckVoltage()
    {
      if (inputField == null)
      {
        InputValidationEvents.TriggerInvalidFirstPoint = true;
        return (false, "Элемент ввода не найден.", -1);
      }

      var voltageString = inputField.GetInputFieldVoltageValuesSafe();

      if (double.TryParse(voltageString, out double result))
      {
        return (true, string.Empty, result);
      }
      else
      {
        return (false, "Время выполнения должно быть дробным числом вида : x.y", -1);
      }
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

  /// <summary>
  /// Модель данных элемента.
  /// </summary>
  public class DataModel
  {
    /// <summary>
    /// Модель первой точки.
    /// </summary>
    public PointModel FirstPoint { get; set; }

    /// <summary>
    /// Модель второй точки.
    /// </summary>
    public PointModel SecondPoint { get; set; }

    /// <summary>
    /// Значение электрического параметра.
    /// </summary>
    public double Param { get; set; }

    /// <summary>
    /// Значение времени при выполнения теста (ППУ).
    /// </summary>
    public double Time { get; set; }

    /// <summary>
    /// Значение нарастания времени при выполнения теста (ППУ).
    /// </summary>
    public double RampTime { get; set; }

    /// <summary>
    /// Значение напряжения при выполнения теста (ППУ).
    /// </summary>
    public double Voltage { get; set; }

    /// <summary>
    /// Заданная шина.
    /// </summary>
    public BusPoint ActiveBus { get; set; }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DataModel"/>.
    /// </summary>
    /// <param name="first">Первая точка.</param>
    /// <param name="second">Вторая точка.</param>
    /// <param name="param">Значение электрического параметра.</param>
    public DataModel(PointModel first, PointModel second, double param)
    {
      FirstPoint = first;
      SecondPoint = second;
      Param = param;
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DataModel"/>.
    /// </summary>
    /// <param name="first">Первая точка.</param>
    /// <param name="second">Вторая точка.</param>
    /// <param name="param">Значение электрического параметра.</param>
    public DataModel() { }
  }
}
