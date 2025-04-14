using System;
using System.Collections.Generic;
using System.IO;

namespace UI.Components.FileComparerControls
{

  public class FileCompare
  {
    /// <summary>
    /// Сравнивает содержимое двух текстовых файлов построчно и возвращает словари с различиями.
    /// </summary>
    /// <param name="path1">Путь к первому файлу.</param>
    /// <param name="path2">Путь ко второму файлу.</param>
    /// <returns>
    /// Список из двух словарей: 
    /// - Первый словарь содержит строки, отличающиеся в первом файле (индекс → строка).
    /// - Второй словарь — различия во втором файле.
    /// </returns>
    public static List<Dictionary<int, string>> CompareFileContents(string path1, string path2)
    {
      var lines1 = File.ReadAllLines(path1);
      var lines2 = File.ReadAllLines(path2);
      return CompareLineArrays(lines1, lines2);
    }

    /// <summary>
    /// Сравнивает два массива строк и возвращает различающиеся строки.
    /// </summary>
    /// <param name="lines1">Строки из первого файла.</param>
    /// <param name="lines2">Строки из второго файла.</param>
    /// <returns>
    /// Список из двух словарей:
    /// - Первый словарь содержит строки, отличающиеся в первом массиве.
    /// - Второй — во втором.
    /// </returns>
    public static List<Dictionary<int, string>> CompareLineArrays(string[] lines1, string[] lines2)
    {
      int maxLines = Math.Max(lines1.Length, lines2.Length);
      int diffCountFirst = 0;
      int diffCountSecond = 0;

      var differencesFirst = new Dictionary<int, string>();
      var differencesSecond = new Dictionary<int, string>();

      for (int i = 0; i < maxLines; i++)
      {
        int index1 = Math.Min(i + diffCountFirst, lines1.Length - 1);
        int index2 = Math.Min(i + diffCountSecond, lines2.Length - 1);

        string line1 = i < lines1.Length ? lines1[index1] : string.Empty;
        string line2 = i < lines2.Length ? lines2[index2] : string.Empty;

        if (line1 == line2)
          continue;

        if (string.IsNullOrEmpty(line1) && i + 1 < lines1.Length)
        {
          string nextLine1 = lines1[index1 + 1];
          if (nextLine1 == line2)
          {
            diffCountFirst++;
            continue;
          }
        }

        if (string.IsNullOrEmpty(line2) && i + 1 < lines2.Length)
        {
          string nextLine2 = lines2[index2 + 1];
          if (nextLine2 == line1)
          {
            diffCountSecond++;
            continue;
          }
        }

        if (i < lines1.Length && !differencesFirst.ContainsKey(index1))
          differencesFirst.Add(index1, lines1[index1]);

        if (i < lines2.Length && !differencesSecond.ContainsKey(index2))
          differencesSecond.Add(index2, lines2[index2]);
      }

      return new List<Dictionary<int, string>> { differencesFirst, differencesSecond };
    }
  }
}