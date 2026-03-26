using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;

namespace Ask.DataBase.Engine.Static.Devices;

/// <summary>
/// Статичный фасад для работы с пробойными установками.
/// </summary>
public static class BreakdownTesters
{
  public static Task<IBreakdownTester?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetByIdAsync<IBreakdownTester>(id, cancellationToken);

  public static Task<List<IBreakdownTester>> GetAllAsync(CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetAllAsync<IBreakdownTester>(cancellationToken);

  public static Task<IBreakdownTester?> GetByNumberAsync(int number, CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetByNumberAsync<IBreakdownTester>(number, cancellationToken);

  public static Task<List<IBreakdownTester>> GetDevicesByNumberChassisAsync(int numberChassis, CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetDevicesByNumberChassisAsync<IBreakdownTester>(numberChassis, cancellationToken);

  public static Task<IBreakdownTester?> GetDeviceByNumberChassisAsync(int numberChassis, int number, CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetDeviceByNumberChassisAsync<IBreakdownTester>(numberChassis, number, cancellationToken);

  public static Task<IBreakdownTester> CreateAsync(IBreakdownTester device, CancellationToken cancellationToken = default) =>
    DeviceRuntime.CreateAsync(device, cancellationToken);

  public static Task<IBreakdownTester> UpdateAsync(IBreakdownTester device, CancellationToken cancellationToken = default) =>
    DeviceRuntime.UpdateAsync(device, cancellationToken);

  public static Task<bool> DeleteAsync(IBreakdownTester device, CancellationToken cancellationToken = default) =>
    DeviceRuntime.DeleteAsync(device, cancellationToken);

  public static Task<bool> DeleteByIdAsync(int id, CancellationToken cancellationToken = default) =>
    DeviceRuntime.DeleteByIdAsync<IBreakdownTester>(id, cancellationToken);
}
