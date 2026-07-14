using System.Collections.Concurrent;
using System.Reflection;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.DataBase.Provider.Services.Devices;

namespace Ask.Engine.Tests.SelfControl;

internal static class SelfCheckDeviceRuntime
{
  private static readonly ConcurrentDictionary<string, Type> TypeCache = new();

  public static async Task<List<ISwitchingDevice>> GetSwitchingDevicesByNumberChassisAsync(
    int numberChassis,
    CancellationToken cancellationToken = default)
  {
    var service = new SwitchingDeviceDtoService();
    var devices = await service.GetDevicesByNumberChassisAsync(numberChassis, cancellationToken);
    return devices.Select(CreateDevice<ISwitchingDevice>).ToList();
  }

  public static async Task<List<IRelaySwitchModule>> GetRelaySwitchModulesByNumberChassisAsync(
    int numberChassis,
    CancellationToken cancellationToken = default)
  {
    var service = new RelaySwitchModuleDtoService();
    var devices = await service.GetDevicesByNumberChassisAsync(numberChassis, cancellationToken);
    return devices.Select(CreateDevice<IRelaySwitchModule>).ToList();
  }

  public static async Task<List<IMultimeter>> GetFastMetersByNumberChassisAsync(
    int numberChassis,
    CancellationToken cancellationToken = default)
  {
    var service = new FastMeterDtoService();
    var devices = await service.GetDevicesByNumberChassisAsync(numberChassis, cancellationToken);
    return devices.Select(CreateDevice<IMultimeter>).ToList();
  }

  public static async Task<List<IChassisManager>> GetChassisManagersAsync(
    CancellationToken cancellationToken = default)
  {
    var service = new ChassisManagerDtoService();
    var devices = await service.GetAllAsync(cancellationToken);
    return devices.Select(CreateDevice<IChassisManager>).ToList();
  }

  private static TDevice CreateDevice<TDevice>(object dto)
    where TDevice : class, IDevice
  {
    var deviceClass = GetDeviceClass(dto);
    var type = ResolveDeviceType(deviceClass);

    if (Activator.CreateInstance(type) is not TDevice device)
    {
      throw new InvalidOperationException(
        $"Класс '{deviceClass}' не удалось создать как '{typeof(TDevice).Name}'.");
    }

    ApplyProperties(dto, device);
    return device;
  }

  private static string GetDeviceClass(object dto)
  {
    var property = dto.GetType().GetProperty("DeviceClass");
    var value = property?.GetValue(dto) as string;

    if (string.IsNullOrWhiteSpace(value))
    {
      throw new InvalidOperationException("В настройках устройства не задан DeviceClass.");
    }

    return value;
  }

  private static Type ResolveDeviceType(string deviceClass)
  {
    return TypeCache.GetOrAdd(deviceClass, static key =>
    {
      var type = Type.GetType(key, throwOnError: false);
      if (type != null)
      {
        return type;
      }

      foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
      {
        type = assembly.GetType(key, throwOnError: false, ignoreCase: false);
        if (type != null)
        {
          return type;
        }
      }

      var runtimeAssembly = AppDomain.CurrentDomain.GetAssemblies()
        .FirstOrDefault(x => x.GetName().Name == "Ask.Device.Runtime")
        ?? Assembly.Load(new AssemblyName("Ask.Device.Runtime"));

      type = runtimeAssembly.GetType(key, throwOnError: false, ignoreCase: false);
      return type ?? throw new InvalidOperationException($"Тип устройства '{key}' не найден.");
    });
  }

  private static void ApplyProperties(object source, object target)
  {
    var targetProperties = target.GetType()
      .GetProperties(BindingFlags.Instance | BindingFlags.Public)
      .Where(x => x.CanWrite)
      .ToDictionary(x => x.Name, StringComparer.Ordinal);

    foreach (var sourceProperty in source.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
    {
      if (!sourceProperty.CanRead ||
          !targetProperties.TryGetValue(sourceProperty.Name, out var targetProperty))
      {
        continue;
      }

      var value = sourceProperty.GetValue(source);
      if (value == null)
      {
        if (!targetProperty.PropertyType.IsValueType ||
            Nullable.GetUnderlyingType(targetProperty.PropertyType) != null)
        {
          targetProperty.SetValue(target, null);
        }

        continue;
      }

      if (targetProperty.PropertyType.IsInstanceOfType(value))
      {
        targetProperty.SetValue(target, value);
      }
      else
      {
        var targetType = Nullable.GetUnderlyingType(targetProperty.PropertyType) ?? targetProperty.PropertyType;
        targetProperty.SetValue(target, Convert.ChangeType(value, targetType));
      }
    }
  }
}
