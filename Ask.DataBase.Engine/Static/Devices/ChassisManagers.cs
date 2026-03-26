using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis;

namespace Ask.DataBase.Engine.Static.Devices;

/// <summary>
/// Статичный фасад для работы с менеджерами шасси.
/// </summary>
public static class ChassisManagers
{
  public static Task<IChassisManager?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetByIdAsync<IChassisManager>(id, cancellationToken);

  public static Task<List<IChassisManager>> GetAllAsync(CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetAllAsync<IChassisManager>(cancellationToken);

  public static Task<IChassisManager?> GetByNumberAsync(int number, CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetByNumberAsync<IChassisManager>(number, cancellationToken);

  public static Task<IChassisManager> CreateAsync(IChassisManager device, CancellationToken cancellationToken = default) =>
    DeviceRuntime.CreateAsync(device, cancellationToken);

  public static Task<IChassisManager> UpdateAsync(IChassisManager device, CancellationToken cancellationToken = default) =>
    DeviceRuntime.UpdateAsync(device, cancellationToken);

  public static Task<bool> DeleteAsync(IChassisManager device, CancellationToken cancellationToken = default) =>
    DeviceRuntime.DeleteAsync(device, cancellationToken);

  public static Task<bool> DeleteByIdAsync(int id, CancellationToken cancellationToken = default) =>
    DeviceRuntime.DeleteByIdAsync<IChassisManager>(id, cancellationToken);
}
