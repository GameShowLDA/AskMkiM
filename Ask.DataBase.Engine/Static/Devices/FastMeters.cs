using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;

namespace Ask.DataBase.Engine.Static.Devices;

/// <summary>
/// Статичный фасад для работы с быстрыми измерителями.
/// </summary>
public static class FastMeters
{
  public static Task<IFastMeter?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetByIdAsync<IFastMeter>(id, cancellationToken);

  public static Task<List<IFastMeter>> GetAllAsync(CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetAllAsync<IFastMeter>(cancellationToken);

  public static Task<IFastMeter?> GetByNumberAsync(int number, CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetByNumberAsync<IFastMeter>(number, cancellationToken);

  public static Task<List<IFastMeter>> GetDevicesByNumberChassisAsync(int numberChassis, CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetDevicesByNumberChassisAsync<IFastMeter>(numberChassis, cancellationToken);

  public static Task<IFastMeter?> GetDeviceByNumberChassisAsync(int numberChassis, int number, CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetDeviceByNumberChassisAsync<IFastMeter>(numberChassis, number, cancellationToken);

  public static Task<IFastMeter> CreateAsync(IFastMeter device, CancellationToken cancellationToken = default) =>
    DeviceRuntime.CreateAsync(device, cancellationToken);

  public static Task<IFastMeter> UpdateAsync(IFastMeter device, CancellationToken cancellationToken = default) =>
    DeviceRuntime.UpdateAsync(device, cancellationToken);

  public static Task<bool> DeleteAsync(IFastMeter device, CancellationToken cancellationToken = default) =>
    DeviceRuntime.DeleteAsync(device, cancellationToken);

  public static Task<bool> DeleteByIdAsync(int id, CancellationToken cancellationToken = default) =>
    DeviceRuntime.DeleteByIdAsync<IFastMeter>(id, cancellationToken);
}
