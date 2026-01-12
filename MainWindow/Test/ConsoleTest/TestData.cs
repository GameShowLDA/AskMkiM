using static Ask.LogLib.LoggerUtility;

namespace MainWindowProgram.Test.ConsoleTest
{
  static internal class TestData
  {
    static internal async Task PrintTestData()
    {
      int i = 1;
      while (true)
      {
        LogInformation($"Тест {i}");
        await Task.Delay(10); // Задержка в 1 секунду между выводами
        i++;
      }
    }
  }
}
