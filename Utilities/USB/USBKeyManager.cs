using System.IO;
using System.Management;
using Utilities.Encrypter;

namespace Utilities.USB
{
  public class USBKeyManager
  {
    public void CreateKeyFile(DriveInfo drive)
    {
      string apiKey = FileEncryptionManager.GenerateApiKey();
      string deviceId = GetDeviceId(drive);

      string data = $"{apiKey}:{deviceId}";
      string encryptedData = FileEncryptionManager.Encrypt(data);

      string filePath = Path.Combine(drive.RootDirectory.FullName, "usbkey.dat");
      File.WriteAllText(filePath, encryptedData);

      Console.WriteLine($"Ключ создан на флешке: {drive.Name}");
    }

    public string GetDeviceId(DriveInfo drive)
    {
      string deviceId = string.Empty;
      try
      {
        string query = "SELECT * FROM Win32_DiskDrive WHERE MediaType='Removable Media'";
        using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
        {
          foreach (ManagementObject disk in searcher.Get())
          {
            foreach (ManagementObject partition in disk.GetRelated("Win32_DiskPartition"))
            {
              foreach (ManagementObject logicalDisk in partition.GetRelated("Win32_LogicalDisk"))
              {
                if (logicalDisk["DeviceID"].ToString() == drive.Name.TrimEnd('\\'))
                {
                  deviceId = disk["SerialNumber"]?.ToString().Trim();
                  break;
                }
              }
            }
          }
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Ошибка при получении идентификатора устройства: {ex.Message}");
      }
      return deviceId;
    }
  }
}
