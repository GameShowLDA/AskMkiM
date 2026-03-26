using Ask.Core.Shared.Interfaces.DeviceInterfaces.UninterruptiblePowerSupply;

namespace Ask.DataBase.Engine.Static.Devices;

/// <summary>
/// Статичный фасад для работы с источниками бесперебойного питания.
/// </summary>
public static class UninterruptiblePowerSupplies
{
  public static Task<IUninterruptiblePowerSupply?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetByIdAsync<IUninterruptiblePowerSupply>(id, cancellationToken);

  public static Task<List<IUninterruptiblePowerSupply>> GetAllAsync(CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetAllAsync<IUninterruptiblePowerSupply>(cancellationToken);

  public static Task<IUninterruptiblePowerSupply?> GetByNumberAsync(int number, CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetByNumberAsync<IUninterruptiblePowerSupply>(number, cancellationToken);

  public static Task<List<IUninterruptiblePowerSupply>> GetDevicesByNumberChassisAsync(int numberChassis, CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetDevicesByNumberChassisAsync<IUninterruptiblePowerSupply>(numberChassis, cancellationToken);

  public static Task<IUninterruptiblePowerSupply?> GetDeviceByNumberChassisAsync(int numberChassis, int number, CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetDeviceByNumberChassisAsync<IUninterruptiblePowerSupply>(numberChassis, number, cancellationToken);

  public static Task<IUninterruptiblePowerSupply> CreateAsync(IUninterruptiblePowerSupply device, CancellationToken cancellationToken = default) =>
    DeviceRuntime.CreateAsync(device, cancellationToken);

  public static Task<IUninterruptiblePowerSupply> UpdateAsync(IUninterruptiblePowerSupply device, CancellationToken cancellationToken = default) =>
    DeviceRuntime.UpdateAsync(device, cancellationToken);

  public static Task<bool> DeleteAsync(IUninterruptiblePowerSupply device, CancellationToken cancellationToken = default) =>
    DeviceRuntime.DeleteAsync(device, cancellationToken);

  public static Task<bool> DeleteByIdAsync(int id, CancellationToken cancellationToken = default) =>
    DeviceRuntime.DeleteByIdAsync<IUninterruptiblePowerSupply>(id, cancellationToken);
}
