using System.Reflection;
using NewCore.Base.Device;
using NewCore.Base.Interface.Main;
using static Utilities.LoggerUtility;

namespace TestConsole.MINT
{
  static public partial class Mint_Test
  {
    /// <summary>
    /// Проверка формирования дискрет напряжения.
    /// </summary>
    /// <param name="token">Токен для отмены операции.</param>
    static private async Task GenerateDiscreteVoltageCheck(IFastMeter fastMeter, IPowerSourceModule powerSource)
    {
      LogInformation("Начало проверки формирования дискрет напряжения");

      await CheckVoltageLevelsAsync(0.1, 0.9, 0.1, 20, fastMeter, powerSource);
      await CheckVoltageLevelsAsync(1, 9, 1, 20, fastMeter, powerSource);
      await CheckVoltageLevelsAsync(10, 40, 10, 20, fastMeter, powerSource);

      LogInformation("Завершение проверки формирования дискрет напряжения");
    }

    /// <summary>
    /// Проверяет уровни напряжения по заданному диапазону и шагу.
    /// </summary>
    /// <param name="startVoltage">Начальное значение напряжения.</param>
    /// <param name="endVoltage">Конечное значение напряжения.</param>
    /// <param name="step">Шаг напряжения.</param>
    /// <param name="delay">Задержка между измерениями.</param>
    /// <param name="token">Токен для отмены операции.</param>
    static private async Task CheckVoltageLevelsAsync(double startVoltage, double endVoltage, double step, int delay, IFastMeter fastMeter, IPowerSourceModule powerSource)
    {
      LogInformation($"Проверка уровней напряжения от {startVoltage} до {endVoltage} с шагом {step}");
      for (double voltage = startVoltage; voltage <= endVoltage; voltage += step)
      {
        double roundedVoltage = Math.Round(voltage, 1);
        await SetVoltageAndShowMessage(roundedVoltage, powerSource);
        await SetVoltageIfNotIdle(roundedVoltage, powerSource);
        await Task.Delay(1000);
        await MeasureAndCompareVoltage(roundedVoltage, delay, fastMeter);
      }
    }

    /// <summary>
    /// Устанавливает напряжение и отображает сообщение.
    /// </summary>
    /// <param name="voltage">Устанавливаемое напряжение.</param>
    static private async Task SetVoltageAndShowMessage(double voltage, IPowerSourceModule powerSource)
    {
      int a = (int)voltage;
      int b = (int)((voltage - a) * 10);
      LogInformation($"Установка напряжения {a}.{b} В");
      await powerSource.VoltageManager.SetVoltageLevelAsync(a, b);
    }

    /// <summary>
    /// Устанавливает напряжение, если не в режиме ожидания.
    /// </summary>
    /// <param name="voltage">Устанавливаемое напряжение.</param>
    static private async Task SetVoltageIfNotIdle(double voltage, IPowerSourceModule powerSource)
    {
      int a = (int)voltage;
      int b = (int)((voltage - a) * 10);
      LogInformation($"Установка уровня напряжения {a}.{b} В");
      await powerSource.VoltageManager.SetVoltageLevelAsync(a, b);
    }

    /// <summary>
    /// Измеряет и сравнивает напряжение.
    /// </summary>
    /// <param name="voltage">Ожидаемое напряжение.</param>
    /// <param name="delay">Задержка перед измерением.</param>
    /// <param name="token">Токен отмены.</param>
    static private async Task MeasureAndCompareVoltage(double voltage, int delay, IFastMeter fastMeter)
    {
      double tolerance = 0.0001;
      double firstNorm = voltage - (0.01 * voltage + 0.1);
      double lastNorm = voltage + (0.01 * voltage + 0.1);

      await Task.Delay(40).ConfigureAwait(true);
      double result = await GetMeasurementResult(voltage, delay, fastMeter);

      bool error = !(result >= firstNorm - tolerance && result <= lastNorm + tolerance);
      var statusText = !error ? "В норме" : "Вне нормы";
      if (!error)
      {
        LogDebug($"Результат измерения: {result} В ({firstNorm} - {lastNorm}). Статус: {statusText}");
      }
      else
      {
        LogError($"Результат измерения: {result} В ({firstNorm} - {lastNorm}). Статус: {statusText}");
      }

      await Task.Delay(1);
    }

    private static async Task SettingsMeter(IFastMeter meter)
    {
      await meter.DcVoltageManager.SetDCVoltageModeAsync();
    }
    private static async Task<bool> CheckConnectionsAsync(ISwitchingDevice device, IFastMeter meter, IPowerSourceModule powerSource)
    {
      Console.ForegroundColor = ConsoleColor.Green;
      Console.WriteLine("Проверка подключения устройств");
      var result1 = await device.ConnectableManager.InitializeAsync();
      var result2 = await meter.ConnectableManager.InitializeAsync();
      var result3 = await powerSource.ConnectableManager.InitializeAsync();
      Console.ForegroundColor = ConsoleColor.White;

      if (result1.Connect && result2.Connect && result3.Connect)
      {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Оба устройства подключены");
        return true;
      }
      if (!result1.Connect)
      {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("УКШ не подключено");
        Console.ForegroundColor = ConsoleColor.White;
      }
      if (!result2.Connect)
      {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Мультиметр не подключен");
        Console.ForegroundColor = ConsoleColor.White;
      }
      if (!result3.Connect)
      {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("МИНТ не подключен");
        Console.ForegroundColor = ConsoleColor.White;
      }
      Console.ForegroundColor = ConsoleColor.White;
      return false;
    }

    public static T GetDeviceInstance<T>(Func<T> selectDevice) where T : class, IDevice
    {
      var device = selectDevice();
      if (device == null)
      {
        Console.WriteLine("Ошибка: Устройство не выбрано или отсутствует в БД.");
        return null;
      }

      object instance = CreateDeviceInstance(device.DeviceClass);
      if (instance == null || !(instance is T))
      {
        Console.WriteLine($"Ошибка: Не удалось создать объект {device.DeviceClass}.");
        return null;
      }

      CopyProperties(device, instance);

      return instance as T;
    }

    private static object CreateDeviceInstance(string className)
    {
      Console.WriteLine($"Создание объекта класса: {className}");

      Type type = Type.GetType(className);
      if (type == null)
      {
        type = AppDomain.CurrentDomain
                        .GetAssemblies()
                        .Select(a => a.GetType(className))
                        .FirstOrDefault(t => t != null);
      }

      if (type == null)
      {
        Console.WriteLine($"Ошибка: Класс {className} не найден.");
        return null;
      }

      return Activator.CreateInstance(type);
    }

    public static void CopyProperties(object source, object target)
    {
      if (source == null || target == null) return;

      Type sourceType = source.GetType();
      Type targetType = target.GetType();

      foreach (PropertyInfo sourceProp in sourceType.GetProperties())
      {
        PropertyInfo targetProp = targetType.GetProperty(sourceProp.Name);
        if (targetProp != null && targetProp.CanWrite)
        {
          object value = sourceProp.GetValue(source);
          if (value != null)
          {
            targetProp.SetValue(target, value);
          }
        }
      }
    }

    /// <summary>
    /// Получает результат измерения.
    /// </summary>
    /// <param name="voltage">Ожидаемое напряжение.</param>
    /// <param name="delay">Задержка перед измерением.</param>
    /// <param name="token">Токен отмены.</param>
    /// <returns>Результат измерения.</returns>
    static private async Task<double> GetMeasurementResult(double voltage, int delay, IFastMeter meter)
    {
      await Task.Delay(delay);
      double result = await meter.DcVoltageManager.MeasureDCVoltageAsync();
      LogInformation($"Измеренное напряжение: {result} В");
      return result;
    }
  }
}
