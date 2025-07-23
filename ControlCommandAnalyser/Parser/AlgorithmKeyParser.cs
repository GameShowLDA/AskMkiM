using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlCommandAnalyser.Parser
{
  /// <summary>
  /// Утилита для извлечения ключей алгоритма из строки команды.
  /// </summary>
  public static class AlgorithmKeyParser
  {
    /// <summary>
    /// Извлекает все ключи из текста команды, присутствующие в перечислении AlgorithmKey.
    /// </summary>
    /// <param name="line">Исходная строка команды.</param>
    /// <returns>Список найденных ключей.</returns>
    public static List<string> ExtractKeys(string line)
    {
      if (string.IsNullOrWhiteSpace(line))
        return new();

      var enumKeys = Enum.GetNames(typeof(AlgorithmKey));

      // Разбиваем строку и ищем совпадения с допустимыми ключами (точно по имени)
      return line
        .Split(new[] { ' ', '\t', ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
        .Where(token => enumKeys.Contains(token))
        .Distinct()
        .ToList();
    }
  }
}
