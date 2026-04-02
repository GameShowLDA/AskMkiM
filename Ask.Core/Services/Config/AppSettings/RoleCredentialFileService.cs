using Ask.Core.Shared.Entity.Settings;
using Ask.Core.Shared.Metadata.Enums.RoleEnums;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace Ask.Core.Services.Config.AppSettings
{
  /// <summary>
  /// Local file-based credential storage for application roles.
  /// </summary>
  public sealed class RoleCredentialFileService
  {
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
      WriteIndented = true,
    };

    private static readonly SemaphoreSlim SyncRoot = new(1, 1);
    private readonly string _filePath;

    public RoleCredentialFileService(string? filePath = null)
    {
      _filePath = filePath ?? GetDefaultFilePath();
    }

    /// <summary>
    /// Returns the list of available roles.
    /// </summary>
    public async Task<IReadOnlyList<RoleCredentialModel>> GetRolesAsync()
    {
      var store = await ReadStoreAsync();
      return store.Roles
        .OrderBy(x => x.Role)
        .ToList();
    }

    /// <summary>
    /// Returns the last successfully selected role.
    /// </summary>
    public async Task<RoleType?> GetLastSelectedRoleAsync()
    {
      var store = await ReadStoreAsync();
      return store.LastSelectedRole;
    }

    /// <summary>
    /// Verifies the password for the selected role and stores it as the last selected role.
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

      RoleCredentialsStoreFileModel? fileStore;
      await using (var stream = File.OpenRead(_filePath))
      {
        fileStore = await JsonSerializer.DeserializeAsync<RoleCredentialsStoreFileModel>(stream, JsonOptions);
      }

      var normalizedStore = NormalizeStore(fileStore);
      var normalizedFileStore = ToFileStore(normalizedStore);

      var sourceSnapshot = JsonSerializer.Serialize(fileStore ?? new RoleCredentialsStoreFileModel(), JsonOptions);
      var normalizedSnapshot = JsonSerializer.Serialize(normalizedFileStore, JsonOptions);

      if (!string.Equals(sourceSnapshot, normalizedSnapshot, StringComparison.Ordinal))
      {
        await SaveStoreInternalAsync(normalizedStore);
      }

      return normalizedStore;
    }

    private async Task SaveStoreInternalAsync(RoleCredentialsStoreModel store)
    {
      var directory = Path.GetDirectoryName(_filePath);
      if (!string.IsNullOrWhiteSpace(directory))
      {
        Directory.CreateDirectory(directory);
      }

      await using var stream = File.Create(_filePath);
      await JsonSerializer.SerializeAsync(stream, ToFileStore(store), JsonOptions);
    }

    private static RoleCredentialsStoreModel CreateDefaultStore()
    {
      return new RoleCredentialsStoreModel
      {
        LastSelectedRole = RoleType.Administrator,
        Roles =
        {
          CreateCredential(RoleType.Administrator, "Администратор", "test"),
          CreateCredential(RoleType.Adjuster, "Регулировщик", "test"),
          CreateCredential(RoleType.Developer, "Разработчик", "test"),
        },
      };
    }

    private static RoleCredentialsStoreModel NormalizeStore(RoleCredentialsStoreFileModel? fileStore)
    {
      if (fileStore == null)
      {
        return CreateDefaultStore();
      }

      var roles = fileStore.Roles ?? new List<RoleCredentialFileModel>();

      var administrator = CreateNormalizedCredential(
        RoleType.Administrator,
        "Администратор",
        FindFirstRole(roles, "Administrator"));

      var adjuster = CreateNormalizedCredential(
        RoleType.Adjuster,
        "Регулировщик",
        FindFirstRole(roles, "Adjuster", "Metrology", "SystemMaintenance"));

      var developer = CreateNormalizedCredential(
        RoleType.Developer,
        "Разработчик",
        FindFirstRole(roles, "Developer"));

      return new RoleCredentialsStoreModel
      {
        LastSelectedRole = NormalizeRoleName(fileStore.LastSelectedRole) ?? RoleType.Administrator,
        Roles =
        {
          administrator,
          adjuster,
          developer,
        },
      };
    }

    private static RoleCredentialModel CreateNormalizedCredential(
      RoleType role,
      string displayName,
      RoleCredentialFileModel? persistedRole)
    {
      if (persistedRole == null
          || string.IsNullOrWhiteSpace(persistedRole.PasswordHash)
          || string.IsNullOrWhiteSpace(persistedRole.PasswordSalt))
      {
        return CreateCredential(role, displayName, "test");
      }

      return new RoleCredentialModel
      {
        Role = role,
        DisplayName = displayName,
        PasswordHash = persistedRole.PasswordHash,
        PasswordSalt = persistedRole.PasswordSalt,
      };
    }

    private static RoleCredentialFileModel? FindFirstRole(
      IEnumerable<RoleCredentialFileModel> roles,
      params string[] roleNames)
    {
      foreach (var roleName in roleNames)
      {
        var role = roles.FirstOrDefault(x => string.Equals(x.Role, roleName, StringComparison.Ordinal));
        if (role != null)
        {
          return role;
        }
      }

      return null;
    }

    private static RoleType? NormalizeRoleName(string? roleName) => roleName switch
    {
      "Administrator" => RoleType.Administrator,
      "Adjuster" => RoleType.Adjuster,
      "Metrology" => RoleType.Adjuster,
      "SystemMaintenance" => RoleType.Adjuster,
      "Developer" => RoleType.Developer,
      _ => null,
    };

    private static RoleCredentialsStoreFileModel ToFileStore(RoleCredentialsStoreModel store)
    {
      return new RoleCredentialsStoreFileModel
      {
        LastSelectedRole = store.LastSelectedRole.HasValue
          ? GetRoleName(store.LastSelectedRole.Value)
          : null,
        Roles = store.Roles
          .OrderBy(x => x.Role)
          .Select(x => new RoleCredentialFileModel
          {
            Role = GetRoleName(x.Role),
            DisplayName = x.DisplayName,
            PasswordHash = x.PasswordHash,
            PasswordSalt = x.PasswordSalt,
          })
          .ToList(),
      };
    }

    private static string GetRoleName(RoleType role) => role switch
    {
      RoleType.Administrator => "Administrator",
      RoleType.Adjuster => "Adjuster",
      RoleType.Developer => "Developer",
      _ => throw new ArgumentOutOfRangeException(nameof(role), role, null),
    };

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

    private sealed class RoleCredentialsStoreFileModel
    {
      public string? LastSelectedRole { get; set; }

      public List<RoleCredentialFileModel> Roles { get; set; } = new();
    }

    private sealed class RoleCredentialFileModel
    {
      public string Role { get; set; } = string.Empty;

      public string DisplayName { get; set; } = string.Empty;

      public string PasswordHash { get; set; } = string.Empty;

      public string PasswordSalt { get; set; } = string.Empty;
    }
  }
}
