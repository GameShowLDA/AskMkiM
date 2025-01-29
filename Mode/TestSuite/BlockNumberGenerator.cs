using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.ConfigCollector;

namespace Mode.TestSuite
{
  static internal class BlockNumberGenerator
  {
    public static List<Core.ModuleRelayControl.Model> GetBlockModelsBetween(
        Core.ModuleRelayControl.Model first,
        Core.ModuleRelayControl.Model last)
    {
      List<Core.ModuleRelayControl.Model> allBlocks = ConfigCollector.GetMkrModels();

      // Найти индексы начального и конечного блоков
      int startIndex = allBlocks.FindIndex(block => block.Number == first.Number);
      int endIndex = allBlocks.FindIndex(block => block.Number == last.Number);

      // Убедиться, что индексы корректны
      if (startIndex == -1 || endIndex == -1 || startIndex > endIndex)
      {
        throw new ArgumentException("Некорректные номера блоков или порядок.");
      }

      // Извлечь подмножество блоков
      return allBlocks.Skip(startIndex).Take(endIndex - startIndex + 1).ToList();
    }
  }
}
