using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Utilities.LoggerUtility;

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
