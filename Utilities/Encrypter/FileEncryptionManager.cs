using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Utilities.Encrypter
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
  }
}
