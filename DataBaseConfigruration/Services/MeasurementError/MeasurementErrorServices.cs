using Ask.Core.Shared.DTO.Executor.MeasurementError;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using DataBaseConfiguration.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DataBaseConfiguration.Services.MeasurementError
{
  /// <summary>
  /// Сервис для получения метрологических погрешностей по типу команды и диапазону измерений.
  /// </summary>
  public class MeasurementErrorServices : Repository<MeasurementErrorEntity>, IMeasurementErrorProvider
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="MeasurementErrorServices"/>.
    /// </summary>
    public MeasurementErrorServices() : base(DataBaseConfig.Context)
    {
      EnsureDefaultData();
    }

    /// <summary>
    /// Возвращает все метрологические данные (типы команд и их диапазоны погрешностей),
    /// включая структуру диапазонов для каждой команды.
    /// </summary>
    /// <typeparam name="T">
    /// Тип возвращаемых данных (например, <see cref="MeasurementErrorEntity"/>).
    /// </typeparam>
    /// <returns>
    /// Коллекция объектов типа <typeparamref name="T"/>,
    /// каждая из которых содержит диапазоны (<see cref="MeasurementErrorRangeEntity"/>).
    /// </returns>
    public IEnumerable<T> GetAllWithRanges<T>()
    {
      if (typeof(T) == typeof(MeasurementErrorEntity))
      {
        // Загружаем сущности с диапазонами и приводим к типу T
        var data = _context.Set<MeasurementErrorEntity>()
                           .Include(e => e.Ranges)
                           .AsNoTracking()
                           .ToList();

        return (IEnumerable<T>)data;
      }

      throw new NotSupportedException(
        $"Тип {typeof(T).Name} не поддерживается реализацией {nameof(MeasurementErrorServices)}. " +
        $"Ожидается {nameof(MeasurementErrorEntity)}.");
    }

    /// <summary>
    /// Возвращает числовую и процентную погрешность по типу команды и значению измерения.
    /// </summary>
    /// <param name="type">Тип команды (режим метрологии).</param>
    /// <param name="measuredValue">Измеренное значение, для которого подбирается диапазон.</param>
    /// <returns>Кортеж: числовая и процентная погрешности.</returns>
    public (double Numeric, double Percent) GetErrorParameters(MeasurementTypeCommand type, double measuredValue)
    {
      var entity = _dbSet
        .Include(e => e.Ranges)
        .FirstOrDefault(e => e.Type == type);

      if (entity == null)
        throw new InvalidOperationException($"Не найдена запись погрешности для типа команды {type}.");

      var range = entity.Ranges.FirstOrDefault(r =>
          measuredValue >= r.MinValue &&
          (r.MaxValue == null || measuredValue < r.MaxValue));

      if (range == null)
        throw new InvalidOperationException($"Не найден диапазон погрешности для {type} при значении {measuredValue}.");

      return (range.NumericError, range.PercentageError);
    }

    /// <summary>
    /// Возвращает диапазон допустимых значений (Min, Max) для измеренного значения.
    /// </summary>
    /// <param name="type">Тип команды (режим метрологии).</param>
    /// <param name="expectedValue">Ожидаемое значение измерения.</param>
    /// <returns>Кортеж: нижняя и верхняя границы допустимого диапазона.</returns>
    public (double Min, double Max) GetRange(MeasurementTypeCommand type, double expectedValue)
    {
      var (numeric, percent) = GetErrorParameters(type, expectedValue);
      double min = expectedValue - numeric - (expectedValue * percent / 100.0);
      double max = expectedValue + numeric + (expectedValue * percent / 100.0);
      return (min, max);
    }

    /// <summary>
    /// Проверяет наличие данных и инициализирует значения по умолчанию при их отсутствии.
    /// </summary>
    private void EnsureDefaultData()
    {
      var defaults = new List<MeasurementErrorEntity>
      {
        new MeasurementErrorEntity(MeasurementTypeCommand.IE)
        {
            Ranges = new List<MeasurementErrorRangeEntity>
            {
                new() { MinValue = 0, MaxValue = null, PercentageError = 5.0, NumericError = 100.0 }
            }
        },
        new MeasurementErrorEntity(MeasurementTypeCommand.PR)
        {
            Ranges = new List<MeasurementErrorRangeEntity>
            {
                new() { MinValue = 0, MaxValue = null, PercentageError = 1.0, NumericError = 0.8 }
            }
        },
        new MeasurementErrorEntity(MeasurementTypeCommand.KC)
        {
            Ranges = new List<MeasurementErrorRangeEntity>
            {
                new() { MinValue = 0.001, MaxValue = 1_000_000, PercentageError = 1.0, NumericError = 1.0 },
                new() { MinValue = 1_000_000, MaxValue = null, PercentageError = 5.0, NumericError = 0.0 }
            }
        },
        new MeasurementErrorEntity(MeasurementTypeCommand.SI)
        {
            Ranges = new List<MeasurementErrorRangeEntity>
            {
                new() { MinValue = 0, MaxValue = null, PercentageError = 2.0, NumericError = 0.0 }
            }
        }
    };

      foreach (var def in defaults)
      {
        // ищем сущность по типу
        var existing = _dbSet
            .Include(e => e.Ranges)
            .FirstOrDefault(e => e.Type == def.Type);

        if (existing == null)
        {
          // если не найдено — добавляем полностью новую запись
          _dbSet.Add(def);
        }
        else if (existing.Ranges == null || !existing.Ranges.Any())
        {
          // если есть, но без диапазонов — добавляем их
          foreach (var range in def.Ranges)
            existing.Ranges.Add(range);
        }
      }

      _context.SaveChanges();
    }

  }
}
