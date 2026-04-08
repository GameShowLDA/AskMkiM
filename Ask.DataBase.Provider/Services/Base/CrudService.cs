using Ask.Core.Services.Errors.DataBase;
using Ask.Core.Shared.DTO.Devices.Base;
using Ask.DataBase.Provider.Context;
using Ask.DataBase.Provider.Contracts;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using static Ask.LogLib.LoggerUtility;

namespace Ask.DataBase.Provider.Services.Base;

/// <summary>
/// Базовый универсальный CRUD-сервис для работы с DTO.
/// Вся общая логика чтения, создания, обновления, удаления
/// и вспомогательных выборок находится здесь.
/// </summary>
/// <typeparam name="T">Тип DTO.</typeparam>
public class CrudService<T> : ICrudService<T> where T : class
{
  /// <inheritdoc/>
  public virtual async Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default)
  {
    await using var context = CreateContext();

    LogInformation($"[{nameof(CrudService<T>)}] Получение всех записей типа {typeof(T).Name}");
    return await context.Set<T>().ToListAsync(cancellationToken);
  }

  /// <inheritdoc/>
  public virtual async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
  {
    await using var context = CreateContext();

    LogInformation($"[{nameof(CrudService<T>)}] Получение записи {typeof(T).Name} по Id={id}");
    return await context.Set<T>().FindAsync([id], cancellationToken);
  }

  /// <inheritdoc/>
  public virtual async Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(entity);

    await using var context = CreateContext();
    await ValidateUniqueDeviceNumberAsync(context, entity, cancellationToken);

    LogInformation($"[{nameof(CrudService<T>)}] Создание записи типа {typeof(T).Name}");
    context.Set<T>().Add(entity);
    await context.SaveChangesAsync(cancellationToken);

    return entity;
  }

  /// <inheritdoc/>
  public virtual async Task<List<T>> CreateRangeAsync(
    IEnumerable<T> entities,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(entities);

    await using var context = CreateContext();

    var list = entities.ToList();
    if (list.Count == 0)
    {
      return list;
    }

    LogInformation($"[{nameof(CrudService<T>)}] Создание набора записей типа {typeof(T).Name}. Количество: {list.Count}");

    context.Set<T>().AddRange(list);
    await context.SaveChangesAsync(cancellationToken);

    return list;
  }

  /// <inheritdoc/>
  public virtual async Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(entity);

    await using var context = CreateContext();
    await ValidateUniqueDeviceNumberAsync(context, entity, cancellationToken);

    LogInformation($"[{nameof(CrudService<T>)}] Обновление записи типа {typeof(T).Name}");
    context.Set<T>().Update(entity);
    await context.SaveChangesAsync(cancellationToken);

    return entity;
  }

  /// <inheritdoc/>
  public virtual async Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(entity);

    await using var context = CreateContext();

    LogInformation($"[{nameof(CrudService<T>)}] Удаление записи типа {typeof(T).Name}");
    context.Set<T>().Remove(entity);
    await context.SaveChangesAsync(cancellationToken);
  }

  /// <inheritdoc/>
  public virtual async Task<bool> DeleteByIdAsync(int id, CancellationToken cancellationToken = default)
  {
    await using var context = CreateContext();

    LogInformation($"[{nameof(CrudService<T>)}] Удаление записи {typeof(T).Name} по Id={id}");

    var entity = await context.Set<T>().FindAsync([id], cancellationToken);
    if (entity == null)
    {
      return false;
    }

    context.Set<T>().Remove(entity);
    await context.SaveChangesAsync(cancellationToken);

    return true;
  }

  /// <inheritdoc/>
  public async Task<bool> DeleteAllAsync(CancellationToken cancellationToken = default)
  {
    await using var context = CreateContext();

    LogInformation($"[{nameof(CrudService<T>)}] Удаление всех записей типа {typeof(T).Name}");

    var set = context.Set<T>();

    var any = await set.AnyAsync(cancellationToken);
    if (!any)
    {
      return false;
    }

    set.RemoveRange(set);
    await context.SaveChangesAsync(cancellationToken);

    return true;
  }

  /// <inheritdoc/>
  public virtual async Task<List<T>> FindByPropertyAsync<TProperty>(
    Expression<Func<T, TProperty>> propertyExpression,
    TProperty value,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(propertyExpression);

    await using var context = CreateContext();

    var body = UnwrapExpression(propertyExpression.Body);
    var valueExpression = Expression.Constant(value, body.Type);
    var predicateBody = Expression.Equal(body, valueExpression);
    var predicate = Expression.Lambda<Func<T, bool>>(predicateBody, propertyExpression.Parameters);

    var propertyName = TryGetPropertyName(body);
    LogInformation($"[{nameof(CrudService<T>)}] Поиск записей {typeof(T).Name} по свойству {propertyName}");

    return await context.Set<T>()
      .Where(predicate)
      .ToListAsync(cancellationToken);
  }

  /// <summary>
  /// Возвращает первую запись, удовлетворяющую условию.
  /// Метод предназначен для внутренней логики специализированных сервисов.
  /// </summary>
  protected virtual async Task<T?> GetFirstOrDefaultAsync(
    Expression<Func<T, bool>> predicate,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(predicate);

    await using var context = CreateContext();

    LogInformation($"[{nameof(CrudService<T>)}] Получение первой записи {typeof(T).Name} по условию");
    return await context.Set<T>().FirstOrDefaultAsync(predicate, cancellationToken);
  }

  /// <summary>
  /// Возвращает все записи, удовлетворяющие условию.
  /// Метод предназначен для внутренней логики специализированных сервисов.
  /// </summary>
  protected virtual async Task<List<T>> GetByPredicateAsync(
    Expression<Func<T, bool>> predicate,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(predicate);

    await using var context = CreateContext();

    LogInformation($"[{nameof(CrudService<T>)}] Получение записей {typeof(T).Name} по условию");
    return await context.Set<T>()
      .Where(predicate)
      .ToListAsync(cancellationToken);
  }

  /// <summary>
  /// Возвращает первую запись таблицы без дополнительных условий.
  /// Используется для таблиц с одной строкой настроек.
  /// </summary>
  protected virtual async Task<T?> GetFirstOrDefaultAsync(CancellationToken cancellationToken = default)
  {
    await using var context = CreateContext();

    LogInformation($"[{nameof(CrudService<T>)}] Получение первой записи типа {typeof(T).Name}");
    return await context.Set<T>().FirstOrDefaultAsync(cancellationToken);
  }

  /// <summary>
  /// Сохраняет единственную строку таблицы:
  /// создаёт её при отсутствии или обновляет существующую запись.
  /// </summary>
  protected virtual async Task<T> SaveSingleAsync(
    T entity,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(entity);

    await using var context = CreateContext();

    LogInformation($"[{nameof(CrudService<T>)}] Сохранение одиночной записи типа {typeof(T).Name}");

    var existing = await context.Set<T>().FirstOrDefaultAsync(cancellationToken);
    if (existing == null)
    {
      context.Set<T>().Add(entity);
      await context.SaveChangesAsync(cancellationToken);
      return entity;
    }

    var entry = context.Entry(existing);
    var keyNames = entry.Metadata.FindPrimaryKey()?.Properties
      .Select(x => x.Name)
      .ToHashSet(StringComparer.Ordinal)
      ?? [];

    foreach (var propertyEntry in entry.Properties)
    {
      if (keyNames.Contains(propertyEntry.Metadata.Name))
      {
        continue;
      }

      var sourceProperty = typeof(T).GetProperty(propertyEntry.Metadata.Name);
      if (sourceProperty == null || !sourceProperty.CanRead)
      {
        continue;
      }

      propertyEntry.CurrentValue = sourceProperty.GetValue(entity);
    }

    await context.SaveChangesAsync(cancellationToken);
    return existing;
  }

  /// <summary>
  /// Создаёт экземпляр контекста базы данных.
  /// Вынесено в отдельный метод, чтобы при необходимости переопределить способ создания контекста.
  /// </summary>
  protected virtual AppDbContext CreateContext()
  {
    return new AppDbContext();
  }

  private static async Task ValidateUniqueDeviceNumberAsync(
    AppDbContext context,
    T entity,
    CancellationToken cancellationToken)
  {
    if (entity is not DeviceDto deviceDto)
    {
      return;
    }

    var parameter = Expression.Parameter(typeof(T), "x");
    Expression predicateBody =
      Expression.NotEqual(
        Expression.Property(parameter, nameof(DeviceDto.Id)),
        Expression.Constant(deviceDto.Id));

    predicateBody = Expression.AndAlso(
      predicateBody,
      Expression.Equal(
        Expression.Property(parameter, nameof(DeviceDto.Number)),
        Expression.Constant(deviceDto.Number)));

    string duplicateKeyDescription = $"номером {deviceDto.Number}";

    if (entity is AttachableDeviceDto attachableDeviceDto)
    {
      predicateBody = Expression.AndAlso(
        predicateBody,
        Expression.Equal(
          Expression.Property(parameter, nameof(AttachableDeviceDto.NumberChassis)),
          Expression.Constant(attachableDeviceDto.NumberChassis)));

      duplicateKeyDescription = $"шасси {attachableDeviceDto.NumberChassis} и номером {deviceDto.Number}";
    }

    var predicate = Expression.Lambda<Func<T, bool>>(predicateBody, parameter);
    bool duplicateExists = await context.Set<T>().AnyAsync(predicate, cancellationToken);

    if (!duplicateExists)
    {
      return;
    }

    throw new DuplicateEntityException(
      $"Запись типа {typeof(T).Name} с {duplicateKeyDescription} уже существует.");
  }

  private static Expression UnwrapExpression(Expression expression)
  {
    if (expression is UnaryExpression unaryExpression &&
        unaryExpression.NodeType == ExpressionType.Convert)
    {
      return unaryExpression.Operand;
    }

    return expression;
  }

  private static string TryGetPropertyName(Expression expression)
  {
    return expression is MemberExpression memberExpression
      ? memberExpression.Member.Name
      : expression.ToString();
  }
}
