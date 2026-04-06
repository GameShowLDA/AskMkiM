using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using System.Collections.Concurrent;
using System.Reflection;

namespace Ask.DataBase.Engine.Factory;

/// <summary>
/// Фабрика для создания runtime-устройств по значению свойства <c>DeviceClass</c>.
/// Использует рефлексию, разрешение типа по сборкам и кэширование найденных типов.
/// </summary>
public static class DeviceFactory
{
  private static readonly ConcurrentDictionary<string, Type> TypeCache = new();
  private static readonly string[] KnownAssemblyNames =
  [
    "Ask.Device.Runtime",
  ];

  /// <summary>
  /// Создаёт экземпляр устройства по строковому имени класса.
  /// </summary>
  /// <param name="deviceClass">Полное имя типа устройства.</param>
  /// <returns>Экземпляр устройства.</returns>
  public static IDevice Create(string deviceClass)
  {
    var type = ResolveDeviceType(deviceClass);

    if (Activator.CreateInstance(type) is not IDevice device)
    {
      throw new InvalidOperationException(
        $"Не удалось создать устройство по классу '{deviceClass}'.");
    }

    return device;
  }

  /// <summary>
  /// Создаёт экземпляр устройства и возвращает его как нужный интерфейс устройства.
  /// </summary>
  /// <typeparam name="TDevice">Нужный интерфейс устройства.</typeparam>
  /// <param name="deviceClass">Полное имя типа устройства.</param>
  /// <returns>Экземпляр устройства нужного интерфейса.</returns>
  public static TDevice Create<TDevice>(string deviceClass)
    where TDevice : class, IDevice
  {
    var type = ResolveDeviceType(deviceClass);

    if (!typeof(TDevice).IsAssignableFrom(type))
    {
      throw new InvalidOperationException(
        $"Класс '{deviceClass}' не реализует интерфейс '{typeof(TDevice).Name}'.");
    }

    if (Activator.CreateInstance(type) is not TDevice device)
    {
      throw new InvalidOperationException(
        $"Не удалось создать устройство '{deviceClass}' как '{typeof(TDevice).Name}'.");
    }

    return device;
  }

  private static Type ResolveDeviceType(string deviceClass)
  {
    if (string.IsNullOrWhiteSpace(deviceClass))
    {
      throw new ArgumentException("Имя класса устройства не задано.", nameof(deviceClass));
    }

    return TypeCache.GetOrAdd(deviceClass, static key =>
    {
      var type = Type.GetType(key, throwOnError: false);
      if (type != null)
      {
        return type;
      }

      foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
      {
        try
        {
          type = assembly.GetType(key, throwOnError: false, ignoreCase: false);
          if (type != null)
          {
            return type;
          }
        }
        catch (ReflectionTypeLoadException)
        {
        }
      }

      foreach (var assemblyName in GetCandidateAssemblyNames(key))
      {
        try
        {
          var assembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(x => string.Equals(x.GetName().Name, assemblyName, StringComparison.Ordinal))
            ?? Assembly.Load(new AssemblyName(assemblyName));

          type = assembly.GetType(key, throwOnError: false, ignoreCase: false);
          if (type != null)
          {
            return type;
          }
        }
        catch
        {
        }
      }

      throw new InvalidOperationException($"Тип устройства '{key}' не найден.");
    });
  }

  private static IEnumerable<string> GetCandidateAssemblyNames(string deviceClass)
  {
    var commaIndex = deviceClass.IndexOf(',');
    if (commaIndex >= 0)
    {
      var assemblyName = deviceClass[(commaIndex + 1)..].Trim();
      if (!string.IsNullOrWhiteSpace(assemblyName))
      {
        yield return assemblyName;
      }
    }

    if (deviceClass.StartsWith("Ask.Device.Runtime.", StringComparison.Ordinal) ||
        deviceClass.StartsWith("NewCore.", StringComparison.Ordinal))
    {
      yield return "Ask.Device.Runtime";
    }

    foreach (var knownAssemblyName in KnownAssemblyNames)
    {
      yield return knownAssemblyName;
    }
  }
}
