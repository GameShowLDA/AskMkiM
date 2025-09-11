using AppConfiguration;
using DataBaseConfiguration;
using Microsoft.Extensions.Hosting;
using TestConsole.GPT;
using TestConsole.MINT;
using Microsoft.Extensions.Hosting;


namespace TestConsole
{
  internal class Program
  {
    static async Task Main(string[] args)
    {
      var db = DataBaseConfig.InitializeDB();
      Console.ForegroundColor = ConsoleColor.White;

      Console.WriteLine("=== Главное меню ===");

      while (true)
      {
        // Отображаем доступные опции
        Console.WriteLine("\nВыберите действие:");
        Console.WriteLine("1. Тест COM-портов");
        Console.WriteLine("2. Работа с базой данных");
        Console.WriteLine("3. Работа с Keysight");
        Console.WriteLine("4. Самоконтроль УКШ");
        Console.WriteLine("5. Самоконтроль МИНТ");
        Console.WriteLine("6. Проверка ввода данных");
        Console.WriteLine("7. ППУ");
        Console.WriteLine("8. МИНТ колибровка");
        Console.WriteLine("9. Тест вентиляторов");
        Console.WriteLine("10. Тест списка точек");
        Console.WriteLine("0. Выход");

        // Запрашиваем выбор пользователя
        Console.Write("Введите номер действия: ");
        if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 0 || choice > 10)
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

          case 4:

            await DBC_SelfControl.RunAsync();
            break;

          case 5:

            await Mint_Test.RunAsync();
            break;

          case 6:

            InputValidator.Validate();
            break;

          case 7:
            await GPT_Test.RunAsync();
            break;


          case 8:
            await ResistanceCalibrationEditor.RunAsync();
            break;

          case 9:
            await TestAirSpeed.RunAsync();
            break;


          case 10:
            await DictonaryManager.RunAsync();
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
