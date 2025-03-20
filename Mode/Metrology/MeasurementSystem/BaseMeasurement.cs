using System.Globalization;
using System.Text.RegularExpressions;
using AppConfig.DataBase.Repositories;

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
      if (!IsValidPointFormat(point1) || !IsValidPointFormat(point2))
      {
        throw new ArgumentException("Ошибка: Точки должны быть в формате A.B.C, где A, B, C – целые числа.");
      }

      if (!PointExistsInDatabase(point1) || !PointExistsInDatabase(point2))
      {
        throw new ArgumentException("Ошибка: Одна или обе точки отсутствуют в базе данных.");
      }

      if (!IsValidElectricalParameter(referenceValue, out double parsedValue))
      {
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
    /// Проверяет, соответствует ли точка формату A.B.C.
    /// </summary>
    /// <param name="point">Точка измерения.</param>
    /// <returns>True, если точка соответствует формату, иначе false.</returns>
    private bool IsValidPointFormat(string point)
    {
      if (string.IsNullOrWhiteSpace(point))
      {
        return false;
      }

      string pattern = @"^\d+\.\d+\.\d+$";
      return Regex.IsMatch(point, pattern);
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
    /// Проверяет, существует ли модуль коммутации реле и точка в базе данных.
    /// </summary>
    /// <param name="point">Точка измерения в формате A.B.C.</param>
    /// <returns>True, если точка существует в БД, иначе false.</returns>
    private bool PointExistsInDatabase(string point)
    {
      int chassisNumber = GetChassisNumber(point);
      int moduleNumber = GetModuleNumber(point);
      int pointNumber = GetPointNumber(point);

      if (!ChassisExistsInDatabase(chassisNumber))
      {
        return false;
      }

      var modules = _relaySwitchModuleRepository.GetDevicesByNumberChassis(chassisNumber);
      var module = modules.FirstOrDefault(m => m.Number == moduleNumber);
      return module != null && pointNumber >= 0 && pointNumber < module.PointCount;
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

    #endregion
  }
}
