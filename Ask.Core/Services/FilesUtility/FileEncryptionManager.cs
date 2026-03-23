using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Ask.Core.Services.FilesUtility
{
  /// <summary>
  /// Статический класс для управления шифрованием и дешифрованием файлов с использованием AES.
  /// </summary>
  static public class FileEncryptionManager
  {
    /// <summary>
    /// Константная строка, используемая для генерации ключа шифрования.
    /// </summary>
    static readonly private string KeyProgramm = "AskMkiM";

    /// <summary>
    /// Массив байтов, сгенерированный из KeyProgramm с использованием SHA-256, служит ключом шифрования.
    /// </summary>
    static private readonly byte[] encryptionKey = GenerateKeyFromString();
    static private readonly string EncryptedFilePrefix = "ASKM_FILE_ENCRYPTED_V1:";
    static private readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    /// <summary>
    /// Генерирует ключ шифрования из строки с использованием SHA-256.
    /// </summary>
    /// <returns>Хеш SHA-256 в виде массива байтов.</returns> 
    static public byte[] GenerateKeyFromString()
    {
      using (SHA256 sha256 = SHA256.Create())
      {
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(KeyProgramm));
      }
    }

    /// <summary>
    /// Генерирует новый уникальный идентификатор (GUID) в виде строки.
    /// </summary>
    /// <returns>Строка GUID.</returns>
    static public string GenerateApiKey()
    {
      return Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Дешифрует зашифрованный текст, закодированный в base64, с использованием AES.
    /// </summary>
    /// <param name="cipherText">Зашифрованный текст в формате base64.</param>
    /// <returns>Расшифрованный текст.</returns>
    static public string Decrypt(string cipherText)
    {
      byte[] fullCipher = Convert.FromBase64String(cipherText);
      byte[] iv = new byte[16];
      byte[] cipher = new byte[fullCipher.Length - iv.Length];

      Array.Copy(fullCipher, iv, iv.Length);
      Array.Copy(fullCipher, iv.Length, cipher, 0, cipher.Length);

      using (Aes aes = Aes.Create())
      {
        aes.Key = encryptionKey;
        ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, iv);

        using (MemoryStream ms = new MemoryStream(cipher))
        {
          using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
          {
            using (StreamReader sr = new StreamReader(cs))
            {
              return sr.ReadToEnd();
            }
          }
        }
      }
    }

    /// <summary>
    /// Шифрует обычный текст с использованием AES и возвращает зашифрованный текст в формате base64.
    /// </summary>
    /// <param name="plainText">Обычный текст для шифрования.</param>
    /// <returns>Зашифрованный текст в формате base64.</returns>
    static public string Encrypt(string plainText)
    {
      using (Aes aes = Aes.Create())
      {
        aes.Key = GenerateKeyFromString();
        aes.GenerateIV();
        ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

        using (MemoryStream ms = new MemoryStream())
        {
          ms.Write(aes.IV, 0, aes.IV.Length);
          using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
          {
            using (StreamWriter sw = new StreamWriter(cs))
            {
              sw.Write(plainText);
            }
          }

          return Convert.ToBase64String(ms.ToArray());
        }
      }
    }

    /// <summary>
    /// Проверяет, зашифрован ли файл в формате FileEncryptionManager.
    /// </summary>
    /// <param name="filePath">Путь к файлу.</param>
    /// <returns>True, если файл зашифрован.</returns>
    static public bool IsFileEncrypted(string filePath)
    {
      string fullPath = ValidateFilePath(filePath);
      byte[] prefixBytes = Encoding.UTF8.GetBytes(EncryptedFilePrefix);
      byte[] utf8Bom = new byte[] { 0xEF, 0xBB, 0xBF };
      byte[] headerBytes = new byte[prefixBytes.Length + utf8Bom.Length];

      using (FileStream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
      {
        int bytesRead = stream.Read(headerBytes, 0, headerBytes.Length);
        if (bytesRead < prefixBytes.Length)
        {
          return false;
        }

        if (MatchesPrefixAtOffset(headerBytes, bytesRead, prefixBytes, offset: 0))
        {
          return true;
        }

        if (bytesRead >= prefixBytes.Length + utf8Bom.Length &&
            headerBytes[0] == utf8Bom[0] &&
            headerBytes[1] == utf8Bom[1] &&
            headerBytes[2] == utf8Bom[2] &&
            MatchesPrefixAtOffset(headerBytes, bytesRead, prefixBytes, offset: utf8Bom.Length))
        {
          return true;
        }
      }

      return false;
    }

    /// <summary>
    /// Шифрует файл и сохраняет результат в этом же файле.
    /// </summary>
    /// <param name="filePath">Путь к исходному файлу.</param>
    static public void EncryptFile(string filePath)
    {
      string fullPath = ValidateFilePath(filePath);
      if (IsFileEncrypted(fullPath))
      {
        return;
      }

      byte[] sourceBytes = File.ReadAllBytes(fullPath);
      string sourceBase64 = Convert.ToBase64String(sourceBytes);
      string cipherText = Encrypt(sourceBase64);

      File.WriteAllText(fullPath, EncryptedFilePrefix + cipherText, Utf8NoBom);
    }

    /// <summary>
    /// Расшифровывает файл, зашифрованный методом EncryptFile, и сохраняет результат в этом же файле.
    /// </summary>
    /// <param name="filePath">Путь к файлу.</param>
    static public void DecryptFile(string filePath)
    {
      string fullPath = ValidateFilePath(filePath);
      if (!IsFileEncrypted(fullPath))
      {
        return;
      }

      string encryptedFileContent = File.ReadAllText(fullPath, Encoding.UTF8);
      if (encryptedFileContent.Length > 0 && encryptedFileContent[0] == '\uFEFF')
      {
        encryptedFileContent = encryptedFileContent[1..];
      }

      if (!encryptedFileContent.StartsWith(EncryptedFilePrefix, StringComparison.Ordinal))
      {
        throw new InvalidDataException("Неподдерживаемый формат зашифрованного файла.");
      }

      string cipherText = encryptedFileContent[EncryptedFilePrefix.Length..].Trim();
      if (string.IsNullOrWhiteSpace(cipherText))
      {
        throw new InvalidDataException("Зашифрованный файл не содержит данных.");
      }

      string decryptedBase64 = Decrypt(cipherText);
      byte[] decryptedBytes = Convert.FromBase64String(decryptedBase64);
      File.WriteAllBytes(fullPath, decryptedBytes);
    }

    /// <summary>
    /// Валидирует путь к файлу.
    /// </summary>
    /// <param name="filePath">Путь к файлу.</param>
    /// <returns>Полный путь к существующему файлу.</returns>
    static private string ValidateFilePath(string filePath)
    {
      if (string.IsNullOrWhiteSpace(filePath))
      {
        throw new ArgumentException("Требуется указать путь к файлу.", nameof(filePath));
      }

      string fullPath = Path.GetFullPath(filePath);
      if (!File.Exists(fullPath))
      {
        throw new FileNotFoundException($"Файл не был найден: {fullPath}", fullPath);
      }

      return fullPath;
    }

    static private bool MatchesPrefixAtOffset(byte[] source, int sourceLength, byte[] prefix, int offset)
    {
      if (offset < 0 || sourceLength - offset < prefix.Length)
      {
        return false;
      }

      for (int i = 0; i < prefix.Length; i++)
      {
        if (source[offset + i] != prefix[i])
        {
          return false;
        }
      }

      return true;
    }
  }
}
