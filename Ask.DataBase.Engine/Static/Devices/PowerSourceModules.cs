using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule;

namespace Ask.DataBase.Engine.Static.Devices;

/// <summary>
/// Статичный фасад для работы с модулями источника питания.
/// </summary>
public static class PowerSourceModules
{
  public static Task<IPowerSourceModule?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetByIdAsync<IPowerSourceModule>(id, cancellationToken);

  public static Task<List<IPowerSourceModule>> GetAllAsync(CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetAllAsync<IPowerSourceModule>(cancellationToken);

  public static Task<IPowerSourceModule?> GetByNumberAsync(int number, CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetByNumberAsync<IPowerSourceModule>(number, cancellationToken);

  public static Task<List<IPowerSourceModule>> GetDevicesByNumberChassisAsync(int numberChassis, CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetDevicesByNumberChassisAsync<IPowerSourceModule>(numberChassis, cancellationToken);

  public static Task<IPowerSourceModule?> GetDeviceByNumberChassisAsync(int numberChassis, int number, CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetDeviceByNumberChassisAsync<IPowerSourceModule>(numberChassis, number, cancellationToken);

  public static Task<IPowerSourceModule> CreateAsync(IPowerSourceModule device, CancellationToken cancellationToken = default) =>
    DeviceRuntime.CreateAsync(device, cancellationToken);

  public static Task<IPowerSourceModule> UpdateAsync(IPowerSourceModule device, CancellationToken cancellationToken = default) =>
    DeviceRuntime.UpdateAsync(device, cancellationToken);

  public static Task<bool> DeleteAsync(IPowerSourceModule device, CancellationToken cancellationToken = default) =>
    DeviceRuntime.DeleteAsync(device, cancellationToken);

  public static Task<bool> DeleteByIdAsync(int id, CancellationToken cancellationToken = default) =>
    DeviceRuntime.DeleteByIdAsync<IPowerSourceModule>(id, cancellationToken);
}
