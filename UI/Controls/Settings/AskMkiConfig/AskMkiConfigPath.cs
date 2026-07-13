using Ask.Core.Services.Config.LegacyMki;
using System;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;

namespace UI.Controls.Settings.AskMkiConfig;

/// <summary>
/// Чтение и запись legacy-полей по пути.
/// </summary>
public partial class AskMkiConfigControl
{
  /// <summary>
  /// Находит ближайшего визуального родителя указанного типа.
  /// </summary>
  private static T? FindVisualParent<T>(DependencyObject? source)
    where T : DependencyObject
  {
    while (source != null)
    {
      if (source is T target)
      {
        return target;
      }

      source = VisualTreeHelper.GetParent(source);
    }

    return null;
  }

  private LegacyMkiHardwareProfile CurrentProfile()
  {
    if (_currentProfile == null)
    {
      throw new InvalidOperationException(UiString("AskMki.Error.ProfileNotLoaded"));
    }

    return _currentProfile;
  }

  /// <summary>
  /// Читает значение объекта по точечному пути с поддержкой индексов массивов.
  /// </summary>
  private static object? GetValueByPath(object source, string path)
  {
    object? current = source;

    foreach (var segment in path.Split('.'))
    {
      if (current == null)
      {
        return null;
      }

      current = GetSegmentValue(current, segment);
    }

    return current;
  }

  /// <summary>
  /// Определяет тип значения по точечному пути с поддержкой индексов массивов.
  /// </summary>
  private static Type GetValueTypeByPath(object source, string path)
  {
    object? current = source;
    var currentType = source.GetType();

    foreach (var segment in path.Split('.'))
    {
      var parsed = ParseSegment(segment);
      var property = currentType.GetProperty(parsed.PropertyName, BindingFlags.Instance | BindingFlags.Public)
        ?? throw new InvalidOperationException($"Свойство \"{parsed.PropertyName}\" не найдено в {currentType.Name}.");

      if (parsed.Index == null)
      {
        currentType = property.PropertyType;
        current = property.GetValue(current);
        continue;
      }

      var array = property.GetValue(current) as Array
        ?? throw new InvalidOperationException($"Свойство \"{parsed.PropertyName}\" не является массивом.");

      currentType = property.PropertyType.GetElementType()
        ?? throw new InvalidOperationException($"Не удалось определить тип элемента массива \"{parsed.PropertyName}\".");

      current = array.GetValue(parsed.Index.Value);
    }

    return currentType;
  }

  /// <summary>
  /// Записывает значение объекта по точечному пути с поддержкой индексов массивов.
  /// </summary>
  private static void SetValueByPath(object source, string path, object? value)
  {
    var parts = path.Split('.');
    object? current = source;

    for (var index = 0; index < parts.Length - 1; index++)
    {
      current = GetSegmentValue(current!, parts[index]);
    }

    if (current == null)
    {
      throw new InvalidOperationException($"Не удалось записать значение по пути \"{path}\".");
    }

    SetSegmentValue(current, parts[^1], value);
  }

  /// <summary>
  /// Читает значение одного сегмента пути.
  /// </summary>
  private static object? GetSegmentValue(object source, string segment)
  {
    var parsed = ParseSegment(segment);
    var property = source.GetType().GetProperty(parsed.PropertyName, BindingFlags.Instance | BindingFlags.Public)
      ?? throw new InvalidOperationException($"Свойство \"{parsed.PropertyName}\" не найдено в {source.GetType().Name}.");

    var value = property.GetValue(source);

    if (parsed.Index == null)
    {
      return value;
    }

    if (value is not Array array)
    {
      throw new InvalidOperationException($"Свойство \"{parsed.PropertyName}\" не является массивом.");
    }

    return array.GetValue(parsed.Index.Value);
  }

  /// <summary>
  /// Записывает значение одного сегмента пути.
  /// </summary>
  private static void SetSegmentValue(object source, string segment, object? value)
  {
    var parsed = ParseSegment(segment);
    var property = source.GetType().GetProperty(parsed.PropertyName, BindingFlags.Instance | BindingFlags.Public)
      ?? throw new InvalidOperationException($"Свойство \"{parsed.PropertyName}\" не найдено в {source.GetType().Name}.");

    if (parsed.Index == null)
    {
      property.SetValue(source, ConvertObjectToType(value, property.PropertyType));
      return;
    }

    if (property.GetValue(source) is not Array array)
    {
      throw new InvalidOperationException($"Свойство \"{parsed.PropertyName}\" не является массивом.");
    }

    var elementType = property.PropertyType.GetElementType()
      ?? throw new InvalidOperationException($"Не удалось определить тип элемента массива \"{parsed.PropertyName}\".");

    array.SetValue(ConvertObjectToType(value, elementType), parsed.Index.Value);
  }

  /// <summary>
  /// Разбирает сегмент пути на имя свойства и необязательный индекс массива.
  /// </summary>
  private static (string PropertyName, int? Index) ParseSegment(string segment)
  {
    var match = Regex.Match(segment, @"^(?<name>[A-Za-z0-9_]+)(\[(?<index>\d+)\])?$");

    if (!match.Success)
    {
      throw new InvalidOperationException($"Некорректный сегмент пути: \"{segment}\".");
    }

    var propertyName = match.Groups["name"].Value;
    var indexGroup = match.Groups["index"];

    return indexGroup.Success
      ? (propertyName, int.Parse(indexGroup.Value, CultureInfo.InvariantCulture))
      : (propertyName, null);
  }

  /// <summary>
  /// Преобразует значение legacy-поля к логическому признаку включения.
  /// </summary>
  private static bool IsTruthy(object? value)
  {
    return value switch
    {
      null => false,
      bool boolValue => boolValue,
      byte byteValue => byteValue != 0,
      ushort ushortValue => ushortValue != 0,
      short shortValue => shortValue != 0,
      int intValue => intValue != 0,
      double doubleValue => Math.Abs(doubleValue) > double.Epsilon,
      _ => !string.IsNullOrWhiteSpace(value.ToString())
    };
  }

  /// <summary>
  /// Преобразует значение переключателя к целевому типу legacy-поля.
  /// </summary>
  private static object ConvertToggleToType(bool value, Type targetType)
  {
    return ConvertTextToType(value ? "1" : "0", targetType);
  }

  /// <summary>
  /// Преобразует текстовое значение редактора к целевому типу legacy-поля.
  /// </summary>
  private static object ConvertTextToType(string? text, Type targetType)
  {
    text ??= string.Empty;

    if (targetType == typeof(string))
    {
      return text;
    }

    if (targetType == typeof(byte))
    {
      return byte.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }

    if (targetType == typeof(ushort))
    {
      return ushort.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }

    if (targetType == typeof(short))
    {
      return short.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }

    if (targetType == typeof(int))
    {
      return int.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }

    if (targetType == typeof(double))
    {
      return double.Parse(text.Replace(',', '.'), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture);
    }

    if (targetType == typeof(bool))
    {
      return text == "1" || bool.Parse(text);
    }

    return Convert.ChangeType(text, targetType, CultureInfo.InvariantCulture);
  }

  /// <summary>
  /// Преобразует произвольное значение к целевому типу legacy-поля.
  /// </summary>
  private static object ConvertObjectToType(object? value, Type targetType)
  {
    if (value != null && targetType.IsInstanceOfType(value))
    {
      return value;
    }

    return ConvertTextToType(value?.ToString(), targetType);
  }
}
