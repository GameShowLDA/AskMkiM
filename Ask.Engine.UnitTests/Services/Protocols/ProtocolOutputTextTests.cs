using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.FilesUtility;
using Ask.Core.Shared.Metadata.Enums.RoleEnums;

namespace Ask.Engine.UnitTests.Services.Protocols;

public class ProtocolOutputTextTests : IDisposable
{
  private readonly RoleType? _role = RoleAuthorizationConfig.CurrentRole;
  private readonly string _displayName = RoleAuthorizationConfig.CurrentRoleDisplayName;
  private const string Text = "Версия ПО: 1.0\r\nРевизия ПО: abc\r\n\r\nПротокол\r\n\r\nЗаключение: годен\r\nИсполнитель: ____";

  [Theory]
  [InlineData(RoleType.Administrator)]
  [InlineData(RoleType.Adjuster)]
  [InlineData(RoleType.Developer)]
  public void OrdinaryRoles_PrintOnlyProtocolBody(RoleType role)
  {
    RoleAuthorizationConfig.SetCurrentRole(role, role.ToString());
    Assert.Equal(Text[(Text.IndexOf("Протокол", StringComparison.Ordinal))..], ProtocolOutputText.Prepare(Text));
  }

  [Fact]
  public void Root_KeepsOriginalText()
  {
    RoleAuthorizationConfig.SetCurrentRole(RoleType.Root, "root");
    Assert.Equal(Text, ProtocolOutputText.Prepare(Text));
  }

  [Fact]
  public void NoRole_HidesBuildMetadata()
  {
    RoleAuthorizationConfig.Clear();
    Assert.DoesNotContain("Версия ПО:", ProtocolOutputText.Prepare(Text));
    Assert.DoesNotContain("Ревизия ПО:", ProtocolOutputText.Prepare(Text));
  }

  [Fact]
  public void TextWithoutMetadata_PreservesWhitespaceAndLineEndings()
  {
    RoleAuthorizationConfig.Clear();
    const string body = "\nПротокол\n\nВерсия ПО изделия соответствует требованиям\n";
    Assert.Equal(body, ProtocolOutputText.Prepare(body));
  }

  public void Dispose()
  {
    if (_role is RoleType role)
      RoleAuthorizationConfig.SetCurrentRole(role, _displayName);
    else
      RoleAuthorizationConfig.Clear();
  }
}
