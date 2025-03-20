using AppConfig.DataBase.Repositories;
using NewCore.Base.Interface.Main;

namespace Mode.TestSuite
{
  /// <summary>
  /// Класс предоставляет методы для работы с номерами блоков реле,
  /// включая получение последовательности блоков в заданном диапазоне.
  /// </summary>
  static internal class BlockNumberGenerator
  {
    /// <summary>
    /// Извлекает подмножество блоков реле из общей коллекции, находящихся
    /// между заданными первым и последним блоками (включительно).
    /// </summary>
    /// <param name="first">Начальный блок реле.</param>
    /// <param name="last">Конечный блок реле.</param>
    /// <returns>Список блоков реле от first до last (включительно).</returns>
    /// <exception cref="ArgumentException">
    /// Выбрасывается, если указанные блоки не найдены или их порядок некорректен.
    /// </exception>
    public static List<IRelaySwitchModule> GetBlockModelsBetween(
        IRelaySwitchModule first,
        IRelaySwitchModule last)
    {
      var allBlocks = new RelaySwitchModuleRepository().GetAll().Cast<IRelaySwitchModule>().ToList();

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
