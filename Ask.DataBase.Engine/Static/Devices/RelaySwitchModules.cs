using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;

namespace Ask.DataBase.Engine.Static.Devices;

/// <summary>
/// Статичный фасад для работы с модулями коммутации реле.
/// </summary>
public static class RelaySwitchModules
{
  public static Task<IRelaySwitchModule?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetByIdAsync<IRelaySwitchModule>(id, cancellationToken);

  public static Task<List<IRelaySwitchModule>> GetAllAsync(CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetAllAsync<IRelaySwitchModule>(cancellationToken);

  public static Task<IRelaySwitchModule?> GetByNumberAsync(int number, CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetByNumberAsync<IRelaySwitchModule>(number, cancellationToken);

  public static Task<List<IRelaySwitchModule>> GetDevicesByNumberChassisAsync(int numberChassis, CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetDevicesByNumberChassisAsync<IRelaySwitchModule>(numberChassis, cancellationToken);

  public static Task<IRelaySwitchModule?> GetDeviceByNumberChassisAsync(int numberChassis, int number, CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetDeviceByNumberChassisAsync<IRelaySwitchModule>(numberChassis, number, cancellationToken);

  public static Task<IRelaySwitchModule> CreateAsync(IRelaySwitchModule device, CancellationToken cancellationToken = default) =>
    DeviceRuntime.CreateAsync(device, cancellationToken);

  public static Task<IRelaySwitchModule> UpdateAsync(IRelaySwitchModule device, CancellationToken cancellationToken = default) =>
    DeviceRuntime.UpdateAsync(device, cancellationToken);

  public static Task<bool> DeleteAsync(IRelaySwitchModule device, CancellationToken cancellationToken = default) =>
    DeviceRuntime.DeleteAsync(device, cancellationToken);

  public static Task<bool> DeleteByIdAsync(int id, CancellationToken cancellationToken = default) =>
    DeviceRuntime.DeleteByIdAsync<IRelaySwitchModule>(id, cancellationToken);

  public static async Task<IRelaySwitchModule?> UpdateResistanceAsync(
    int numberChassis,
    int number,
    double value,
    CancellationToken cancellationToken = default)
  {
    var device = await GetDeviceByNumberChassisAsync(numberChassis, number, cancellationToken);
    if (device == null)
    {
      return null;
    }

    device.SwitchResistance = value;
    return await UpdateAsync(device, cancellationToken);
  }
}
