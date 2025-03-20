using NewCore.Base.Interface.Main;

namespace Mode.TestSuite
{
  static internal class BlockNumberGenerator
  {
    public static List<IRelaySwitchModule> GetBlockModelsBetween(
        IRelaySwitchModule first,
        IRelaySwitchModule last)
    {
      List<IRelaySwitchModule> allBlocks = ConfigCollector.GetMkrModels();

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
