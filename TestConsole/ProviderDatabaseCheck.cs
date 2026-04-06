using Ask.DataBase.Engine.Initialization;

namespace TestConsole;

internal static class ProviderDatabaseCheck
{
  public static async Task RunAsync()
  {
    Console.WriteLine();
    Console.WriteLine("=== Проверка новой БД ===");

    var report = await DatabaseEngineInitializer.InitializeAsync(WriteLog);

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine(
      $"Итог: файл базы {(report.DatabaseAlreadyExisted ? "существовал" : "отсутствовал")}, " +
      $"пересоздана: {report.DatabaseRecreated}, " +
      $"миграций применено: {report.AppliedMigrations}, " +
      $"использован EnsureCreated: {report.UsedEnsureCreated}, " +
      $"горячих клавиш добавлено: {report.SeededHotkeys}, " +
      $"строк настроек создано: {report.CreatedDefaultSettingsRows}");
    Console.ResetColor();
  }

  private static void WriteLog(string message)
  {
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine(message);
    Console.ResetColor();
  }
}
