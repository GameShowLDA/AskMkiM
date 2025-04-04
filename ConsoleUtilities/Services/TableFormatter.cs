using ConsoleTables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ComponentModel.DataAnnotations.Schema;

internal class TableFormatter
{
  public static void DisplayTable<T>(List<T> records)
  {
    if (records == null || records.Count == 0)
    {
      Console.WriteLine("Нет данных для отображения.");
      return;
    }

    var type = typeof(T);

    var props = type
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p =>
            p.GetMethod != null &&
            !p.PropertyType.IsInterface &&
            !Attribute.IsDefined(p, typeof(NotMappedAttribute)))
        .ToList();

    var table = new ConsoleTable(props.Select(p => p.Name).ToArray());

    foreach (var record in records)
    {
      var row = props.Select(p =>
      {
        var value = p.GetValue(record)?.ToString() ?? "";
        return value.Length > 30 ? value.Substring(0, 27) + "..." : value;
      }).ToArray();

      table.AddRow(row);
    }

    table.Write(Format.MarkDown);
    Console.WriteLine();
  }
}
