using Ask.Core.Shared.DTO.Executor.MeasurementError;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;

namespace Ask.Core.Shared.Metadata.Static
{
  /// <summary>
  /// Статический класс с базовыми (эталонными) значениями погрешностей для каждой команды метрологии.
  /// 
  /// Используется для:
  /// <list type="bullet">
  ///   <item>Инициализации БД при первом запуске.</item>
  ///   <item>Сравнения текущих погрешностей с заводскими.</item>
  ///   <item>Отображения эталонных данных в UI.</item>
  /// </list>
  /// </summary>
  public static class MeasurementErrorDefaults
  {
    /// <summary>
    /// Список эталонных погрешностей, сгруппированных по типу команды.
    /// </summary>
    public static readonly List<MeasurementErrorEntity> DefaultErrors = new()
    {
      new MeasurementErrorEntity(MeasurementTypeCommand.KC)
      {
        Ranges = new List<MeasurementErrorRangeEntity>
        {
          new MeasurementErrorRangeEntity { MinValue = 0.001, MaxValue = 1000000, NumericError = 0.8, PercentageError = 1 },
          new MeasurementErrorRangeEntity { MinValue = 1000001, PercentageError = 5 },
        }
      },

      new MeasurementErrorEntity(MeasurementTypeCommand.IE)
      {
        Ranges = new List<MeasurementErrorRangeEntity>
        {
          new MeasurementErrorRangeEntity { MinValue = 0.2, MaxValue = 100000, NumericError = 0.5, PercentageError = 5  },
        }
      },

      new MeasurementErrorEntity(MeasurementTypeCommand.PR)
      {
        Ranges = new List<MeasurementErrorRangeEntity>
        {
          new MeasurementErrorRangeEntity { MinValue = 1, MaxValue = 1000, NumericError = 0.8, PercentageError = 5 },
        }
      },

      new MeasurementErrorEntity(MeasurementTypeCommand.SI)
      {
        Ranges = new List<MeasurementErrorRangeEntity>
        {
          new MeasurementErrorRangeEntity { MinValue = 1, MaxValue = 1000, PercentageError = 10 },
        }
      },

      new MeasurementErrorEntity(MeasurementTypeCommand.KN_DCW)
      {
        Ranges = new List<MeasurementErrorRangeEntity>
        {
          new MeasurementErrorRangeEntity { MinValue = 0.1, PercentageError = 1 },
        }
      },

      new MeasurementErrorEntity(MeasurementTypeCommand.KN_ACW)
      {
        Ranges = new List<MeasurementErrorRangeEntity>
        {
          new MeasurementErrorRangeEntity { MinValue = 0.1, NumericError = 0.5,  PercentageError = 3 },
        }
      },

      new MeasurementErrorEntity(MeasurementTypeCommand.PI_DCW)
      {
        Ranges = new List<MeasurementErrorRangeEntity>
        {
          new MeasurementErrorRangeEntity {  MinValue = 50,  MaxValue = 1000, NumericError = 0,  PercentageError = 5 },
        }
      },

      new MeasurementErrorEntity(MeasurementTypeCommand.PI_ACW)
      {
        Ranges = new List<MeasurementErrorRangeEntity>
        {
          new MeasurementErrorRangeEntity { MinValue = 50,  MaxValue = 650, NumericError = 3,  PercentageError = 2 },
        }
      },

      new MeasurementErrorEntity(MeasurementTypeCommand.EHT)
      {
        Ranges = new List<MeasurementErrorRangeEntity>
        {
          new MeasurementErrorRangeEntity { MinValue = 0.1, MaxValue = 1, NumericError = 0.05 },
          new MeasurementErrorRangeEntity { MinValue = 1, MaxValue = 100, PercentageError = 5 },
        }
      },
    };

    /// <summary>
    /// Возвращает эталонную конфигурацию погрешностей для указанного типа команды.
    /// </summary>
    /// <param name="type">Тип команды метрологии.</param>
    /// <returns>Экземпляр <see cref="MeasurementErrorEntity"/> или <c>null</c>, если не найден.</returns>
    public static MeasurementErrorEntity? GetDefaultsFor(MeasurementTypeCommand type)
    {
      return DefaultErrors.FirstOrDefault(e => e.Type == type);
    }

    /// <summary>
    /// Вычисляет допустимую область значений измерения для заданной команды и измеренного значения.
    /// 
    /// Формула расчёта погрешности:
    /// <c>Δ = NumericError + (PercentageError / 100 * measuredValue)</c>  
    /// <c>Нижняя граница = measuredValue - Δ</c>  
    /// <c>Верхняя граница = measuredValue + Δ</c>
    /// 
    /// Если <see cref="MeasurementErrorRangeEntity.MaxValue"/> не указано, диапазон считается бесконечным.  
    /// Если <see cref="MeasurementErrorRangeEntity.NumericError"/> или <see cref="MeasurementErrorRangeEntity.PercentageError"/> не указаны, они считаются равными 0.
    /// </summary>
    /// <param name="type">Тип метрологической команды.</param>
    /// <param name="measuredValue">Измеренное значение, для которого требуется рассчитать диапазон погрешности.</param>
    /// <returns>
    /// Кортеж из трёх значений:
    /// <list type="bullet">
    ///   <item><c>LowerBound</c> — нижняя граница допустимого значения.</item>
    ///   <item><c>UpperBound</c> — верхняя граница допустимого значения.</item>
    ///   <item><c>Delta</c> — рассчитанная погрешность.</item>
    /// </list>
    /// </returns>
    public static (double LowerBound, double UpperBound, double Delta) CalculateToleranceRange(MeasurementTypeCommand type, double measuredValue)
    {
      var config = GetDefaultsFor(type);
      if (config == null)
        throw new InvalidOperationException($"❌ Не найдены эталонные погрешности для команды: {type}");

      var range = config.Ranges.FirstOrDefault(r =>
          measuredValue >= r.MinValue &&
          (r.MaxValue == null || measuredValue <= r.MaxValue));

      if (range == null)
      {
        var minConfiguredValue = config.Ranges.Min(r => r.MinValue);
        if (measuredValue < minConfiguredValue)
          throw new InvalidOperationException($"❌ Не удалось определить диапазон погрешности для команды {type}");

        range = config.Ranges
            .OrderByDescending(r => r.MaxValue ?? double.MaxValue)
            .FirstOrDefault();

        if (range == null)
          throw new InvalidOperationException($"❌ Не удалось определить диапазон погрешности для команды {type}");
      }

      double numericError = range.NumericError;
      double percentageError = range.PercentageError;

      double delta = numericError + (percentageError / 100.0 * measuredValue);
      double lowerBound = measuredValue - delta;
      double upperBound = measuredValue + delta;

      return (lowerBound, upperBound, delta);
    }
  }
}
