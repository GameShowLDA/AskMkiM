using Ask.Core.Shared.Interfaces.DeviceInterfaces;

namespace Ask.DataBase.Engine.Factory;

/// <summary>
/// Фабрика для создания экземпляров устройств на основе имени класса.
/// Использует рефлексию и кэширует типы для повышения производительности.
/// </summary>
public static class DeviceFactory
{
  /// <summary>
  /// Кэш сопоставления имени класса устройства и его типа.
  /// Позволяет избежать повторного поиска через рефлексию.
  /// </summary>
  private static readonly Dictionary<string, Type> _typeCache = new();

  /// <summary>
  /// Создаёт экземпляр устройства по строковому имени класса.
  /// </summary>
  /// <param name="deviceClass">Полное имя типа устройства.</param>
  /// <returns>Экземпляр устройства.</returns>
  /// <exception cref="InvalidOperationException">
  /// Выбрасывается, если тип не найден или не может быть создан.
  /// </exception>
  public static IDevice Create(string deviceClass)
  {
    if (string.IsNullOrWhiteSpace(deviceClass))
      throw new ArgumentException("Device class is null or empty", nameof(deviceClass));

    if (!_typeCache.TryGetValue(deviceClass, out var type))
    {
      type = Type.GetType(deviceClass)
        ?? throw new InvalidOperationException($"Type not found: {deviceClass}");

      _typeCache[deviceClass] = type;
    }

    if (Activator.CreateInstance(type) is not IDevice device)
      throw new InvalidOperationException($"Cannot create device: {deviceClass}");

    return device;
  }
}