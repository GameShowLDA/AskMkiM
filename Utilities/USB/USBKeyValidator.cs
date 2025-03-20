using System.IO;
using Utilities.Encrypter;

namespace Utilities.USB
{
  /// <summary>
  /// Класс для проверки валидности USB-ключа.
  /// </summary>
  public class USBKeyValidator
  {
    private readonly USBKeyManager _usbKeyManager;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="USBKeyValidator"/>.
    /// </summary>
    public USBKeyValidator()
    {
      _usbKeyManager = new USBKeyManager();
    }

    /// <summary>
    /// Проверяет наличие и валидность USB-ключа.
    /// </summary>
    /// <returns>Возвращает true, если найден валидный USB-ключ, иначе false.</returns>
    public bool IsValidUSBKey()
    {
      var removableDrives = GetRemovableDrives();
      return removableDrives.Any(ValidateKeyFile);
    }

    /// <summary>
    /// Получает список всех подключенных съёмных дисков.
    /// </summary>
    /// <returns>Коллекция <see cref="DriveInfo"/> для всех доступных съемных дисков.</returns>
    private static IEnumerable<DriveInfo> GetRemovableDrives()
    {
      return DriveInfo.GetDrives().Where(drive => drive.DriveType == DriveType.Removable);
    }

    /// <summary>
    /// Проверяет валидность ключевого файла на указанном диске.
    /// </summary>
    /// <param name="drive">Объект <see cref="DriveInfo"/>, представляющий съемный диск.</param>
    /// <returns>True, если ключевой файл найден и его данные валидны, иначе false.</returns>
    private bool ValidateKeyFile(DriveInfo drive)
    {
      string filePath = GetKeyFilePath(drive);
      if (!File.Exists(filePath))
      {
        return false;
      }

      string decryptedData = DecryptKeyFile(filePath);
      if (string.IsNullOrEmpty(decryptedData))
      {
        return false;
      }

      return IsKeyValid(decryptedData, drive);
    }

    /// <summary>
    /// Формирует путь к ключевому файлу на указанном диске.
    /// </summary>
    /// <param name="drive">Объект <see cref="DriveInfo"/>, представляющий съемный диск.</param>
    /// <returns>Полный путь к файлу ключа.</returns>
    private static string GetKeyFilePath(DriveInfo drive)
    {
      return Path.Combine(drive.RootDirectory.FullName, "usbkey.dat");
    }

    /// <summary>
    /// Расшифровывает данные из ключевого файла.
    /// </summary>
    /// <param name="filePath">Полный путь к ключевому файлу.</param>
    /// <returns>Расшифрованные данные или пустую строку, если данные некорректны.</returns>
    private static string DecryptKeyFile(string filePath)
    {
      try
      {
        string encryptedData = File.ReadAllText(filePath);
        return FileEncryptionManager.Decrypt(encryptedData);
      }
      catch (Exception)
      {
        return string.Empty;
      }
    }

    /// <summary>
    /// Проверяет валидность расшифрованных данных ключа.
    /// </summary>
    /// <param name="decryptedData">Расшифрованные данные ключа.</param>
    /// <param name="drive">Объект <see cref="DriveInfo"/>, представляющий съемный диск.</param>
    /// <returns>True, если ключ валиден, иначе false.</returns>
    private bool IsKeyValid(string decryptedData, DriveInfo drive)
    {
      string[] parts = decryptedData.Split(':');
      if (parts.Length != 2)
      {
        return false;
      }

      string apiKey = parts[0];
      string deviceId = parts[1];
      string currentDeviceId = _usbKeyManager.GetDeviceId(drive);

      return !string.IsNullOrEmpty(apiKey) && deviceId == currentDeviceId;
    }
  }
}
