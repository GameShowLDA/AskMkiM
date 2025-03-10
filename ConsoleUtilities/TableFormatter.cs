using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ConsoleUtilities
{
  public static class TableFormatter
  {
    public static void DisplayTable<T>(List<T> records)
    {
      if (records == null || records.Count == 0)
      {
        Console.WriteLine("Таблица пуста.");
        return;
      }

      var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                .Where(p => p.PropertyType.IsPrimitive || p.PropertyType == typeof(string))
                                .ToList();

      // Определяем ширину колонок
      var columnWidths = properties.Select(p => Math.Max(p.Name.Length, records.Max(r => p.GetValue(r)?.ToString()?.Length ?? 0))).ToList();

      // Формируем заголовок таблицы
      string header = "| " + string.Join(" | ", properties.Select((p, i) => p.Name.PadRight(columnWidths[i]))) + " |";
      string separator = new string('-', header.Length);

      Console.WriteLine(separator);
      Console.WriteLine(header);
      Console.WriteLine(separator);

      // Выводим строки таблицы
      foreach (var record in records)
      {
        string row = "| " + string.Join(" | ", properties.Select((p, i) => (p.GetValue(record)?.ToString() ?? "").PadRight(columnWidths[i]))) + " |";
        Console.WriteLine(row);
      }

      Console.WriteLine(separator);
    }
  }
}
