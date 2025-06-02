using AppConfiguration.Enums;
using DataBaseConfiguration.Models.MeasurementError;
using System.Reflection;

namespace DataBaseConfiguration.Services.MeasurementError
{
  internal static class MeasurementErrorSeeder
  {
    /// <summary>
    /// Выполняет инициализацию записей MeasurementErrorEntity по умолчанию из атрибутов.
    /// </summary>
    /// <param name="context">Контекст базы данных.</param>
    public static void Seed(AppDbContext context)
    {
      if (context.MeasurementErrors.Any())
        return;

      var defaults = new List<MeasurementErrorEntity>();

      foreach (var type in Enum.GetValues(typeof(TypeCommand)).Cast<TypeCommand>())
      {
        var member = typeof(TypeCommand).GetMember(type.ToString()).FirstOrDefault();
        var attribute = member?.GetCustomAttribute<CommandInfoAttribute>();

        if (attribute != null)
        {
          defaults.Add(new MeasurementErrorEntity
          {
            Type = type,
            PercentageError = attribute.DefaultPercentage,
            NumericError = attribute.DefaultNumeric
          });
        }
      }

      context.MeasurementErrors.AddRange(defaults);
      context.SaveChanges();

      Console.ForegroundColor = ConsoleColor.Yellow;
      Console.WriteLine("🌿 MeasurementErrorSeeder: Записи успешно созданы из атрибутов.");
      Console.ResetColor();
    }
  }
}
