using TestConsole.GPT;
using TestConsole.MINT;

namespace TestConsole
{
  internal class Program
  {
    private static async Task Main(string[] args)
    {
      while (true)
      {
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
        Console.WriteLine("11. Тест темы");
        Console.WriteLine("12. Скан папки");
        Console.WriteLine("13. Отладка UPS");
        Console.WriteLine("14. Проверка namespace по папкам");
        Console.WriteLine("15. New DB check");
        Console.WriteLine("16. Добавить устройство в новую БД");
        Console.WriteLine("0. Exit");
        Console.Write("Введите номер действия: ");
        if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 0 || choice > 16)
        {
          Console.Write("Введите номер действия: ");
          continue;
        }

        switch (choice)
        {
          case 1:
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

          case 11:
            ThemeManager.RunAsync();
            break;

          case 12:
            FolderTreePrinter.Run();
            break;

          case 13:
            await UninterruptiblePowerSupplyTest.RunAsync();
            break;

          case 14:
            NamespaceFolderScanner.Run();
            break;

          case 15:
            await ProviderDatabaseCheck.RunAsync();
            break;

          case 16:
            await DatabaseDeviceCreate.RunAsync();
            break;

          case 0:
            Console.WriteLine("Exit...");
            return;

          default:
            Console.WriteLine("Неверный выбор. Попробуйте снова.");
            break;
        }
      }
    }

    private static void WriteStartupLog(string message)
    {
      Console.ForegroundColor = ConsoleColor.DarkGray;
      Console.WriteLine(message);
      Console.ResetColor();
    }
  }
}
