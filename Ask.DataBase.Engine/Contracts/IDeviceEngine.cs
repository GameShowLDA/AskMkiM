using Ask.Core.Shared.DTO.Devices.Base;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;

namespace Ask.DataBase.Engine.Contracts;

/// <summary>
/// Движок работы с устройствами.
/// Принимает нужный интерфейс устройства и возвращает готовый runtime-объект
/// для дальнейшего управления оборудованием.
/// </summary>
public interface IDeviceEngine
{
  Task<TDevice?> GetByIdAsync<TDevice>(int id, CancellationToken cancellationToken = default)
    where TDevice : class, IDevice;

  Task<List<TDevice>> GetAllAsync<TDevice>(CancellationToken cancellationToken = default)
    where TDevice : class, IDevice;

  Task<TDevice?> GetByNumberAsync<TDevice>(int number, CancellationToken cancellationToken = default)
    where TDevice : class, IDevice;

  Task<List<TDevice>> GetDevicesByNumberChassisAsync<TDevice>(
    int numberChassis,
    CancellationToken cancellationToken = default)
    where TDevice : class, IDevice;

  Task<TDevice?> GetDeviceByNumberChassisAsync<TDevice>(
    int numberChassis,
    int number,
    CancellationToken cancellationToken = default)
    where TDevice : class, IDevice;

  Task<TDevice?> ReloadByIdAsync<TDevice>(int id, CancellationToken cancellationToken = default)
    where TDevice : class, IDevice;

  Task<TDevice> CreateAsync<TDevice>(TDevice device, CancellationToken cancellationToken = default)
    where TDevice : class, IDevice;

  Task<TDevice> UpdateAsync<TDevice>(TDevice device, CancellationToken cancellationToken = default)
    where TDevice : class, IDevice;

  Task<bool> DeleteAsync<TDevice>(TDevice device, CancellationToken cancellationToken = default)
    where TDevice : class, IDevice;

  Task<bool> DeleteByIdAsync<TDevice>(int id, CancellationToken cancellationToken = default)
    where TDevice : class, IDevice;

  TDevice Build<TDevice>(DeviceDto dto)
    where TDevice : class, IDevice;

  void ClearCache();
}
