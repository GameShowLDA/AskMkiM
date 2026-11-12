using System.Security.Cryptography;

namespace AskMkiM.Updater.Core;

/// <summary>
/// Вычисляет контрольные суммы файлов обновления.
/// </summary>
public static class HashService
{
  /// <summary>Вычисляет SHA-256 файла в шестнадцатеричном представлении.</summary>
  public static string ComputeSha256(string filePath)
  {
    using FileStream stream = File.OpenRead(filePath);
    return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
  }
}
