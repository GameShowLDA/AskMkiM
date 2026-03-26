using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;

namespace Ask.DataBase.Engine.Static.Devices;

/// <summary>
/// Статичный фасад для работы с устройствами коммутации.
/// </summary>
public static class SwitchingDevices
{
  public static Task<ISwitchingDevice?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetByIdAsync<ISwitchingDevice>(id, cancellationToken);

  public static Task<List<ISwitchingDevice>> GetAllAsync(CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetAllAsync<ISwitchingDevice>(cancellationToken);

  public static Task<ISwitchingDevice?> GetByNumberAsync(int number, CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetByNumberAsync<ISwitchingDevice>(number, cancellationToken);

  public static Task<List<ISwitchingDevice>> GetDevicesByNumberChassisAsync(int numberChassis, CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetDevicesByNumberChassisAsync<ISwitchingDevice>(numberChassis, cancellationToken);

  public static Task<ISwitchingDevice?> GetDeviceByNumberChassisAsync(int numberChassis, int number, CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetDeviceByNumberChassisAsync<ISwitchingDevice>(numberChassis, number, cancellationToken);

  public static Task<ISwitchingDevice> CreateAsync(ISwitchingDevice device, CancellationToken cancellationToken = default) =>
    DeviceRuntime.CreateAsync(device, cancellationToken);

  public static Task<ISwitchingDevice> UpdateAsync(ISwitchingDevice device, CancellationToken cancellationToken = default) =>
    DeviceRuntime.UpdateAsync(device, cancellationToken);

  public static Task<bool> DeleteAsync(ISwitchingDevice device, CancellationToken cancellationToken = default) =>
    DeviceRuntime.DeleteAsync(device, cancellationToken);

  public static Task<bool> DeleteByIdAsync(int id, CancellationToken cancellationToken = default) =>
    DeviceRuntime.DeleteByIdAsync<ISwitchingDevice>(id, cancellationToken);
}
