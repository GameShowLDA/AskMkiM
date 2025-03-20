using Mode.Metrology.MeasurementSystem;
using System;
using System.Collections.Generic;

namespace TestConsole
{
  /// <summary>
  /// Класс для проверки входных данных перед измерением.
  /// Позволяет выбрать ручной или автоматический режим.
  /// </summary>
  public static class InputValidator
  {
    /// <summary>
    /// Запускает валидацию в выбранном режиме.
    /// </summary>
    public static void Validate()
    {
      Console.WriteLine("\nВыберите режим:");
      Console.WriteLine("1. Ручной ввод данных");
      Console.WriteLine("2. Автоматический тест");

      Console.Write("Введите номер режима: ");
      string mode = Console.ReadLine();

      if (mode == "1")
      {
        ManualValidation();
      }
      else if (mode == "2")
      {
        AutomatedValidation();
      }
      else
      {
        Console.WriteLine("❌ Ошибка: Неверный выбор.");
      }
    }

    /// <summary>
    /// Выполняет ручную проверку данных.
    /// </summary>
    private static void ManualValidation()
    {
      Console.Write("\nВведите первую точку (A.B.C): ");
      string point1 = Console.ReadLine();

      Console.Write("Введите вторую точку (A.B.C): ");
      string point2 = Console.ReadLine();

      Console.Write("Введите измеряемое значение: ");
      string referenceValue = Console.ReadLine();

      ValidateAndPrintResult(point1, point2, referenceValue);
    }

    /// <summary>
    /// Выполняет автоматическое тестирование валидации входных данных.
    /// </summary>
    private static void AutomatedValidation()
    {
      Console.WriteLine("\n=== Запуск автоматического тестирования ===");

      var testCases = new List<(string point1, string point2, string value, string expected)>
    {
        // ✅ Корректные случаи
        ("1.1.0", "1.2.100", "10.5", "Данные корректны"),
        ("1.1.349", "1.2.349", "80.05", "Данные корректны"),
        ("1.2.50", "1.1.200", "0.001", "Данные корректны"),
        ("1.1.300", "1.2.100", "3.1415", "Данные корректны"),

        // ❌ Ошибки формата
        ("1.2.10", "1.2.20", "-5.5", "Некорректное число (отрицательное значение)"),
        ("1.1.-1", "1.2.0", "50", "Должна быть ошибка формата"),
        ("1..50", "1.2.0", "60", "Должна быть ошибка формата"),
        ("", "1.2.0", "70", "Должна быть ошибка формата"),
        ("1.1", "1.2.50", "100", "Должна быть ошибка формата"),
        ("abc.def.ghi", "1.2.50", "15", "Должна быть ошибка формата"),
        ("1.1.50.5", "1.2.50", "30", "Должна быть ошибка формата"),
        ("1.1.", "1.2.50", "40", "Должна быть ошибка формата"),

        // ❌ Несуществующее шасси
        ("2.1.50", "1.2.50", "20", "Шасси отсутствует в БД"),
        ("3.1.100", "1.1.50", "5.1", "Шасси отсутствует в БД"),

        // ❌ Несуществующий модуль
        ("1.3.50", "1.2.50", "30", "Модуль отсутствует в БД"),
        ("1.4.200", "1.1.0", "12.3", "Модуль отсутствует в БД"),

        // ❌ Точки вне диапазона
        ("1.1.350", "1.2.50", "40", "Выход за пределы точек"),
        ("1.2.351", "1.1.50", "3.5", "Выход за пределы точек"),
        ("1.2.500", "1.1.200", "0.1", "Выход за пределы точек"),
        ("1.1.-5", "1.2.50", "55", "Выход за пределы точек"),

        // ❌ Ошибки электрического параметра
        ("1.1.50", "1.2.50", "abc", "Некорректное число"),
        ("1.1.50", "1.2.50", "", "Пустая строка"),
        ("1.1.50", "1.2.50", " ", "Пустая строка"),
        ("1.1.50", "1.2.50", "NaN", "Некорректное число"),
        ("1.1.50", "1.2.50", "Infinity", "Некорректное число"),
        ("1.1.50", "1.2.50", "-Infinity", "Некорректное число")
    };

      foreach (var test in testCases)
      {
        Console.WriteLine($"\nТест: {test.point1}, {test.point2}, {test.value} ({test.expected})");
        ValidateAndPrintResult(test.point1, test.point2, test.value);
      }
    }


    /// <summary>
    /// Выполняет валидацию и выводит результат в консоль.
    /// </summary>
    /// <summary>
    /// Выполняет валидацию и выводит результат в консоль.
    /// </summary>
    private static void ValidateAndPrintResult(string point1, string point2, string referenceValue)
    {
      try
      {
        var testMeasurement = new TestMeasurement();
        testMeasurement.ValidateInput(point1, point2, referenceValue);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✅ Данные корректны!");
      }
      catch (Exception ex)
      {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"❌ {ex.Message}");
      }
      finally
      {
        Console.ResetColor();
      }
    }
  }

  /// <summary>
  /// Тестовый класс измерения для вызова ValidateInput().
  /// </summary>
  public class TestMeasurement : BaseMeasurement
  {
    public TestMeasurement() : base() { }

    /// <inheritdoc />
    protected override void ConfigureMultimeter()
    {
      // Заглушка для теста
    }
  }
}
