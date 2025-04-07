using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.Components.ArchiveManager.Models;
using static Utilities.LoggerUtility;


namespace UI.Components.ArchiveManager.ArchiveFiles
{
  public class PkEditor
  {
    /// <summary>
    /// Преобразует pk файл в opk файл.
    /// </summary>
    /// <param name="filePath">Путь к файлу.</param>
    /// <returns></returns>
    public async Task<bool> ConvertPkToOpk(string filePath, string archivePath)
    {
      FileInfo file;
      var content = new List<string>();
      try
      {
        if (File.Exists(filePath))
        {
          file = new FileInfo(filePath);

          if (TryReadFile(filePath, out content))
          {
            var newOpk = ParsePkToOpk(content);
            if (newOpk != null)
            {
              newOpk.Creation = File.GetLastWriteTime(filePath);
              var fileName = $"{file.Name.Remove(file.Name.LastIndexOf("."))}.opk";
              newOpk.OpkFilename = fileName;
              var opkEditor = new OpkEditor();
              await opkEditor.CreateOpk(archivePath, newOpk, content);

              return true;
            }
            else
            {
              return false;
            }
          }
          else
          {
            return false;
          }
        }
        else
        {
          LogWarning("Файл не найден");
          return false;
        }
      }
      catch (Exception ex)
      {
        LogError($"Ошибка при изменении расширения: {ex.Message}");
        return false;
      }
    }

    /// <summary>
    /// Безопасно читает файл с обработкой ошибок.
    /// </summary>
    public bool TryReadFile(string filePath, out List<string> content)
    {
      content = new List<string>();
      try
      {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        if (!File.Exists(filePath))
        {
          LogWarning($"Файл не найден: {filePath}");
          return false;
        }
        foreach (string line in File.ReadLines(filePath, Encoding.GetEncoding(866)))
        {
          if (!string.IsNullOrEmpty(line))
          {
            content.Add(line);
          }
        }
        return true;
      }
      catch (Exception ex)
      {
        LogError($"Ошибка при чтении файла: {ex.Message}");
        return false;
      }
    }

    /// <summary>
    /// Находит строки из pk файла, которые необходимо будет добавить в заголовочный файл index.json.
    /// </summary>
    /// <param name="pkContent">Строки, считанные из pk файла.</param>
    /// <returns>Коллекцию строк с необходимыми строками.</returns>
    private List<string> FindStrings(List<string> pkContent)
    {
      var start = false;
      var result = new List<string>();
      foreach (string line in pkContent)
      {
        if (line.StartsWith("10"))
        {
          start = true;
          result.Add(line);
        }
        else if (int.TryParse(line.Substring(0, 1), out int number))
        {
          start = false;
          break;
        }
        else
        {
          if (start)
          {
            result.Add(line);
          }
        }
      }
      return result;
    }

    /// <summary>
    /// Получает информацию из pk файла, затем заполняет необходимые поля для заполнения данных об opk файле для index.json.
    /// </summary>
    /// <param name="pkContent">Данные, полученные из pk файла.</param>
    /// <returns>Объект OpkFile.</returns>
    public OpkFile ParsePkToOpk(List<string> pkContent)
    {
      var opkFile = new OpkFile();
      var content = FindStrings(pkContent);
      var dictionary = new Dictionary<string, string>();
      foreach (string line in content)
      {
        if (line.ToLower().Contains("ок") && !dictionary.ContainsKey("ок"))
        {
          AddValueWithoutTab(dictionary, "ок", line);
        }
        else if (line.ToLower().Contains("заказ") && !dictionary.ContainsKey("заказ"))
        {
          AddValueWithoutTab(dictionary, "заказ", line);
        }
        else if (line.ToLower().Contains("цех") && !dictionary.ContainsKey("цех"))
        {
          AddValueWithoutTab(dictionary, "цех", line);
        }
        else if (line.ToLower().Contains("прим") && !dictionary.ContainsKey("прим"))
        {
          AddValueWithoutTab(dictionary, "прим", line);
        }
      }
      if (!dictionary.ContainsKey("прим"))
      {
        dictionary.Add("прим", string.Empty);
      }
      if (dictionary.Count == 4)
      {
        var strWithoutSpaces = string.Join(string.Empty, dictionary["ок"].Split(' '));
        var startIndex = strWithoutSpaces.IndexOf("10") + 4;
        var endIndex = strWithoutSpaces.LastIndexOf("*");
        opkFile.Marking = strWithoutSpaces.Substring(startIndex, endIndex - startIndex);
        opkFile.Name = dictionary["ок"].Substring(dictionary["ок"].LastIndexOf("*") + 1);
        opkFile.Order = RemoveSpaces(dictionary["заказ"]);
        opkFile.Department = RemoveSpaces(dictionary["цех"]);
        if (dictionary["прим"] == null || dictionary["прим"] == string.Empty)
        {
          opkFile.Description = dictionary["прим"];
        }
        opkFile.Description = RemoveSpaces(dictionary["прим"]);

      }
      return opkFile;
    }

    /// <summary>
    /// Удаляет из строки пробелы, получает подстроку по заданным параметрам.
    /// </summary>
    /// <param name="value">Заданная подстрока.</param>
    /// <returns>Полученную в результате изменений строку.</returns>
    private static string RemoveSpaces(string value)
    {
      var str = string.Join(string.Empty, value.Split(' '));
      return str.Substring(str.LastIndexOf("=") + 1);
    }

    /// <summary>
    /// Удаляет табуляцию в строке, если она есть и добавляет измененную строку в словарь.
    /// </summary>
    /// <param name="dictionary">Словарь для хранения данных парсинга.</param>
    /// <param name="key">Ключ, по которому нужно добавить значение в словарь.</param>
    /// <param name="line">Строка, поступившая на обработку.</param>
    private static void AddValueWithoutTab(Dictionary<string, string> dictionary, string key, string line)
    {
      if (line.Contains("\t"))
      {
        line = line.Replace("\t", string.Empty);
      }
      dictionary.Add(key, line);
    }
  }
}
