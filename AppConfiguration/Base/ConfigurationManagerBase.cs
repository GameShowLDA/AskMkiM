using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Utilities.LoggerUtility;

namespace AppConfiguration.Base
{
  /// <summary>
  /// Базовый абстрактный класс для управления конфигурационными файлами.
  /// </summary>
  public abstract class ConfigurationManagerBase<T>
  {
    /// <summary>
    /// Получает путь к конфигурационному файлу.
    /// </summary>
    public string PathFile { get; set; }

    /// <summary>
    /// Перезаписывает конфигурационный файл.
    /// </summary>
    /// <param name="data">Объект данных.</param>
    public abstract Task RewriteFileAsync(T data);

    /// <summary>
    /// Читает данные из конфигурационного файла.
    /// </summary>
    public abstract Task<T> ReadFileAsync();

    /// <summary>
    /// Асинхронно создаёт файл по указанному пути, если он не существует. Также асинхронно создаёт директорию, если она не существует.
    /// </summary>
    /// <param name="path">Полный путь к файлу, который необходимо создать.</param>
    /// <returns>Возвращает false, если файл не существовал, иначе true.</returns>
    public async Task<bool> CreateFileIfNotExistsAsync()
    {
      bool exist = true;

      var directory = Path.GetDirectoryName(PathFile);

      if (!string.IsNullOrEmpty(directory))
      {
        if (!Directory.Exists(directory))
        {
          await Task.Run(() => Directory.CreateDirectory(directory));
          exist = false;
        }

        if (!File.Exists(PathFile))
        {
          await Task.Run(() => File.Create(PathFile).Dispose());
          exist = false;
        }
      }
      else
      {
        LogError($"Системная ошибка. Путь задан неверно : {PathFile}");
      }

      return exist;
    }

    /// <summary>
    /// Создаёт файл по указанному пути, если он не существует. Также создает директорию, если она не существует.
    /// </summary>
    /// <param name="path">Полный путь к файлу, который необходимо создать.</param>
    /// <returns>Возвращает false, если файл не существовал, иначе true.</returns>
    public bool CreateFileIfNotExists()
    {
      bool exist = true;

      var directory = Path.GetDirectoryName(PathFile);
      if (!string.IsNullOrEmpty(directory))
      {
        if (!Directory.Exists(directory))
        {
          Directory.CreateDirectory(directory);
          exist = false;
        }

        if (!File.Exists(PathFile))
        {
          using (File.Create(PathFile)) { }
          exist = false;
        }
      }
      else
      {
        LogError($"Системная ошибка. Путь задан неверно : {PathFile}");
      }

      return exist;
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ConfigurationManagerBase"/>.
    /// </summary>
    /// <param name="filePath">Путь к файлу.</param>
    public ConfigurationManagerBase(string filePath)
    {
      PathFile = filePath;
    }
  }
}
