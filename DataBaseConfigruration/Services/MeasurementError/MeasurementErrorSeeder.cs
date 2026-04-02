using Ask.Core.Shared.DTO.Executor.MeasurementError;
using Ask.Core.Shared.Metadata.Atributes;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using DataBaseConfiguration.Context;
using System.Reflection;

namespace DataBaseConfiguration.Services.MeasurementError
{
  /// <summary>
  /// Выполняет инициализацию таблицы MeasurementErrors начальными значениями.
  /// </summary>
  internal static class MeasurementErrorSeeder
  {
    /// <summary>
    /// Выполняет инициализацию записей MeasurementErrorEntity с диапазонами погрешностей.
    /// Если таблица уже содержит данные — инициализация не выполняется.
    /// </summary>
    /// <param name="context">Контекст базы данных.</param>
    public static void Seed(AppDbContext context)
    {
      if (context.MeasurementErrors.Any())
        return;

      var defaults = new List<MeasurementErrorEntity>();

      // Перебираем все значения перечисления TypeCommand
      foreach (var type in Enum.GetValues(typeof(MeasurementTypeCommand)).Cast<MeasurementTypeCommand>())
      {
        var member = typeof(MeasurementTypeCommand).GetMember(type.ToString()).FirstOrDefault();
        var attribute = member?.GetCustomAttribute<CommandMeasurementAttribute>();

        if (attribute != null)
        {
          // Если у команды в атрибуте задан диапазон — используем его
          var entity = new MeasurementErrorEntity(type);

          // Если у атрибута есть диапазон — создаём одну запись
          if (attribute.DefaultMinRange.HasValue || attribute.DefaultMaxRange.HasValue)
          {
            entity.Ranges.Add(new MeasurementErrorRangeEntity
            {
              MinValue = attribute.DefaultMinRange ?? 0,
              MaxValue = attribute.DefaultMaxRange,
              NumericError = attribute.DefaultNumeric,
              PercentageError = attribute.DefaultPercentage
            });
          }
          else
          {
            // Если диапазон не указан — добавляем один общий диапазон "от 0 до ∞"
            entity.Ranges.Add(new MeasurementErrorRangeEntity
            {
              MinValue = 0,
              MaxValue = null,
              NumericError = attribute.DefaultNumeric,
              PercentageError = attribute.DefaultPercentage
            });
          }

          defaults.Add(entity);
        }
      }

      context.MeasurementErrors.AddRange(defaults);
      context.SaveChanges();

      Console.ForegroundColor = ConsoleColor.Yellow;
      Console.WriteLine("🌿 MeasurementErrorSeeder: записи MeasurementErrorEntity и диапазоны успешно созданы из атрибутов.");
      Console.ResetColor();
    }
  }
}
