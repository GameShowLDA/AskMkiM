using Ask.Core.Shared.DTO.Devices.Base;
using Ask.Core.Shared.DTO.Devices.Breakdown;
using Ask.Core.Shared.DTO.Devices.ChassisManager;
using Ask.Core.Shared.DTO.Devices.FastMeter;
using Ask.Core.Shared.DTO.Devices.PowerSourceModule;
using Ask.Core.Shared.DTO.Devices.Rack;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Devices.SwitchingDevice;
using Ask.Core.Shared.DTO.Devices.UninterruptiblePowerSupply;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Rack;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.UninterruptiblePowerSupply;
using Ask.DataBase.Engine.Builder;
using Ask.DataBase.Engine.Contracts;
using Ask.DataBase.Engine.Mapping;
using Ask.DataBase.Engine.Mapping.Device;
using Ask.DataBase.Provider.Services.Base;
using Ask.DataBase.Provider.Services.Devices;
using System.Collections.Concurrent;

namespace Ask.DataBase.Engine.Services;

/// <summary>
/// Движок получения runtime-устройств из DTO-провайдера.
/// Сам определяет нужный DTO-сервис по запрошенному интерфейсу устройства,
/// создаёт runtime-класс по свойству <c>DeviceClass</c>
/// и возвращает готовый интерфейс устройства вместо <see cref="IDevice"/>.
/// </summary>
public class DeviceEngine : IDeviceEngine
{
  private readonly record struct ChassisQueryCacheKey(Type DeviceType, int NumberChassis);

  private readonly DeviceCache _cache;
  private readonly BreakdownTesterDtoService _breakdownTesterService;
  private readonly ChassisManagerDtoService _chassisManagerService;
  private readonly FastMeterDtoService _fastMeterService;
  private readonly PowerSourceModuleDtoService _powerSourceModuleService;
  private readonly RackDtoService _rackService;
  private readonly RelaySwitchModuleDtoService _relaySwitchModuleService;
  private readonly SwitchingDeviceDtoService _switchingDeviceService;
  private readonly UninterruptiblePowerSupplyDtoService _uninterruptiblePowerSupplyService;
  private readonly ConcurrentDictionary<Type, int[]> _allQueryCache = new();
  private readonly ConcurrentDictionary<ChassisQueryCacheKey, int[]> _chassisQueryCache = new();

  /// <summary>
  /// Инициализирует новый экземпляр движка устройств.
  /// </summary>
  public DeviceEngine(
    DeviceCache? cache = null,
    BreakdownTesterDtoService? breakdownTesterService = null,
    ChassisManagerDtoService? chassisManagerService = null,
    FastMeterDtoService? fastMeterService = null,
    PowerSourceModuleDtoService? powerSourceModuleService = null,
    RackDtoService? rackService = null,
    RelaySwitchModuleDtoService? relaySwitchModuleService = null,
    SwitchingDeviceDtoService? switchingDeviceService = null,
    UninterruptiblePowerSupplyDtoService? uninterruptiblePowerSupplyService = null)
  {
    _cache = cache ?? new DeviceCache();
    _breakdownTesterService = breakdownTesterService ?? new BreakdownTesterDtoService();
    _chassisManagerService = chassisManagerService ?? new ChassisManagerDtoService();
    _fastMeterService = fastMeterService ?? new FastMeterDtoService();
    _powerSourceModuleService = powerSourceModuleService ?? new PowerSourceModuleDtoService();
    _rackService = rackService ?? new RackDtoService();
    _relaySwitchModuleService = relaySwitchModuleService ?? new RelaySwitchModuleDtoService();
    _switchingDeviceService = switchingDeviceService ?? new SwitchingDeviceDtoService();
    _uninterruptiblePowerSupplyService = uninterruptiblePowerSupplyService ?? new UninterruptiblePowerSupplyDtoService();
  }

  /// <inheritdoc/>
  public Task<TDevice?> GetByIdAsync<TDevice>(int id, CancellationToken cancellationToken = default)
    where TDevice : class, IDevice
  {
    var requestedType = typeof(TDevice);

    if (requestedType == typeof(IBreakdownTester))
      return GetByIdInternalAsync<TDevice, BreakdownTesterDto>(_breakdownTesterService, id, cancellationToken);
    if (requestedType == typeof(IChassisManager))
      return GetByIdInternalAsync<TDevice, ChassisManagerDto>(_chassisManagerService, id, cancellationToken);
    if (requestedType == typeof(IFastMeter))
      return GetByIdInternalAsync<TDevice, FastMeterDto>(_fastMeterService, id, cancellationToken);
    if (requestedType == typeof(IPowerSourceModule))
      return GetByIdInternalAsync<TDevice, PowerSourceModuleDto>(_powerSourceModuleService, id, cancellationToken);
    if (requestedType == typeof(IRack))
      return GetByIdInternalAsync<TDevice, RackDto>(_rackService, id, cancellationToken);
    if (requestedType == typeof(IRelaySwitchModule))
      return GetByIdInternalAsync<TDevice, RelaySwitchModuleDto>(_relaySwitchModuleService, id, cancellationToken);
    if (requestedType == typeof(ISwitchingDevice))
      return GetByIdInternalAsync<TDevice, SwitchingDeviceDto>(_switchingDeviceService, id, cancellationToken);
    if (requestedType == typeof(IUninterruptiblePowerSupply))
      return GetByIdInternalAsync<TDevice, UninterruptiblePowerSupplyDto>(_uninterruptiblePowerSupplyService, id, cancellationToken);

    throw CreateUnsupportedTypeException<TDevice>();
  }

  /// <inheritdoc/>
  public Task<List<TDevice>> GetAllAsync<TDevice>(CancellationToken cancellationToken = default)
    where TDevice : class, IDevice
  {
    var requestedType = typeof(TDevice);

    if (requestedType == typeof(IBreakdownTester))
      return GetAllInternalAsync<TDevice, BreakdownTesterDto>(_breakdownTesterService, cancellationToken);
    if (requestedType == typeof(IChassisManager))
      return GetAllInternalAsync<TDevice, ChassisManagerDto>(_chassisManagerService, cancellationToken);
    if (requestedType == typeof(IFastMeter))
      return GetAllInternalAsync<TDevice, FastMeterDto>(_fastMeterService, cancellationToken);
    if (requestedType == typeof(IPowerSourceModule))
      return GetAllInternalAsync<TDevice, PowerSourceModuleDto>(_powerSourceModuleService, cancellationToken);
    if (requestedType == typeof(IRack))
      return GetAllInternalAsync<TDevice, RackDto>(_rackService, cancellationToken);
    if (requestedType == typeof(IRelaySwitchModule))
      return GetAllInternalAsync<TDevice, RelaySwitchModuleDto>(_relaySwitchModuleService, cancellationToken);
    if (requestedType == typeof(ISwitchingDevice))
      return GetAllInternalAsync<TDevice, SwitchingDeviceDto>(_switchingDeviceService, cancellationToken);
    if (requestedType == typeof(IUninterruptiblePowerSupply))
      return GetAllInternalAsync<TDevice, UninterruptiblePowerSupplyDto>(_uninterruptiblePowerSupplyService, cancellationToken);

    throw CreateUnsupportedTypeException<TDevice>();
  }

  /// <inheritdoc/>
  public Task<TDevice?> GetByNumberAsync<TDevice>(int number, CancellationToken cancellationToken = default)
    where TDevice : class, IDevice
  {
    var requestedType = typeof(TDevice);

    if (requestedType == typeof(IBreakdownTester))
      return GetByNumberInternalAsync<TDevice, BreakdownTesterDto>(_breakdownTesterService, number, cancellationToken);
    if (requestedType == typeof(IChassisManager))
      return GetByNumberInternalAsync<TDevice, ChassisManagerDto>(_chassisManagerService, number, cancellationToken);
    if (requestedType == typeof(IFastMeter))
      return GetByNumberInternalAsync<TDevice, FastMeterDto>(_fastMeterService, number, cancellationToken);
    if (requestedType == typeof(IPowerSourceModule))
      return GetByNumberInternalAsync<TDevice, PowerSourceModuleDto>(_powerSourceModuleService, number, cancellationToken);
    if (requestedType == typeof(IRack))
      return GetByNumberInternalAsync<TDevice, RackDto>(_rackService, number, cancellationToken);
    if (requestedType == typeof(IRelaySwitchModule))
      return GetByNumberInternalAsync<TDevice, RelaySwitchModuleDto>(_relaySwitchModuleService, number, cancellationToken);
    if (requestedType == typeof(ISwitchingDevice))
      return GetByNumberInternalAsync<TDevice, SwitchingDeviceDto>(_switchingDeviceService, number, cancellationToken);
    if (requestedType == typeof(IUninterruptiblePowerSupply))
      return GetByNumberInternalAsync<TDevice, UninterruptiblePowerSupplyDto>(_uninterruptiblePowerSupplyService, number, cancellationToken);

    throw CreateUnsupportedTypeException<TDevice>();
  }

  /// <inheritdoc/>
  public Task<List<TDevice>> GetDevicesByNumberChassisAsync<TDevice>(
    int numberChassis,
    CancellationToken cancellationToken = default)
    where TDevice : class, IDevice
  {
    var requestedType = typeof(TDevice);

    if (requestedType == typeof(IBreakdownTester))
      return GetByChassisInternalAsync<TDevice, BreakdownTesterDto>(_breakdownTesterService, numberChassis, cancellationToken);
    if (requestedType == typeof(IFastMeter))
      return GetByChassisInternalAsync<TDevice, FastMeterDto>(_fastMeterService, numberChassis, cancellationToken);
    if (requestedType == typeof(IPowerSourceModule))
      return GetByChassisInternalAsync<TDevice, PowerSourceModuleDto>(_powerSourceModuleService, numberChassis, cancellationToken);
    if (requestedType == typeof(IRelaySwitchModule))
      return GetByChassisInternalAsync<TDevice, RelaySwitchModuleDto>(_relaySwitchModuleService, numberChassis, cancellationToken);
    if (requestedType == typeof(ISwitchingDevice))
      return GetByChassisInternalAsync<TDevice, SwitchingDeviceDto>(_switchingDeviceService, numberChassis, cancellationToken);
    if (requestedType == typeof(IUninterruptiblePowerSupply))
      return GetByChassisInternalAsync<TDevice, UninterruptiblePowerSupplyDto>(_uninterruptiblePowerSupplyService, numberChassis, cancellationToken);

    throw new NotSupportedException(
      $"Для интерфейса '{requestedType.Name}' выборка по номеру шасси не поддерживается.");
  }

  /// <inheritdoc/>
  public Task<TDevice?> GetDeviceByNumberChassisAsync<TDevice>(
    int numberChassis,
    int number,
    CancellationToken cancellationToken = default)
    where TDevice : class, IDevice
  {
    var requestedType = typeof(TDevice);

    if (requestedType == typeof(IBreakdownTester))
      return GetByChassisAndNumberInternalAsync<TDevice, BreakdownTesterDto>(_breakdownTesterService, numberChassis, number, cancellationToken);
    if (requestedType == typeof(IFastMeter))
      return GetByChassisAndNumberInternalAsync<TDevice, FastMeterDto>(_fastMeterService, numberChassis, number, cancellationToken);
    if (requestedType == typeof(IPowerSourceModule))
      return GetByChassisAndNumberInternalAsync<TDevice, PowerSourceModuleDto>(_powerSourceModuleService, numberChassis, number, cancellationToken);
    if (requestedType == typeof(IRelaySwitchModule))
      return GetByChassisAndNumberInternalAsync<TDevice, RelaySwitchModuleDto>(_relaySwitchModuleService, numberChassis, number, cancellationToken);
    if (requestedType == typeof(ISwitchingDevice))
      return GetByChassisAndNumberInternalAsync<TDevice, SwitchingDeviceDto>(_switchingDeviceService, numberChassis, number, cancellationToken);
    if (requestedType == typeof(IUninterruptiblePowerSupply))
      return GetByChassisAndNumberInternalAsync<TDevice, UninterruptiblePowerSupplyDto>(_uninterruptiblePowerSupplyService, numberChassis, number, cancellationToken);

    throw new NotSupportedException(
      $"Для интерфейса '{requestedType.Name}' выборка по номеру шасси и номеру устройства не поддерживается.");
  }

  /// <inheritdoc/>
  public async Task<TDevice?> ReloadByIdAsync<TDevice>(int id, CancellationToken cancellationToken = default)
    where TDevice : class, IDevice
  {
    _cache.Remove(typeof(TDevice), id);
    return await GetByIdAsync<TDevice>(id, cancellationToken);
  }

  /// <inheritdoc/>
  public Task<TDevice> CreateAsync<TDevice>(TDevice device, CancellationToken cancellationToken = default)
    where TDevice : class, IDevice
  {
    ArgumentNullException.ThrowIfNull(device);

    var requestedType = typeof(TDevice);

    if (requestedType == typeof(IBreakdownTester))
      return CreateInternalAsync(device, x => BreakdownTesterMapper.ToDto((IBreakdownTester)(object)x), _breakdownTesterService, cancellationToken);
    if (requestedType == typeof(IChassisManager))
      return CreateInternalAsync(device, x => ChassisManagerMapper.ToDto((IChassisManager)(object)x), _chassisManagerService, cancellationToken);
    if (requestedType == typeof(IFastMeter))
      return CreateInternalAsync(device, x => FastMeterMapper.ToDto((IFastMeter)(object)x), _fastMeterService, cancellationToken);
    if (requestedType == typeof(IPowerSourceModule))
      return CreateInternalAsync(device, x => PowerSourceModuleMapper.ToDto((IPowerSourceModule)(object)x), _powerSourceModuleService, cancellationToken);
    if (requestedType == typeof(IRack))
      return CreateInternalAsync(device, x => RackMapper.ToDto((IRack)(object)x), _rackService, cancellationToken);
    if (requestedType == typeof(IRelaySwitchModule))
      return CreateInternalAsync(device, x => RelaySwitchModuleMapper.ToDto((IRelaySwitchModule)(object)x), _relaySwitchModuleService, cancellationToken);
    if (requestedType == typeof(ISwitchingDevice))
      return CreateInternalAsync(device, x => SwitchingDeviceMapper.ToDto((ISwitchingDevice)(object)x), _switchingDeviceService, cancellationToken);
    if (requestedType == typeof(IUninterruptiblePowerSupply))
      return CreateInternalAsync(device, x => UninterruptiblePowerSupplyMapper.ToDto((IUninterruptiblePowerSupply)(object)x), _uninterruptiblePowerSupplyService, cancellationToken);

    throw CreateUnsupportedTypeException<TDevice>();
  }

  /// <inheritdoc/>
  public Task<List<TDevice>> CreateRangeAsync<TDevice>(
    IEnumerable<TDevice> devices,
    CancellationToken cancellationToken = default)
    where TDevice : class, IDevice
  {
    ArgumentNullException.ThrowIfNull(devices);

    var requestedType = typeof(TDevice);

    if (requestedType == typeof(IBreakdownTester))
      return CreateRangeInternalAsync(devices, x => BreakdownTesterMapper.ToDto((IBreakdownTester)(object)x), _breakdownTesterService, cancellationToken);
    if (requestedType == typeof(IChassisManager))
      return CreateRangeInternalAsync(devices, x => ChassisManagerMapper.ToDto((IChassisManager)(object)x), _chassisManagerService, cancellationToken);
    if (requestedType == typeof(IFastMeter))
      return CreateRangeInternalAsync(devices, x => FastMeterMapper.ToDto((IFastMeter)(object)x), _fastMeterService, cancellationToken);
    if (requestedType == typeof(IPowerSourceModule))
      return CreateRangeInternalAsync(devices, x => PowerSourceModuleMapper.ToDto((IPowerSourceModule)(object)x), _powerSourceModuleService, cancellationToken);
    if (requestedType == typeof(IRack))
      return CreateRangeInternalAsync(devices, x => RackMapper.ToDto((IRack)(object)x), _rackService, cancellationToken);
    if (requestedType == typeof(IRelaySwitchModule))
      return CreateRangeInternalAsync(devices, x => RelaySwitchModuleMapper.ToDto((IRelaySwitchModule)(object)x), _relaySwitchModuleService, cancellationToken);
    if (requestedType == typeof(ISwitchingDevice))
      return CreateRangeInternalAsync(devices, x => SwitchingDeviceMapper.ToDto((ISwitchingDevice)(object)x), _switchingDeviceService, cancellationToken);
    if (requestedType == typeof(IUninterruptiblePowerSupply))
      return CreateRangeInternalAsync(devices, x => UninterruptiblePowerSupplyMapper.ToDto((IUninterruptiblePowerSupply)(object)x), _uninterruptiblePowerSupplyService, cancellationToken);

    throw CreateUnsupportedTypeException<TDevice>();
  }

  /// <inheritdoc/>
  public Task<TDevice> UpdateAsync<TDevice>(TDevice device, CancellationToken cancellationToken = default)
    where TDevice : class, IDevice
  {
    ArgumentNullException.ThrowIfNull(device);

    var requestedType = typeof(TDevice);

    if (requestedType == typeof(IBreakdownTester))
      return UpdateInternalAsync(device, x => BreakdownTesterMapper.ToDto((IBreakdownTester)(object)x), _breakdownTesterService, cancellationToken);
    if (requestedType == typeof(IChassisManager))
      return UpdateInternalAsync(device, x => ChassisManagerMapper.ToDto((IChassisManager)(object)x), _chassisManagerService, cancellationToken);
    if (requestedType == typeof(IFastMeter))
      return UpdateInternalAsync(device, x => FastMeterMapper.ToDto((IFastMeter)(object)x), _fastMeterService, cancellationToken);
    if (requestedType == typeof(IPowerSourceModule))
      return UpdateInternalAsync(device, x => PowerSourceModuleMapper.ToDto((IPowerSourceModule)(object)x), _powerSourceModuleService, cancellationToken);
    if (requestedType == typeof(IRack))
      return UpdateInternalAsync(device, x => RackMapper.ToDto((IRack)(object)x), _rackService, cancellationToken);
    if (requestedType == typeof(IRelaySwitchModule))
      return UpdateInternalAsync(device, x => RelaySwitchModuleMapper.ToDto((IRelaySwitchModule)(object)x), _relaySwitchModuleService, cancellationToken);
    if (requestedType == typeof(ISwitchingDevice))
      return UpdateInternalAsync(device, x => SwitchingDeviceMapper.ToDto((ISwitchingDevice)(object)x), _switchingDeviceService, cancellationToken);
    if (requestedType == typeof(IUninterruptiblePowerSupply))
      return UpdateInternalAsync(device, x => UninterruptiblePowerSupplyMapper.ToDto((IUninterruptiblePowerSupply)(object)x), _uninterruptiblePowerSupplyService, cancellationToken);

    throw CreateUnsupportedTypeException<TDevice>();
  }

  /// <inheritdoc/>
  public Task<bool> DeleteAsync<TDevice>(TDevice device, CancellationToken cancellationToken = default)
    where TDevice : class, IDevice
  {
    ArgumentNullException.ThrowIfNull(device);
    return DeleteByIdAsync<TDevice>(device.Id, cancellationToken);
  }

  /// <inheritdoc/>
  public Task<bool> DeleteByIdAsync<TDevice>(int id, CancellationToken cancellationToken = default)
    where TDevice : class, IDevice
  {
    var requestedType = typeof(TDevice);

    if (requestedType == typeof(IBreakdownTester))
      return DeleteByIdInternalAsync<TDevice, BreakdownTesterDto>(id, _breakdownTesterService, cancellationToken);
    if (requestedType == typeof(IChassisManager))
      return DeleteByIdInternalAsync<TDevice, ChassisManagerDto>(id, _chassisManagerService, cancellationToken);
    if (requestedType == typeof(IFastMeter))
      return DeleteByIdInternalAsync<TDevice, FastMeterDto>(id, _fastMeterService, cancellationToken);
    if (requestedType == typeof(IPowerSourceModule))
      return DeleteByIdInternalAsync<TDevice, PowerSourceModuleDto>(id, _powerSourceModuleService, cancellationToken);
    if (requestedType == typeof(IRack))
      return DeleteByIdInternalAsync<TDevice, RackDto>(id, _rackService, cancellationToken);
    if (requestedType == typeof(IRelaySwitchModule))
      return DeleteByIdInternalAsync<TDevice, RelaySwitchModuleDto>(id, _relaySwitchModuleService, cancellationToken);
    if (requestedType == typeof(ISwitchingDevice))
      return DeleteByIdInternalAsync<TDevice, SwitchingDeviceDto>(id, _switchingDeviceService, cancellationToken);
    if (requestedType == typeof(IUninterruptiblePowerSupply))
      return DeleteByIdInternalAsync<TDevice, UninterruptiblePowerSupplyDto>(id, _uninterruptiblePowerSupplyService, cancellationToken);

    throw CreateUnsupportedTypeException<TDevice>();
  }

  /// <inheritdoc/>
  Task<bool> IDeviceEngine.DeleteAllAsync<TDevice>(CancellationToken cancellationToken)
  {
    var requestedType = typeof(TDevice);

    if (requestedType == typeof(IBreakdownTester))
      return DeleteAllInternalAsync<TDevice, BreakdownTesterDto>(_breakdownTesterService, cancellationToken);
    if (requestedType == typeof(IChassisManager))
      return DeleteAllInternalAsync<TDevice, ChassisManagerDto>(_chassisManagerService, cancellationToken);
    if (requestedType == typeof(IFastMeter))
      return DeleteAllInternalAsync<TDevice, FastMeterDto>(_fastMeterService, cancellationToken);
    if (requestedType == typeof(IPowerSourceModule))
      return DeleteAllInternalAsync<TDevice, PowerSourceModuleDto>(_powerSourceModuleService, cancellationToken);
    if (requestedType == typeof(IRack))
      return DeleteAllInternalAsync<TDevice, RackDto>(_rackService, cancellationToken);
    if (requestedType == typeof(IRelaySwitchModule))
      return DeleteAllInternalAsync<TDevice, RelaySwitchModuleDto>(_relaySwitchModuleService, cancellationToken);
    if (requestedType == typeof(ISwitchingDevice))
      return DeleteAllInternalAsync<TDevice, SwitchingDeviceDto>(_switchingDeviceService, cancellationToken);
    if (requestedType == typeof(IUninterruptiblePowerSupply))
      return DeleteAllInternalAsync<TDevice, UninterruptiblePowerSupplyDto>(_uninterruptiblePowerSupplyService, cancellationToken);

    throw CreateUnsupportedTypeException<TDevice>();
  }

  /// <inheritdoc/>
  public TDevice Build<TDevice>(DeviceDto dto)
    where TDevice : class, IDevice
  {
    ArgumentNullException.ThrowIfNull(dto);

    if (_cache.TryGet(typeof(TDevice), dto.Id, out var cached))
    {
      if (cached is TDevice typedCached)
      {
        if (!string.Equals(cached.GetType().FullName, dto.DeviceClass, StringComparison.Ordinal))
        {
          _cache.Remove(typeof(TDevice), dto.Id);
          var rebuiltDevice = DeviceBuilder.Build<TDevice>(dto);
          _cache.Set(typeof(TDevice), dto.Id, rebuiltDevice);
          return rebuiltDevice;
        }

        DeviceMapperRegistry.Apply(typedCached, dto);
        return typedCached;
      }

      throw new InvalidOperationException(
        $"Устройство с Id={dto.Id} уже находится в кэше с другим интерфейсом.");
    }

    var device = DeviceBuilder.Build<TDevice>(dto);
    _cache.Set(typeof(TDevice), dto.Id, device);
    return device;
  }

  /// <inheritdoc/>
  public void ClearCache()
  {
    _cache.Clear();
    ClearQueryCaches();
  }

  private async Task<TDevice?> GetByIdInternalAsync<TDevice, TDto>(
    CrudService<TDto> service,
    int id,
    CancellationToken cancellationToken)
    where TDevice : class, IDevice
    where TDto : DeviceDto
  {
    if (_cache.TryGet(typeof(TDevice), id, out var cached))
    {
      if (cached is TDevice typedCached)
      {
        return typedCached;
      }

      throw new InvalidOperationException(
        $"Устройство с Id={id} уже находится в кэше с другим интерфейсом.");
    }

    var dto = await service.GetByIdAsync(id, cancellationToken);
    return dto == null ? null : Build<TDevice>(dto);
  }

  private async Task<List<TDevice>> GetAllInternalAsync<TDevice, TDto>(
    CrudService<TDto> service,
    CancellationToken cancellationToken)
    where TDevice : class, IDevice
    where TDto : DeviceDto
  {
    if (TryGetCachedDevices(_allQueryCache, typeof(TDevice), out List<TDevice>? cachedDevices))
    {
      return cachedDevices;
    }

    var dtoList = await service.GetAllAsync(cancellationToken);
    var devices = dtoList.Select(Build<TDevice>).ToList();
    _allQueryCache[typeof(TDevice)] = dtoList.Select(dto => dto.Id).ToArray();
    return devices;
  }

  private async Task<TDevice?> GetByNumberInternalAsync<TDevice, TDto>(
    CrudService<TDto> service,
    int number,
    CancellationToken cancellationToken)
    where TDevice : class, IDevice
    where TDto : DeviceDto
  {
    var dtoList = await service.FindByPropertyAsync(x => x.Number, number, cancellationToken);

    if (dtoList.Count > 1)
    {
      throw new InvalidOperationException(
        $"Для типа '{typeof(TDevice).Name}' найдено несколько устройств с Number={number}. Используй более точную выборку.");
    }

    var dto = dtoList.FirstOrDefault();
    return dto == null ? null : Build<TDevice>(dto);
  }

  private async Task<List<TDevice>> GetByChassisInternalAsync<TDevice, TDto>(
    CrudService<TDto> service,
    int numberChassis,
    CancellationToken cancellationToken)
    where TDevice : class, IDevice
    where TDto : AttachableDeviceDto
  {
    var cacheKey = new ChassisQueryCacheKey(typeof(TDevice), numberChassis);
    if (TryGetCachedDevices(_chassisQueryCache, cacheKey, out List<TDevice>? cachedDevices))
    {
      return cachedDevices;
    }

    var dtoList = await service.FindByPropertyAsync(x => x.NumberChassis, numberChassis, cancellationToken);
    var devices = dtoList.Select(Build<TDevice>).ToList();
    _chassisQueryCache[cacheKey] = dtoList.Select(dto => dto.Id).ToArray();
    return devices;
  }

  private async Task<TDevice?> GetByChassisAndNumberInternalAsync<TDevice, TDto>(
    CrudService<TDto> service,
    int numberChassis,
    int number,
    CancellationToken cancellationToken)
    where TDevice : class, IDevice
    where TDto : AttachableDeviceDto
  {
    var dtoList = await service.FindByPropertyAsync(x => x.NumberChassis, numberChassis, cancellationToken);
    var matched = dtoList.Where(x => x.Number == number).ToList();

    if (matched.Count > 1)
    {
      throw new InvalidOperationException(
        $"Для типа '{typeof(TDevice).Name}' найдено несколько устройств с NumberChassis={numberChassis} и Number={number}.");
    }

    var dto = matched.FirstOrDefault();
    return dto == null ? null : Build<TDevice>(dto);
  }

  private async Task<TDevice> CreateInternalAsync<TDevice, TDto>(
    TDevice device,
    Func<TDevice, TDto> mapper,
    CrudService<TDto> service,
    CancellationToken cancellationToken)
    where TDevice : class, IDevice
    where TDto : DeviceDto
  {
    var dto = mapper(device);
    dto.Id = 0;

    var created = await service.CreateAsync(dto, cancellationToken);
    _cache.Remove(typeof(TDevice), created.Id);
    InvalidateQueryCaches(typeof(TDevice));
    return Build<TDevice>(created);
  }

  private async Task<List<TDevice>> CreateRangeInternalAsync<TDevice, TDto>(
    IEnumerable<TDevice> devices,
    Func<TDevice, TDto> mapper,
    CrudService<TDto> service,
    CancellationToken cancellationToken)
    where TDevice : class, IDevice
    where TDto : DeviceDto
  {
    var result = new List<TDevice>();

    foreach (var device in devices)
    {
      ArgumentNullException.ThrowIfNull(device);

      var dto = mapper(device);
      dto.Id = 0;

      var created = await service.CreateAsync(dto, cancellationToken);

      _cache.Remove(typeof(TDevice), created.Id);
      InvalidateQueryCaches(typeof(TDevice));

      var built = Build<TDevice>(created);
      result.Add(built);
    }

    return result;
  }

  private async Task<TDevice> UpdateInternalAsync<TDevice, TDto>(
    TDevice device,
    Func<TDevice, TDto> mapper,
    CrudService<TDto> service,
    CancellationToken cancellationToken)
    where TDevice : class, IDevice
    where TDto : DeviceDto
  {
    var dto = mapper(device);

    var updated = await service.UpdateAsync(dto, cancellationToken);
    _cache.Remove(typeof(TDevice), updated.Id);
    InvalidateQueryCaches(typeof(TDevice));
    return Build<TDevice>(updated);
  }

  private async Task<bool> DeleteByIdInternalAsync<TDevice, TDto>(
    int id,
    CrudService<TDto> service,
    CancellationToken cancellationToken)
    where TDevice : class, IDevice
    where TDto : DeviceDto
  {
    if (id <= 0)
    {
      return false;
    }

    var deleted = await service.DeleteByIdAsync(id, cancellationToken);
    if (deleted)
    {
      _cache.Remove(typeof(TDevice), id);
      InvalidateQueryCaches(typeof(TDevice));
    }

    return deleted;
  }

  private async Task<bool> DeleteAllInternalAsync<TDevice, TDto>(
    CrudService<TDto> service,
    CancellationToken cancellationToken)
    where TDevice : class, IDevice
    where TDto : DeviceDto
  {
    var all = await service.GetAllAsync(cancellationToken);

    if (all.Count == 0)
      return false;

    foreach (var dto in all)
    {
      await service.DeleteByIdAsync(dto.Id, cancellationToken);
      _cache.Remove(typeof(TDevice), dto.Id);
    }

    InvalidateQueryCaches(typeof(TDevice));
    return true;
  }

  private void ClearQueryCaches()
  {
    _allQueryCache.Clear();
    _chassisQueryCache.Clear();
  }

  private void InvalidateQueryCaches(Type deviceType)
  {
    _allQueryCache.TryRemove(deviceType, out _);

    foreach (var entry in _chassisQueryCache.Keys)
    {
      if (entry.DeviceType == deviceType)
      {
        _chassisQueryCache.TryRemove(entry, out _);
      }
    }
  }

  private bool TryGetCachedDevices<TCacheKey, TDevice>(
    ConcurrentDictionary<TCacheKey, int[]> queryCache,
    TCacheKey cacheKey,
    out List<TDevice>? devices)
    where TCacheKey : notnull
    where TDevice : class, IDevice
  {
    devices = null;

    if (!queryCache.TryGetValue(cacheKey, out int[]? ids))
    {
      return false;
    }

    var result = new List<TDevice>(ids.Length);
    foreach (int id in ids)
    {
      if (!_cache.TryGet(typeof(TDevice), id, out var cached) || cached is not TDevice typedCached)
      {
        queryCache.TryRemove(cacheKey, out _);
        return false;
      }

      result.Add(typedCached);
    }

    devices = result;
    return true;
  }

  private static NotSupportedException CreateUnsupportedTypeException<TDevice>()
    where TDevice : class, IDevice
  {
    return new NotSupportedException(
      $"Интерфейс устройства '{typeof(TDevice).Name}' не зарегистрирован в DeviceEngine.");
  }
}
