using System.IO;
using Utilities.Encrypter;

namespace Utilities.USB
{
  public class USBKeyValidator
  {
    private readonly USBKeyManager usbKeyManager;

    public USBKeyValidator()
    {
      usbKeyManager = new USBKeyManager();
    }

    public bool IsValidUSBKey()
    {
      var drives = DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Removable);
      foreach (var drive in drives)
      {
        if (ValidateKeyFile(drive))
        {
          return true;
        }
      }
      // TODO : заменить на false
      return true;
    }

    private bool ValidateKeyFile(DriveInfo drive)
    {
      string filePath = Path.Combine(drive.RootDirectory.FullName, "usbkey.dat");
      if (!File.Exists(filePath))
      {
        return false;
      }

      string encryptedData = File.ReadAllText(filePath);
      string decryptedData = FileEncryptionManager.Decrypt(encryptedData);

      // Предположим, что формат данных: "apiKey:deviceId"
      var parts = decryptedData.Split(':');
      if (parts.Length != 2)
      {
        return false;
      }

      string apiKey = parts[0];
      string deviceId = parts[1];

      // Проверяем, что идентификатор устройства совпадает
      string currentDeviceId = usbKeyManager.GetDeviceId(drive);
      return !string.IsNullOrEmpty(apiKey) && deviceId == currentDeviceId;
    }
  }
}
