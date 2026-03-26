using Ask.Core.Shared.DTO.Devices.Base;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.DataBase.Engine.Contracts;
using Ask.DataBase.Engine.Services;

namespace Ask.DataBase.Engine.Static;

/// <summary>
/// Универсальный статичный вход в движок устройств.
/// Нужен для случаев, когда удобнее работать без создания экземпляров сервисов вручную.
/// </summary>
public static class DeviceRuntime
{
  private static readonly IDeviceEngine Engine = new DeviceEngine();

  public static Task<TDevice?> GetByIdAsync<TDevice>(int id, CancellationToken cancellationToken = default)
    where TDevice : class, IDevice =>
    Engine.GetByIdAsync<TDevice>(id, cancellationToken);

  public static Task<List<TDevice>> GetAllAsync<TDevice>(CancellationToken cancellationToken = default)
    where TDevice : class, IDevice =>
    Engine.GetAllAsync<TDevice>(cancellationToken);

  public static Task<TDevice?> GetByNumberAsync<TDevice>(int number, CancellationToken cancellationToken = default)
    where TDevice : class, IDevice =>
    Engine.GetByNumberAsync<TDevice>(number, cancellationToken);

  public static Task<List<TDevice>> GetDevicesByNumberChassisAsync<TDevice>(
    int numberChassis,
    CancellationToken cancellationToken = default)
    where TDevice : class, IDevice =>
    Engine.GetDevicesByNumberChassisAsync<TDevice>(numberChassis, cancellationToken);

  public static Task<TDevice?> GetDeviceByNumberChassisAsync<TDevice>(
    int numberChassis,
    int number,
    CancellationToken cancellationToken = default)
    where TDevice : class, IDevice =>
    Engine.GetDeviceByNumberChassisAsync<TDevice>(numberChassis, number, cancellationToken);

  public static Task<TDevice?> ReloadByIdAsync<TDevice>(int id, CancellationToken cancellationToken = default)
    where TDevice : class, IDevice =>
    Engine.ReloadByIdAsync<TDevice>(id, cancellationToken);

  public static Task<TDevice> CreateAsync<TDevice>(TDevice device, CancellationToken cancellationToken = default)
    where TDevice : class, IDevice =>
    Engine.CreateAsync(device, cancellationToken);

  public static Task<TDevice> UpdateAsync<TDevice>(TDevice device, CancellationToken cancellationToken = default)
    where TDevice : class, IDevice =>
    Engine.UpdateAsync(device, cancellationToken);

  public static Task<bool> DeleteAsync<TDevice>(TDevice device, CancellationToken cancellationToken = default)
    where TDevice : class, IDevice =>
    Engine.DeleteAsync(device, cancellationToken);

  public static Task<bool> DeleteByIdAsync<TDevice>(int id, CancellationToken cancellationToken = default)
    where TDevice : class, IDevice =>
    Engine.DeleteByIdAsync<TDevice>(id, cancellationToken);

  public static TDevice Build<TDevice>(DeviceDto dto)
    where TDevice : class, IDevice =>
    Engine.Build<TDevice>(dto);

  public static void ClearCache() => Engine.ClearCache();
}
