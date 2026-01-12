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
    { typeof(IChassisManager), typeof(ChassisManagerEntity) },
    { typeof(IRack), typeof(RackEntity) },
  };

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
    }

    /// <summary>
    /// Возвращает все устройства типа <typeparamref name="T"/>.
    /// </summary>
    /// <returns>Список экземпляров.</returns>
    public List<T> GetAll()
    {
      var dbSet = GetDbSet();
      var devices = dbSet
        .Cast<T>()
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
      var dbSet = GetDbSet();
      return dbSet.Cast<object>().ToList();
    }

    /// <summary>
    /// Возвращает сущность из базы данных по ID.
    /// </summary>
    /// <param name="id">Уникальный идентификатор сущности.</param>
    /// <returns>Объект сущности или <c>null</c>, если не найден.</returns>
    public object GetEntityById(int id)
    {
      var dbSet = GetDbSet();

      var method = dbSet.GetType().GetMethod("Find", new[] { typeof(object[]) });
      if (method == null)
      {
        throw new InvalidOperationException("Метод Find не найден.");
      }

      return method.Invoke(dbSet, new object[] { new object[] { id } });
    }

    /// <summary>
    /// Возвращает устройство по его ID.
    /// </summary>
    /// <param name="id">Уникальный идентификатор.</param>
    /// <returns>Экземпляр устройства, либо <c>null</c>.</returns>
    public T GetById(int id)
    {
      var dbSet = GetDbSet();
      var method = dbSet.GetType().GetMethod("Find", new[] { typeof(object[]) });

      if (method == null)
      {
        throw new InvalidOperationException("Метод Find не найден.");
      }

      var entity = method.Invoke(dbSet, new object[] { new object[] { id } });

      if (entity is not T device)
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

      // Копируем свойства, кроме runtime (COMPort и т.п.)
      CopyProperties(device, instance);

      return instance;
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
