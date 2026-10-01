using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Metadata.Enums.RoleEnums;
using System.Text.Json.Nodes;

namespace Ask.Engine.UnitTests.Services.Devices;

public sealed class RoleCredentialFileServiceTests : IDisposable
{
  private readonly string _directory = Path.Combine(Path.GetTempPath(), "AskRoleLoginTests", Guid.NewGuid().ToString("N"));
  private string FilePath => Path.Combine(_directory, "role-auth.json");

  [Theory]
  [InlineData(RoleType.Administrator, "admin", "test")]
  [InlineData(RoleType.Adjuster, "adjuster", "test")]
  [InlineData(RoleType.Developer, "developer", "test")]
  [InlineData(RoleType.Root, "root", "root")]
  public async Task AuthorizeAsync_RequiresMatchingRoleAndPassword(RoleType role, string login, string password)
  {
    var service = new RoleCredentialFileService(FilePath);

    Assert.Null(await service.AuthorizeAsync(role, "wrong"));
    var result = await service.AuthorizeAsync(role, password);

    Assert.NotNull(result);
    Assert.Equal(role, result.Role);
    Assert.Equal(login, result.Login);
  }

  [Fact]
  public async Task LoadLegacyFile_AddsLoginsWithoutChangingPasswordHashes()
  {
    var service = new RoleCredentialFileService(FilePath);
    await service.GetManageableRolesAsync();
    var store = JsonNode.Parse(await File.ReadAllTextAsync(FilePath))!;
    var roles = store["Roles"]!.AsArray();
    var hashes = roles.Select(role => role!["PasswordHash"]!.ToString()).ToArray();
    var salts = roles.Select(role => role!["PasswordSalt"]!.ToString()).ToArray();
    foreach (var role in roles)
    {
      role!.AsObject().Remove("Login");
    }
    await File.WriteAllTextAsync(FilePath, store.ToJsonString());

    var normalized = await service.GetManageableRolesAsync();

    Assert.Equal(hashes, normalized.Select(role => role.PasswordHash));
    Assert.Equal(salts, normalized.Select(role => role.PasswordSalt));
    Assert.Equal(new[] { "admin", "adjuster", "developer", "root" }, normalized.Select(role => role.Login));
    Assert.NotNull(await service.AuthorizeAsync(RoleType.Administrator, "test"));
    var persisted = JsonNode.Parse(await File.ReadAllTextAsync(FilePath))!;
    Assert.All(persisted["Roles"]!.AsArray(), role => Assert.False(string.IsNullOrWhiteSpace(role!["Login"]?.ToString())));
  }

  public void Dispose()
  {
    if (Directory.Exists(_directory))
    {
      Directory.Delete(_directory, recursive: true);
    }
  }
}
