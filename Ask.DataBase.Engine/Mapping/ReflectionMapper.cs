using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ask.DataBase.Engine.Mapping
{

  /// <summary>
  /// Универсальный маппер на основе рефлексии для копирования значений свойств
  /// между объектами с совпадающими именами и совместимыми типами.
  /// Поддерживает кэширование сопоставлений для повышения производительности.
  /// </summary>
  internal static class ReflectionMapper
  {
    /// <summary>
    /// Кэш сопоставлений свойств между типами источника и назначения.
    /// Ключ — пара типов (Source, Destination),
    /// значение — список пар свойств (источник → назначение).
    /// </summary>
    private static readonly Dictionary<(Type, Type), List<(PropertyInfo src, PropertyInfo dst)>> _cache = new();

    /// <summary>
    /// Создаёт новый экземпляр <typeparamref name="TDestination"/> и копирует в него
    /// значения свойств из <typeparamref name="TSource"/> по совпадающим именам и типам.
    /// </summary>
    /// <typeparam name="TSource">Тип источника.</typeparam>
    /// <typeparam name="TDestination">Тип назначения.</typeparam>
    /// <param name="source">Объект-источник.</param>
    /// <returns>Новый экземпляр <typeparamref name="TDestination"/> с заполненными данными.</returns>
    /// <exception cref="ArgumentNullException">Если source равен null.</exception>
    public static TDestination Map<TSource, TDestination>(TSource source)
      where TDestination : new()
    {
      if (source == null)
        throw new ArgumentNullException(nameof(source));

      var key = (typeof(TSource), typeof(TDestination));

      if (!_cache.TryGetValue(key, out var map))
      {
        map = BuildMap(typeof(TSource), typeof(TDestination));
        _cache[key] = map;
      }

      var destination = new TDestination();

      foreach (var (src, dst) in map)
      {
        var value = src.GetValue(source);
        dst.SetValue(destination, value);
      }

      return destination;
    }

    /// <summary>
    /// Копирует значения свойств из объекта-источника в уже существующий объект назначения.
    /// Использует сопоставление по имени и совместимости типов.
    /// </summary>
    /// <typeparam name="TSource">Тип источника.</typeparam>
    /// <typeparam name="TDestination">Тип назначения.</typeparam>
    /// <param name="source">Объект-источник.</param>
    /// <param name="destination">Объект назначения.</param>
    /// <exception cref="ArgumentNullException">Если source или destination равны null.</exception>
    public static void Apply<TSource, TDestination>(TSource source, TDestination destination)
    {
      if (source == null)
        throw new ArgumentNullException(nameof(source));

      if (destination == null)
        throw new ArgumentNullException(nameof(destination));

      var key = (typeof(TSource), typeof(TDestination));

      if (!_cache.TryGetValue(key, out var map))
      {
        map = BuildMap(typeof(TSource), typeof(TDestination));
        _cache[key] = map;
      }

      foreach (var (src, dst) in map)
      {
        var value = src.GetValue(source);
        dst.SetValue(destination, value);
      }
    }

    /// <summary>
    /// Строит сопоставление свойств между типами источника и назначения.
    /// В сопоставление включаются только свойства:
    /// - с одинаковыми именами;
    /// - доступные для чтения (источник) и записи (назначение);
    /// - с совместимыми типами.
    /// </summary>
    /// <param name="source">Тип источника.</param>
    /// <param name="destination">Тип назначения.</param>
    /// <returns>Список пар свойств для копирования.</returns>
    private static List<(PropertyInfo src, PropertyInfo dst)> BuildMap(Type source, Type destination)
    {
      var srcProps = GetPublicInstanceProperties(source);
      var dstProps = GetPublicInstanceProperties(destination);

      return (from s in srcProps
              join d in dstProps on s.Name equals d.Name
              where s.CanRead && d.CanWrite
              where d.PropertyType.IsAssignableFrom(s.PropertyType)
              select (s, d)).ToList();
    }

    private static IReadOnlyList<PropertyInfo> GetPublicInstanceProperties(Type type)
    {
      if (!type.IsInterface)
      {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
      }

      return type
        .GetInterfaces()
        .Append(type)
        .SelectMany(x => x.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        .GroupBy(x => (x.Name, x.PropertyType))
        .Select(x => x.First())
        .ToList();
    }
  }
}
