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
      var differencesFirst = new Dictionary<int, string>();
      var differencesSecond = new Dictionary<int, string>();

      int i = 0, j = 0;

      while (i < lines1.Length || j < lines2.Length)
      {
        // Пропускаем пустые строки (или состоящие только из пробелов)
        while (i < lines1.Length && string.IsNullOrWhiteSpace(lines1[i]))
          i++;
        while (j < lines2.Length && string.IsNullOrWhiteSpace(lines2[j]))
          j++;

        if (i >= lines1.Length && j >= lines2.Length)
          break;

        string line1 = i < lines1.Length ? lines1[i].Trim() : string.Empty;
        string line2 = j < lines2.Length ? lines2[j].Trim() : string.Empty;

        if (line1 != line2)
        {
          if (i < lines1.Length)
            differencesFirst[i] = lines1[i];
          if (j < lines2.Length)
            differencesSecond[j] = lines2[j];
        }

        i++;
        j++;
      }

      return new List<Dictionary<int, string>> { differencesFirst, differencesSecond };
    }

  }
}