using Ask.Core.Shared.Entity.Settings;
using Ask.Core.Shared.Metadata.Enums.RoleEnums;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;

namespace Ask.Core.Services.Config.AppSettings
{
  /// <summary>
  /// Локальное файловое хранилище учетных данных ролей.
  /// </summary>
  public sealed class RoleCredentialFileService
  {
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
      WriteIndented = true,
      Converters =
      {
        new JsonStringEnumConverter(),
      },
    };

    private static readonly SemaphoreSlim SyncRoot = new(1, 1);
    private readonly string _filePath;

    public RoleCredentialFileService(string? filePath = null)
    {
      _filePath = filePath ?? GetDefaultFilePath();
    }

    /// <summary>
    /// Возвращает список доступных ролей.
    /// </summary>
    public async Task<IReadOnlyList<RoleCredentialModel>> GetRolesAsync()
    {
      var store = await ReadStoreAsync();
      return store.Roles
        .OrderBy(x => x.Role)
        .ToList();
    }

    /// <summary>
    /// Возвращает последнюю успешно выбранную роль.
    /// </summary>
    public async Task<RoleType?> GetLastSelectedRoleAsync()
    {
      var store = await ReadStoreAsync();
      return store.LastSelectedRole;
    }

    /// <summary>
    /// Проверяет пароль роли и сохраняет её как последнюю выбранную.
    /// </summary>
    public async Task<RoleCredentialModel?> AuthorizeAsync(RoleType role, string password)
    {
      await SyncRoot.WaitAsync();
      try
      {
        var store = await LoadStoreInternalAsync();
        var roleCredential = store.Roles.FirstOrDefault(x => x.Role == role);
        if (roleCredential == null || !VerifyPassword(password, roleCredential))
        {
          return null;
        }

        store.LastSelectedRole = role;
        await SaveStoreInternalAsync(store);
        return roleCredential;
      }
      finally
      {
        SyncRoot.Release();
      }
    }

    private static string GetDefaultFilePath()
    {
      return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "role-auth.json");
    }

    private async Task<RoleCredentialsStoreModel> ReadStoreAsync()
    {
      await SyncRoot.WaitAsync();
      try
      {
        return await LoadStoreInternalAsync();
      }
      finally
      {
        SyncRoot.Release();
      }
    }

    private async Task<RoleCredentialsStoreModel> LoadStoreInternalAsync()
    {
      if (!File.Exists(_filePath))
      {
        var defaultStore = CreateDefaultStore();
        await SaveStoreInternalAsync(defaultStore);
        return defaultStore;
      }

      await using var stream = File.OpenRead(_filePath);
      var store = await JsonSerializer.DeserializeAsync<RoleCredentialsStoreModel>(stream, JsonOptions);

      if (store == null || store.Roles.Count == 0)
      {
        store = CreateDefaultStore();
        await SaveStoreInternalAsync(store);
      }

      return store;
    }

    private async Task SaveStoreInternalAsync(RoleCredentialsStoreModel store)
    {
      var directory = Path.GetDirectoryName(_filePath);
      if (!string.IsNullOrWhiteSpace(directory))
      {
        Directory.CreateDirectory(directory);
      }

      await using var stream = File.Create(_filePath);
      await JsonSerializer.SerializeAsync(stream, store, JsonOptions);
    }

    private static RoleCredentialsStoreModel CreateDefaultStore()
    {
      return new RoleCredentialsStoreModel
      {
        LastSelectedRole = RoleType.Administrator,
        Roles =
        {
          CreateCredential(RoleType.Administrator, "Администратор", "test"),
          CreateCredential(RoleType.Metrology, "Метрология", "test"),
          CreateCredential(RoleType.SystemMaintenance, "Обслуживание системы", "test"),
          CreateCredential(RoleType.Developer, "Разработчик", "test"),
        },
      };
    }

    private static RoleCredentialModel CreateCredential(RoleType role, string displayName, string password)
    {
      var saltBytes = RandomNumberGenerator.GetBytes(SaltSize);
      var hashBytes = HashPassword(password, saltBytes);

      return new RoleCredentialModel
      {
        Role = role,
        DisplayName = displayName,
        PasswordSalt = Convert.ToBase64String(saltBytes),
        PasswordHash = Convert.ToBase64String(hashBytes),
      };
    }

    private static bool VerifyPassword(string password, RoleCredentialModel credential)
    {
      if (string.IsNullOrWhiteSpace(credential.PasswordSalt) || string.IsNullOrWhiteSpace(credential.PasswordHash))
      {
        return false;
      }

      byte[] saltBytes;
      byte[] expectedHashBytes;

      try
      {
        saltBytes = Convert.FromBase64String(credential.PasswordSalt);
        expectedHashBytes = Convert.FromBase64String(credential.PasswordHash);
      }
      catch (FormatException)
      {
        return false;
      }

      var actualHashBytes = HashPassword(password, saltBytes);
      return CryptographicOperations.FixedTimeEquals(actualHashBytes, expectedHashBytes);
    }

    private static byte[] HashPassword(string password, byte[] saltBytes)
    {
      return Rfc2898DeriveBytes.Pbkdf2(
        password,
        saltBytes,
        Iterations,
        HashAlgorithmName.SHA256,
        HashSize);
    }
  }
}
