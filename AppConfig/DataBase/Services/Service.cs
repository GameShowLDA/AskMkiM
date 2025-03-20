using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AppConfig.DataBase.Models;
using AppConfig.DataBase.Repositories;
using AppConfig.Interface;
using NewCore.Base.Device;
using NewCore.Base.Interface.Main;
using static Utilities.LoggerUtility;

namespace AppConfig.DataBase.Services
{
  /// <summary>
  /// Универсальный сервисный класс, реализующий базовые операции CRUD (Create, Read, Update, Delete)
  /// для работы с объектами типа <typeparamref name="T"/>.
  /// </summary>
  /// <typeparam name="T">Тип сущности, с которой работает сервис.</typeparam>
  public class Service<T> where T : class, IDevice
  {
    /// <summary>
    /// Контекст базы данных для работы с сущностями.
    /// </summary>
    internal readonly AppDbContext _context;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="Service"/>.
    /// </summary>
    /// <param name="context">Контекст базы данных для работы с сущностями.</param>
    internal Service(AppDbContext context)
    {
      _context = context;
    }

    /// <inheritdoc />
    public void Create(T entity)
    {
      var repository = GetRepositoryForEntity(entity);
      repository?.Create(entity);
    }

    /// <inheritdoc />
    public void Delete(T entity)
    {
      var repository = GetRepositoryForEntity(entity);
      repository?.Delete(entity.Id);
    }

    /// <inheritdoc />
    public List<T> GetAll()
    {
      var data = GetEntitiesForType();
      var result = data
         .OfType<IDevice>()
         .Select(GetDeviceInstance)
         .Where(instance => instance != null)
         .ToList();

      return result;
    }

    /// <inheritdoc />
    public T GetById(int id)
    {
      var entity = new Repository<T>(_context).GetById(id);
      if (entity is not IDevice device)
      {
        LogInformation($"Ошибка: Объект с ID {id} не является устройством.");
        return null;
      }

      return GetDeviceInstance(device);
    }

    /// <inheritdoc />
    public void Update(T entity)
    {
      var repository = GetRepositoryForEntity(entity);
      repository?.Update(entity);
    }

    /// <summary>
    /// Возвращает найденное устройство.
    /// </summary>
    /// <param name="device">Модель устройства.</param>
    /// <returns>Объект устройства.</returns>
    internal T GetDeviceInstance(IDevice device)
    {
      object instance = CreateDeviceInstance(device.DeviceClass);
      if (instance == null || !(instance is T))
      {
        Console.WriteLine($"Ошибка: Не удалось создать объект {device.DeviceClass}.");
        return null;
      }

      CopyProperties(device, instance);

      return instance as T;
    }

    /// <summary>
    /// Создаёт объект по имени класса.
    /// </summary>
    /// <param name="className">Имя класса.</param>
    /// <returns>Объект класса.</returns>
    private static object CreateDeviceInstance(string className)
    {
      LogInformation($"Создание объекта класса: {className}");

      Type type = Type.GetType(className);
      if (type == null)
      {
        type = AppDomain.CurrentDomain
                        .GetAssemblies()
                        .Select(a => a.GetType(className))
                        .FirstOrDefault(t => t != null);
      }

      if (type == null)
      {
        LogError($"Ошибка: Класс {className} не найден.");
        return null;
      }

      return Activator.CreateInstance(type);
    }

    /// <summary>
    /// Копирует значения свойств из одного объекта в другой, если свойства совпадают по имени и доступны для записи.
    /// </summary>
    /// <param name="source">Исходный объект, из которого копируются значения.</param>
    /// <param name="target">Целевой объект, в который копируются значения.</param>
    private static void CopyProperties(object source, object target)
    {
      if (source == null || target == null)
      {
        return;
      }

      Type sourceType = source.GetType();
      Type targetType = target.GetType();

      foreach (PropertyInfo sourceProp in sourceType.GetProperties())
      {
        PropertyInfo targetProp = targetType.GetProperty(sourceProp.Name);
        if (targetProp != null && targetProp.CanWrite)
        {
          object value = sourceProp.GetValue(source);
          if (value != null)
          {
            targetProp.SetValue(target, value);
          }
        }
      }
    }

    /// <summary>
    /// Определяет, какой репозиторий соответствует типу <typeparamref name="T"/> 
    /// и получает список объектов этого типа из базы данных.
    /// </summary>
    /// <returns>Список устройств типа <typeparamref name="T"/> в виде <see cref="IEnumerable{IDevice}"/>.</returns>
    private IEnumerable<IDevice> GetEntitiesForType()
    {
      Type type = typeof(T);

      if (typeof(IRelaySwitchModule).IsAssignableFrom(type))
      {
        return new Repository<RelaySwitchModuleEntity>(_context).GetAll();
      }
      else if (typeof(ISwitchingDevice).IsAssignableFrom(type))
      {
        return new Repository<SwitchingDeviceEntity>(_context).GetAll();
      }
      else if (typeof(IPowerSourceModule).IsAssignableFrom(type))
      {
        return new Repository<PowerSourceModuleEntity>(_context).GetAll();
      }
      else if (typeof(IPrecisionMeter).IsAssignableFrom(type))
      {
        return new Repository<PrecisionMeterEntity>(_context).GetAll();
      }
      else if (typeof(IRack).IsAssignableFrom(type))
      {
        return new Repository<RackEntity>(_context).GetAll();
      }
      else if (typeof(IFastMeter).IsAssignableFrom(type))
      {
        return new Repository<FastMeterEntity>(_context).GetAll();
      }

      Console.WriteLine($"Неизвестный тип: {type}");
      return new List<IDevice>(); // Пустой список, если тип не поддерживается
    }

    /// <summary>
    /// Возвращает репозиторий, соответствующий переданному объекту устройства.
    /// </summary>
    /// <typeparam name="T">Тип объекта, реализующего интерфейс <see cref="IDevice"/>.</typeparam>
    /// <param name="entity">Экземпляр объекта устройства.</param>
    /// <returns>Объект репозитория, работающий с соответствующим типом устройства, или <c>null</c>, если репозиторий не найден.</returns>
    private dynamic GetRepositoryForEntity<T>(T entity) where T : class, IDevice
    {
      return entity switch
      {
        IRelaySwitchModule => new RelaySwitchModuleRepository(_context),
        IBreakdownTester => new BreakdownTesterRepository(_context),
        IChassisManager => new ChassisManagerRepository(_context),
        IFastMeter => new FastMeterRepository(_context),
        IPowerSourceModule => new PowerSourceModuleRepository(_context),
        IPrecisionMeter => new PrecisionMeterRepository(_context),
        IRack => new RackRepository(_context),
        ISwitchingDevice => new SwitchingDeviceRepository(_context),
        _ => null // Если не найден репозиторий, возвращаем null
      };
    }
  }
}
