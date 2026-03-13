using Ask.Core.Services.App;
using Ask.Core.Shared.Entity.Devices;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Rack;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.UninterruptiblePowerSupply;
using DataBaseConfiguration.Context;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using static Ask.LogLib.LoggerUtility;


namespace DataBaseConfiguration.Services.Device
{

  /// <summary>
  /// Универсальный сервисный класс, реализующий базовые операции CRUD (Create, Read, Update, Delete)
  /// для работы с объектами типа <typeparamref name="T"/>.
  /// </summary>
  /// <typeparam name="T">Интерфейс сущности устройства, с которой работает сервис.</typeparam>
  public class Service<T> where T : class, IDevice
  {
    /// <summary>
    /// Словарь, отображающий интерфейс устройства на соответствующую сущность базы данных.
    /// </summary>
    private static readonly Dictionary<Type, Type> EntityTypeMapping = new()
  {
    { typeof(IBreakdownTester), typeof(BreakdownTesterEntity) },
    { typeof(IFastMeter), typeof(FastMeterEntity) },
    { typeof(IPowerSourceModule), typeof(PowerSourceModuleEntity) },
    { typeof(IRelaySwitchModule), typeof(RelaySwitchModuleEntity) },
    { typeof(ISwitchingDevice), typeof(SwitchingDeviceEntity) },
    { typeof(IUninterruptiblePowerSupply), typeof(UninterruptiblePowerSupplyEntity) },
    { typeof(IChassisManager), typeof(ChassisManagerEntity) },
    { typeof(IRack), typeof(RackEntity) },
  };

    private static readonly object CacheSync = new();
    private static List<T> EntityCache = new();
    private static bool IsCacheInitialized;

    /// <summary>
    /// Контекст базы данных.
    /// </summary>
    internal readonly AppDbContext _context;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="Service{T}"/>.
    /// </summary>
    /// <param name="context">Контекст базы данных.</param>
    internal Service(AppDbContext context)
    {
      _context = context;
      EnsureCacheInitialized();
    }

    /// <summary>
    /// Добавляет новую сущность в базу данных.
    /// </summary>
    /// <param name="entity">Экземпляр сущности.</param>
    public virtual void Create(T entity)
    {
      try
      {
        LogInformation($"[{nameof(Service<T>)}] Создание сущности {typeof(T).Name}");

        _context.Add(entity);
        _context.SaveChanges();
        SyncCacheOnCreate(entity);

        LogInformation($"[{nameof(Service<T>)}] Сущность {typeof(T).Name} успешно добавлена.");
      }
      catch (Exception ex)
      {
        LogException(ex, $"[{nameof(Service<T>)}] Ошибка при создании сущности {typeof(T).Name}");
        throw new Exception($"Ошибка при добавлении устройства типа {typeof(T).Name}: {ex.Message}", ex);
      }
    }

    /// <summary>
    /// Удаляет сущность из базы данных.
    /// </summary>
    /// <param name="entity">Экземпляр сущности.</param>
    public void Delete(T entity)
    {
      if (entity != null)
      {
        _context.Remove(entity);
        _context.SaveChanges();
        SyncCacheOnDelete(entity);
      }
    }

    /// <summary>
    /// Обновляет сущность в базе данных.
    /// </summary>
    /// <param name="entity">Экземпляр сущности.</param>
    public void Update(T entity)
    {
      _context.Attach(entity);
      _context.Entry(entity).State = EntityState.Modified;
      _context.SaveChanges();
      SyncCacheOnUpdate(entity);
    }

    /// <summary>
    /// Возвращает все устройства типа <typeparamref name="T"/>.
    /// </summary>
    /// <returns>Список экземпляров.</returns>
    public List<T> GetAll()
    {
      var devices = GetCachedEntities()
        .Select(GetDeviceInstance)
        .Where(x => x != null)
        .Cast<T>()
        .ToList();

      return devices;
    }

    /// <summary>
    /// Возвращает список сущностей базы данных, соответствующих типу <typeparamref name="T"/>.
    /// </summary>
    /// <returns>Список необработанных сущностей.</returns>
    internal virtual List<object> GetAllData()
    {
      return GetCachedEntities()
        .Cast<object>()
        .ToList();
    }

    /// <summary>
    /// Возвращает сущность из базы данных по ID.
    /// </summary>
    /// <param name="id">Уникальный идентификатор сущности.</param>
    /// <returns>Объект сущности или <c>null</c>, если не найден.</returns>
    public object GetEntityById(int id)
    {
      return GetCachedEntities().FirstOrDefault(x => x.Id == id);
    }

    /// <summary>
    /// Возвращает устройство по его ID.
    /// </summary>
    /// <param name="id">Уникальный идентификатор.</param>
    /// <returns>Экземпляр устройства, либо <c>null</c>.</returns>
    public T GetById(int id)
    {
      var device = GetCachedEntities().FirstOrDefault(x => x.Id == id);
      if (device is null)
      {
        return null;
      }

      return GetDeviceInstance(device);
    }

    /// <summary>
    /// Возвращает объект <see cref="IQueryable"/> для соответствующего типа сущности.
    /// </summary>
    /// <returns><see cref="IQueryable"/> набор объектов.</returns>
    /// <exception cref="InvalidOperationException">Если тип <typeparamref name="T"/> не зарегистрирован в словаре.</exception>
    private IQueryable GetDbSet()
    {
      if (!EntityTypeMapping.TryGetValue(typeof(T), out var entityType))
      {
        throw new InvalidOperationException($"Тип {typeof(T).Name} не зарегистрирован.");
      }

      var setMethod = typeof(DbContext).GetMethods()
        .First(m => m.Name == "Set" && m.IsGenericMethod && m.GetParameters().Length == 0);

      var genericSet = setMethod.MakeGenericMethod(entityType);
      return (IQueryable)genericSet.Invoke(_context, null);
    }

    /// <summary>
    /// Принудительно перезагружает кэш сущностей из базы данных.
    /// </summary>
    public void ReloadCache()
    {
      lock (CacheSync)
      {
        LoadCacheFromDb();
      }
    }

    private List<T> GetCachedEntities()
    {
      EnsureCacheInitialized();
      lock (CacheSync)
      {
        return EntityCache.ToList();
      }
    }

    private void EnsureCacheInitialized()
    {
      if (IsCacheInitialized)
        return;

      lock (CacheSync)
      {
        if (IsCacheInitialized)
          return;

        LoadCacheFromDb();
      }
    }

    private void LoadCacheFromDb()
    {
      var dbSet = GetDbSet();
      EntityCache = dbSet.Cast<T>().ToList();
      IsCacheInitialized = true;
    }

    private static void SyncCacheOnCreate(T entity)
    {
      lock (CacheSync)
      {
        if (!IsCacheInitialized)
          return;

        var index = EntityCache.FindIndex(x => x.Id == entity.Id);
        if (index >= 0)
        {
          EntityCache[index] = entity;
        }
        else
        {
          EntityCache.Add(entity);
        }
      }
    }

    private static void SyncCacheOnUpdate(T entity)
    {
      lock (CacheSync)
      {
        if (!IsCacheInitialized)
          return;

        var index = EntityCache.FindIndex(x => x.Id == entity.Id);
        if (index >= 0)
        {
          EntityCache[index] = entity;
        }
      }
    }

    private static void SyncCacheOnDelete(T entity)
    {
      lock (CacheSync)
      {
        if (!IsCacheInitialized)
          return;

        EntityCache.RemoveAll(x => x.Id == entity.Id);
      }
    }

    /// <summary>
    /// Создаёт или возвращает экземпляр устройства на основе интерфейса.
    /// Сначала пытается достать из DI, если не найдено — создаёт новый.
    /// </summary>
    /// <param name="device">Объект с данными устройства.</param>
    /// <returns>Экземпляр устройства.</returns>
    internal T GetDeviceInstance(T device)
    {
      if (device == null)
        return null;

      T instance = default;

      try
      {
        instance = ServiceLocator.TryGet<T>();
      }
      catch
      {
        // Игнорируем ошибки — значит сервис не зарегистрирован
      }

      if (instance == null)
      {
        // Создаём новый объект по DeviceClass
        object created = CreateDeviceInstance(device.DeviceClass);
        if (created is not T casted)
        {
          Console.WriteLine($"Ошибка: Не удалось создать объект {device.DeviceClass}.");
          return null;
        }
        instance = casted;
      }

      // Применяем конфигурацию, по умолчанию через копирование свойств.
      ApplyConfiguration(device, instance);

      return instance;
    }

    /// <summary>
    /// Применяет конфигурацию из объекта-источника к runtime-экземпляру устройства.
    /// По умолчанию используется копирование свойств через рефлексию.
    /// </summary>
    /// <param name="source">Источник конфигурации (обычно сущность БД).</param>
    /// <param name="target">Готовый экземпляр устройства.</param>
    protected virtual void ApplyConfiguration(T source, T target)
    {
      CopyProperties(source, target);
    }

    /// <summary>
    /// Создаёт объект по имени класса.
    /// </summary>
    /// <param name="className">Полное имя класса.</param>
    /// <returns>Экземпляр объекта, либо <c>null</c>.</returns>
    private static object CreateDeviceInstance(string className)
    {
      LogInformation($"Создание объекта класса: {className}");

      Type type = Type.GetType(className)
                  ?? AppDomain.CurrentDomain.GetAssemblies()
                       .Select(a => a.GetType(className))
                       .FirstOrDefault(t => t != null);

      if (type == null)
      {
        LogError($"Ошибка: Класс {className} не найден.");
        return null;
      }

      return Activator.CreateInstance(type);
    }
    private static void CopyProperties(object source, object target)
    {
      if (source == null || target == null)
        return;

      Type sourceType = source.GetType();
      Type targetType = target.GetType();

      foreach (PropertyInfo sourceProp in sourceType.GetProperties())
      {
        PropertyInfo targetProp = targetType.GetProperty(sourceProp.Name);
        if (targetProp != null && targetProp.CanWrite)
        {
          // Не трогаем runtime-свойства
          if (targetProp.Name is "COMPort" or "DeviceProtocol" or "ConnectableManager")
            continue;

          object value = sourceProp.GetValue(source);
          if (value != null)
          {
            targetProp.SetValue(target, value);
          }
        }
      }
    }
  }
}
