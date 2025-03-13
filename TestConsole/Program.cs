namespace TestConsole
{
  internal class Program
  {
    static void Main(string[] args)
    {
      Console.WriteLine("=== Главное меню ===");

      while (true)
      {
        // Отображаем доступные опции
        Console.WriteLine("\nВыберите действие:");
        Console.WriteLine("1. Тест COM-портов");
        Console.WriteLine("2. Работа с базой данных");
        Console.WriteLine("3. Работа с Keysight");
        Console.WriteLine("0. Выход");

        // Запрашиваем выбор пользователя
        Console.Write("Введите номер действия: ");
        if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 0 || choice > 3)
        {
          Console.WriteLine("Неверный выбор. Попробуйте снова.");
          continue;
        }

        // Обработка выбора
        switch (choice)
        {
          case 1:
            // Запускаем тест COM-портов
            COMTest.Run();
            break;

          case 2:

            DBTest.Run();
            break;

          case 3:

            TestKeysight.RunAsync();
            break;

          case 0:
            // Выход из программы
            Console.WriteLine("Выход из программы...");
            return;

          default:
            Console.WriteLine("Неверный выбор. Попробуйте снова.");
            break;
        }
      }
    }
  }
}
