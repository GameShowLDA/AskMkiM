using System.Globalization;
using System.Text.RegularExpressions;
using AppConfig.DataBase.Repositories;
using NewCore.Base.Device;
using UI.Components;

namespace Mode.Metrology.MeasurementSystem
{
  /// <summary>
  /// Базовый класс для всех типов измерений, содержащий общий алгоритм.
  /// Использует шаблонный метод для автоматизации процесса.
  /// </summary>
  public abstract class BaseMeasurement
  {
    #region Репозитории устройств.

    /// <summary>
    /// Репозиторий для работы с менеджерами шасси.
    /// Используется для проверки существования шасси в базе данных.
    /// </summary>
    private readonly ChassisManagerRepository _chassisManagerRepository = new ChassisManagerRepository();

    /// <summary>
    /// Репозиторий для работы с модулями коммутации реле.
    /// Используется для проверки существования модуля и точки.
    /// </summary>
    private readonly RelaySwitchModuleRepository _relaySwitchModuleRepository = new RelaySwitchModuleRepository();

    #endregion

    /// <summary>
    /// Запускает процесс измерения.
    /// </summary>
    /// <param name="point1">Первая точка измерения.</param>
    /// <param name="point2">Вторая точка измерения.</param>
    /// <param name="referenceValue">Эталонное значение.</param>
    public void ExecuteMeasurement(string point1, string point2, string referenceValue)
    {
      ValidateInput(point1, point2, referenceValue);
      ConnectToEquipment();
      SetupCommutation(point1, point2);
      ConfigureMultimeter();
      PerformMeasurement();
      FinalizeMeasurement();
    }

    /// <summary>
    /// Проверяет корректность входных данных.
    /// </summary>
    /// <param name="point1">Первая точка.</param>
    /// <param name="point2">Вторая точка.</param>
    /// <param name="referenceValue">Эталонное значение.</param>
    public virtual void ValidateInput(string point1, string point2, string referenceValue)
    {
      ValidatePointFormat(point1, "Первая");
      ValidatePointFormat(point2, "Вторая");

      if (point1 == point2)
      {
        Utilities.Events.InputValidationEvents.TriggerDuplicatePoints = true;
        throw new ArgumentException($"Ошибка: Точки не должны совпадать (введено: \"{point1}\").");
      }

      ValidatePointExists(point1, "Первая");
      ValidatePointExists(point2, "Вторая");

      if (!IsValidElectricalParameter(referenceValue, out double parsedValue))
      {
        Utilities.Events.InputValidationEvents.TriggerInvalidParameter = true;
        throw new ArgumentException("Ошибка: Электрический параметр должен быть числом.");
      }
    }

    /// <summary>
    /// Подключает оборудование.
    /// </summary>
    protected virtual void ConnectToEquipment()
    {
      // TODO: Реализовать подключение к оборудованию
    }

    /// <summary>
    /// Настраивает коммутацию перед измерением.
    /// </summary>
    /// <param name="point1">Первая точка.</param>
    /// <param name="point2">Вторая точка.</param>
    protected virtual void SetupCommutation(string point1, string point2)
    {
      // TODO: Реализовать настройку коммутации
    }

    /// <summary>
    /// Настраивает измерительное устройство (мультиметр или ППУ).
    /// </summary>
    /// <param name="device">Объект настройки.</param>
    protected abstract void ConfigureMultimeter();

    /// <summary>
    /// Выполняет измерение.
    /// </summary>
    protected virtual void PerformMeasurement()
    {
      // TODO: Реализовать процесс измерения
    }

    /// <summary>
    /// Завершает измерение, размыкает реле и отключает прибор.
    /// </summary>
    protected virtual void FinalizeMeasurement()
    {
      // TODO: Реализовать завершение измерения
    }

    #region private

    /// <summary>
    /// Проверяет, соответствует ли точка формату A.B.C и выбрасывает исключение, если нет.
    /// </summary>
    /// <param name="point">Точка измерения.</param>
    /// <param name="label">Метка точки (например, \"первая\" или \"вторая\").</param>
    private void ValidatePointFormat(string point, string label)
    {
      if (string.IsNullOrWhiteSpace(point))
      {
        if (label == "Первая")
        {
          Utilities.Events.InputValidationEvents.TriggerInvalidFirstPoint = true;
        }
        else if (label == "Вторая")
        {
          Utilities.Events.InputValidationEvents.TriggerInvalidSecondPoint = true;
        }

        throw new ArgumentException($"Ошибка: {label} точка не задана.");
      }

      string pattern = @"^\d+\.\d+\.\d+$";
      if (!Regex.IsMatch(point, pattern))
      {
        if (label == "Первая")
        {
          Utilities.Events.InputValidationEvents.TriggerInvalidFirstPoint = true;
        }
        else if (label == "Вторая")
        {
          Utilities.Events.InputValidationEvents.TriggerInvalidSecondPoint = true;
        }

        throw new ArgumentException($"Ошибка: {label} точка \"{point}\" должна быть в формате A.B.C, где A, B, C – целые числа.");
      }
    }

    /// <summary>
    /// Извлекает номер шасси из точки A.B.C.
    /// </summary>
    /// <param name="point">Точка в формате A.B.C.</param>
    /// <returns>Номер шасси.</returns>
    private int GetChassisNumber(string point)
    {
      return int.Parse(point.Split('.')[0]);
    }

    /// <summary>
    /// Извлекает номер модуля коммутации реле из точки A.B.C.
    /// </summary>
    /// <param name="point">Точка в формате A.B.C.</param>
    /// <returns>Номер модуля.</returns>
    private int GetModuleNumber(string point)
    {
      return int.Parse(point.Split('.')[1]);
    }

    /// <summary>
    /// Извлекает номер точки из точки A.B.C.
    /// </summary>
    /// <param name="point">Точка в формате A.B.C.</param>
    /// <returns>Номер точки.</returns>
    private int GetPointNumber(string point)
    {
      return int.Parse(point.Split('.')[2]);
    }

    /// <summary>
    /// Проверяет, существует ли указанное шасси в базе данных.
    /// </summary>
    /// <param name="chassisNumber">Номер шасси.</param>
    /// <returns>True, если шасси существует, иначе false.</returns>
    private bool ChassisExistsInDatabase(int chassisNumber)
    {
      return _chassisManagerRepository.GetByNumber(chassisNumber) != null;
    }

    /// <summary>
    /// Проверяет, существует ли точка в системе.
    /// </summary>
    /// <param name="point">Точка измерения в формате A.B.C.</param>
    /// <param name="label">Метка точки: "Первая" или "Вторая".</param>
    /// <exception cref="ArgumentException">Выбрасывается с конкретным описанием ошибки.</exception>
    private void ValidatePointExists(string point, string label)
    {
      int chassisNumber = GetChassisNumber(point);
      int moduleNumber = GetModuleNumber(point);
      int pointNumber = GetPointNumber(point);

      if (!ChassisExistsInDatabase(chassisNumber))
      {
        RaisePointValidationEvent(label);
        throw new ArgumentException($"Ошибка: Менеджер шасси с номером {chassisNumber} не найден (введено: \"{point}\").");
      }

      var modules = _relaySwitchModuleRepository.GetDevicesByNumberChassis(chassisNumber);
      var module = modules.FirstOrDefault(m => m.Number == moduleNumber);

      if (module == null)
      {
        RaisePointValidationEvent(label);
        throw new ArgumentException($"Ошибка: Модуль коммутации с номером {moduleNumber} не найден у шасси {chassisNumber} (введено: \"{point}\").");
      }

      if (pointNumber < 0 || pointNumber >= module.PointCount)
      {
        RaisePointValidationEvent(label);
        throw new ArgumentException($"Ошибка: Точка {pointNumber} вне допустимого диапазона (0 - {module.PointCount - 1}) в модуле {moduleNumber} шасси {chassisNumber} (введено: \"{point}\").");
      }
    }

    /// <summary>
    /// Проверяет, можно ли преобразовать строку в корректное положительное число.
    /// </summary>
    /// <param name="value">Строковое значение параметра.</param>
    /// <param name="parsedValue">Выходной параметр с преобразованным значением.</param>
    /// <returns>True, если преобразование успешно и число положительное, иначе false.</returns>
    private bool IsValidElectricalParameter(string value, out double parsedValue)
    {
      if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedValue))
      {
        return !double.IsNaN(parsedValue) && !double.IsInfinity(parsedValue) && parsedValue >= 0;
      }

      return false;
    }

    private void RaisePointValidationEvent(string label)
    {
      if (label == "Первая")
      {
        Utilities.Events.InputValidationEvents.TriggerInvalidFirstPoint = true;
      }
      else if (label == "Вторая")
      {
        Utilities.Events.InputValidationEvents.TriggerInvalidSecondPoint = true;
      }
    }
    #endregion
  }
}
