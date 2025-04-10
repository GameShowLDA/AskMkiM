using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ConsoleUtilities.Core;
using ConsoleUtilities.Models;
using DataBaseConfiguration.Models.Device;
using DataBaseConfiguration.Services;
using NewCore.Base.DeviceResponses;
using NewCore.Device;
using static NewCore.Enum.DeviceEnum;

namespace ConsoleUtilities.Commands
{
  internal class PowerSourceCalibrationEditorCommand : ICommand
  {
    public string Name => "powerSourceCalibration";

    public async Task ExecuteAsync(string[] args, CommandContext context)
    {
      Console.WriteLine("=== Редактор калибровки сопротивления ===");

      var service = new PowerSourceModuleServices();
      var modules = service.GetAllEntities().OfType<PowerSourceModuleEntity>().ToList();

      if (modules == null || !modules.Any())
      {
        Console.WriteLine("Нет доступных модулей источника питания.");
        return;
      }

      var selected = SelectModule(modules);
      if (selected == null) return;

      var editableModule = new ModuleVoltageCurrentSource();
      CopyProperties(selected, editableModule);
      EditResistanceCalibration(selected, service);
    }

    private static PowerSourceModuleEntity SelectModule(List<PowerSourceModuleEntity> modules)
    {
      Console.WriteLine("Выберите модуль:");
      for (int i = 0; i < modules.Count; i++)
      {
        Console.WriteLine($"{i + 1}. {modules[i].Name}");
      }

      while (true)
      {
        Console.Write("Введите номер модуля: ");
        if (int.TryParse(Console.ReadLine(), out int index) &&
            index >= 1 && index <= modules.Count)
        {
          return modules[index - 1];
        }

        Console.WriteLine("Неверный выбор. Попробуйте снова.");
      }
    }

    /// <summary>
    /// Запускает цикл редактирования диапазонов сопротивления с калибровочными коэффициентами.
    /// </summary>
    /// <param name="selected">Сущность модуля из базы.</param>
    /// <param name="service">Сервис для сохранения изменений.</param>
    private static void EditResistanceCalibration(PowerSourceModuleEntity selected, PowerSourceModuleServices service)
    {
      while (true)
      {
        // Считываем актуальные данные из JSON (если есть)
        var editableModule = new ModuleVoltageCurrentSource();
        editableModule.ResistanceCalibration = string.IsNullOrWhiteSpace(selected.ResistanceCalibrationJson)
          ? new()
          : JsonSerializer.Deserialize<List<ResistanceCalibrationRange>>(selected.ResistanceCalibrationJson!) ?? new();

        int rangeIndex = PromptRangeSelection();
        if (rangeIndex == 0) break;

        var range = GetPredefinedRange(rangeIndex);

        // Пробуем найти существующий диапазон
        var existing = editableModule.ResistanceCalibration
          .FirstOrDefault(r => r.ResistanceMin == range.ResistanceMin && r.ResistanceMax == range.ResistanceMax);

        if (existing != null)
        {
          Console.WriteLine($"Текущие значения для диапазона {range.ResistanceMin}–{range.ResistanceMax} Ом:");
          Console.WriteLine($"Ток: {existing.IntegerCurrent}.{existing.DecimalCurrent}");
          Console.WriteLine($"Напряжение: {existing.Voltage}");
        }
        else
        {
          Console.WriteLine($"Для диапазона {range.ResistanceMin}–{range.ResistanceMax} Ом пока нет сохранённых коэффициентов.");
        }

        // Удаляем старую версию диапазона
        editableModule.ResistanceCalibration.RemoveAll(r => r.ResistanceMin == range.ResistanceMin && r.ResistanceMax == range.ResistanceMax);

        // Вводим новые значения
        var updatedRange = new ResistanceCalibrationRange
        {
          ResistanceMin = range.ResistanceMin,
          ResistanceMax = range.ResistanceMax,
          IntegerCurrent = PromptInt("Введите целую часть тока (мА): "),
          DecimalCurrent = PromptInt("Введите дробную часть тока (мА): "),
          IntegerCurrentFake = PromptInt("Введите дробную часть фейкового тока (мА): "),
          DecimalCurrentFake = PromptInt("Введите дробную часть фейкового тока (мА): "),
        };
        var voltage = PromptVoltageSource();

        editableModule.ResistanceCalibration.Add(updatedRange);
        selected.ResistanceCalibrationJson = JsonSerializer.Serialize(editableModule.ResistanceCalibration);
        service.Update(selected);
      }
    }

    private static ResistanceCalibrationRange GetPredefinedRange(int index) => index switch
    {
      1 => new ResistanceCalibrationRange { ResistanceMin = 0, ResistanceMax = 100 },
      2 => new ResistanceCalibrationRange { ResistanceMin = 100, ResistanceMax = 1000 },
      3 => new ResistanceCalibrationRange { ResistanceMin = 1000, ResistanceMax = 10000 },
      4 => new ResistanceCalibrationRange { ResistanceMin = 10000, ResistanceMax = 100000 },
      _ => throw new ArgumentOutOfRangeException(nameof(index), "Недопустимый индекс диапазона.")
    };

    private static int PromptRangeSelection()
    {
      Console.WriteLine("\nВыберите диапазон (1–4) или 0 для выхода:");
      Console.WriteLine("1. 0 – 100 Ом");
      Console.WriteLine("2. 101 – 1000 Ом");
      Console.WriteLine("3. 1001 – 10000 Ом");
      Console.WriteLine("4. 10001 – 100000 Ом");

      while (true)
      {
        Console.Write("Ваш выбор: ");
        if (int.TryParse(Console.ReadLine(), out int input) && input >= 0 && input <= 4)
          return input;

        Console.WriteLine("Неверный ввод. Введите число от 0 до 4.");
      }
    }

    /// <summary>
    /// Запрашивает у пользователя выбор источника напряжения из перечисления VoltageSources.
    /// </summary>
    private static VoltageSources PromptVoltageSource()
    {
      var values = Enum.GetValues(typeof(VoltageSources)).Cast<VoltageSources>().ToList();

      Console.WriteLine("Выберите источник напряжения:");

      for (int i = 0; i < values.Count; i++)
      {
        Console.WriteLine($"{i + 1}. {values[i]}");
      }

      while (true)
      {
        Console.Write("Ваш выбор: ");
        if (int.TryParse(Console.ReadLine(), out int input) &&
            input >= 1 && input <= values.Count)
        {
          return values[input - 1];
        }

        Console.WriteLine("Неверный ввод. Попробуйте снова.");
      }
    }
    private static int PromptInt(string message)
    {
      while (true)
      {
        Console.Write(message);
        if (int.TryParse(Console.ReadLine(), out int value))
          return value;

        Console.WriteLine("Ошибка ввода. Введите целое число.");
      }
    }

    private static void CopyProperties(object source, object target)
    {
      if (source == null || target == null) return;

      var sourceProps = source.GetType().GetProperties();
      var targetProps = target.GetType().GetProperties().ToDictionary(p => p.Name);

      foreach (var prop in sourceProps)
      {
        if (targetProps.TryGetValue(prop.Name, out var targetProp) &&
            targetProp.CanWrite &&
            targetProp.PropertyType == prop.PropertyType)
        {
          var value = prop.GetValue(source);
          if (value != null)
          {
            targetProp.SetValue(target, value);
          }
        }
      }
    }
  }
}