using System.IO;
using Utilities.Encrypter;
using static Utilities.LoggerUtility;

namespace UI.Components.ArchiveManager.ArchiveFiles.ApkwArchive
{
  public class ArchiveEncryption
  {
    /// <summary>
    /// Словарь для хранения временных путей к расшифрованным архивам.
    /// </summary>
    private static readonly Dictionary<string, string> _tempPaths = new Dictionary<string, string>();

    /// <summary>
    /// Выполняет безопасную операцию над зашифрованным архивом с автоматическим шифрованием/дешифрованием.
    /// </summary>
    /// <typeparam name="T">Тип возвращаемого значения операции.</typeparam>
    /// <param name="operation">Делегат, представляющий операцию, которую необходимо выполнить над расшифрованным архивом.</param>
    /// <param name="archivePath">Путь к зашифрованному архиву.</param>
    /// <returns>Результат выполнения операции типа T.</returns>
    public async Task<T> ExecuteSecureOperation<T>(Func<string, Task<T>> operation, string archivePath, bool isNewArchive = false)
    {
      var archiveName = Path.GetFileName(Path.GetFullPath(archivePath));
      LogInformation($"Начало операции с файлом: {archiveName}");

      string tempPath = string.Empty; 
      tempPath = ChoseTempPath(archiveName); // Создает временный файл
      
      try
      {
        if (!isNewArchive && File.Exists(archivePath))
        {
          DecryptArchiveProcess(archivePath, archiveName, tempPath);
        }
        else
        {
          // Если архив новый, временный файл не нужен
          LogInformation("Создание нового архива, временный файл не создается.");
        }

        LogInformation($"Выполнение операции над файлом: {archiveName}");
        T result = await operation(tempPath);

        if (File.Exists(tempPath))
        {
          EncryptArchiveProcess(archivePath, archiveName, tempPath);
        }

        LogInformation($"Операция успешно завершена");
        return result;
      }
      catch (Exception ex)
      {
        LogError($"Ошибка при выполнении операции с файлом: {archiveName}");
        throw;
      }
      finally
      {
        if (System.IO.File.Exists(tempPath))
        {
          RemoveTempFile(archiveName, tempPath);
        }
      }
    }

    #region Работа с временными файлами и путями

    /// <summary>
    /// Создает новый временный путь или получает из словаря ранее созданный временный путь.
    /// </summary>
    /// <param name="archiveName">Название архива.</param>
    /// <returns>Временный путь к архиву.</returns>
    private static string ChoseTempPath(string archiveName)
    {
      string tempPath;
      if (!_tempPaths.ContainsKey(archiveName))
      {
        tempPath = CreateTempPath(archiveName);
      }
      else
      {
        tempPath = _tempPaths[archiveName];
        LogInformation($"Использован существующий временный путь: {tempPath}");
      }

      return tempPath;
    }

    /// <summary>
    /// Создает временный путь для работы с временным файлом архива.
    /// </summary>
    /// <param name="archiveName">Название архива.</param>
    /// <returns>Строку с временным путем к архиву.</returns>
    private static string CreateTempPath(string archiveName, bool needCreate = false)
    {
      string tempPath = Path.Combine(Path.GetTempPath(), ArchiveSettings.TempArchivePath, archiveName);
      int counter = 1;
      int indexLast = tempPath.LastIndexOf("\\");

      string directory = tempPath.Substring(0, indexLast);
      if (!Directory.Exists(directory))
      {
        Directory.CreateDirectory(directory);
      }
      while (File.Exists(tempPath))
      {
        tempPath = Path.Combine(Path.GetTempPath(), ArchiveSettings.TempArchivePath, $"{archiveName}_{counter}");
        counter++;
      }

      if (!File.Exists(tempPath) && needCreate)
      {
        using (FileStream fs = File.Create(tempPath))
        {
          LogInformation($"Создан временный файл: {tempPath}");
        }
      }

      _tempPaths.Add(archiveName, tempPath);
      return tempPath;
    }

    /// <summary>
    /// Удаляет временные файлы, созданные при работе с архивом.
    /// </summary>
    /// <param name="archiveName">Название архива.</param>
    /// <param name="tempPath">Временный путь к временному файлу архива.</param>
    private static void RemoveTempFile(string archiveName, string tempPath)
    {
      File.Delete(tempPath);
      _tempPaths.Remove(archiveName);
      LogInformation($"Временные файлы удалены: {tempPath}");
    }

    #endregion

    #region Шифрование архива

    /// <summary>
    /// Процесс шифрования архива.
    /// </summary>
    /// <param name="archivePath">Путь к архиву.</param>
    /// <param name="archiveName">Название архива.</param>
    /// <param name="tempPath">Временный путь для работы с архивом.</param>
    private void EncryptArchiveProcess(string archivePath, string archiveName, string tempPath)
    {
      LogInformation($"Шифрование файла: {archivePath}");
      byte[] fileData = File.ReadAllBytes(tempPath);
      string dataString = Convert.ToBase64String(fileData);
      string encryptedBase64 = FileEncryptionManager.Encrypt(dataString);
      File.WriteAllBytes(archivePath, Convert.FromBase64String(encryptedBase64));
      LogInformation($"Файл {archivePath} успешно зашифрован");
    }

    #endregion

    #region Дешифрование архива

    /// <summary>
    /// Процесс дешифрования архива.
    /// </summary>
    /// <param name="archivePath">Путь к архиву.</param>
    /// <param name="archiveName">Название архива.</param>
    /// <param name="tempPath">Временный путь для работы с архивом.</param>
    private void DecryptArchiveProcess(string archivePath, string archiveName, string tempPath)
    {
      LogInformation($"Расшифровка файла: {archivePath}");
      string decryptedSting = InitializeArchiveDecryption(archivePath);
      byte[] decryptedData = Convert.FromBase64String(decryptedSting);
      File.WriteAllBytes(tempPath, decryptedData);
      LogInformation($"Файл {archivePath} успешно расшифрован");
    }

    /// <summary>
    /// Инициализация расшифровки архива.
    /// </summary>
    /// <param name="encryptedArchivePath">Путь к зашифрованному архиву.</param>
    /// <param name="key">Ключ шифрования архива.</param>
    /// <returns>Расшифрованные данные архива.</returns>
    public static string InitializeArchiveDecryption(string encryptedArchivePath)
    {
      byte[] encryptedData = File.ReadAllBytes(encryptedArchivePath);
      string base64String = Convert.ToBase64String(encryptedData);
      return FileEncryptionManager.Decrypt(base64String);
    }

    #endregion

    #region Конструкторы

    public ArchiveEncryption()
    { }

    #endregion
  }
}
