using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.Metadata.Enums.RoleEnums;

namespace Ask.Engine.UnitTests.Services.Config;

public sealed class DebugAccessConfigTests : IDisposable
{
  public DebugAccessConfigTests()
  {
    RoleAuthorizationConfig.Clear();
  }

  [Theory]
  [InlineData(RoleType.Administrator)]
  [InlineData(RoleType.Adjuster)]
  [InlineData(RoleType.Developer)]
  public void SetCurrentRole_OrdinaryRole_DisablesDebug(RoleType role)
  {
    RoleAuthorizationConfig.SetCurrentRole(role, role.ToString());

    Assert.False(DebugAccessConfig.IsDebugEnabled);
  }

  [Fact]
  public void SetCurrentRole_Root_EnablesDebug()
  {
    RoleAuthorizationConfig.SetCurrentRole(RoleType.Root, "root");

    Assert.True(DebugAccessConfig.IsDebugEnabled);
  }

  [Fact]
  public void SetCurrentRole_RootThenOrdinary_DisablesDebug()
  {
    RoleAuthorizationConfig.SetCurrentRole(RoleType.Root, "root");
    RoleAuthorizationConfig.SetCurrentRole(RoleType.Adjuster, "adjuster");

    Assert.False(DebugAccessConfig.IsDebugEnabled);
  }

  [Fact]
  public void SetCurrentRole_OrdinaryThenRoot_EnablesDebug()
  {
    RoleAuthorizationConfig.SetCurrentRole(RoleType.Adjuster, "adjuster");
    RoleAuthorizationConfig.SetCurrentRole(RoleType.Root, "root");

    Assert.True(DebugAccessConfig.IsDebugEnabled);
  }

  [Fact]
  public void Clear_RootSession_DisablesDebugAndPublishesChange()
  {
    var publishedStates = new List<bool>();
    Action<SystemStateEvents.DebugRightsChanged> handler = e => publishedStates.Add(e.IsDebugEnabled);
    EventAggregator.Subscribe(handler);

    try
    {
      RoleAuthorizationConfig.SetCurrentRole(RoleType.Root, "root");
      RoleAuthorizationConfig.Clear();

      Assert.False(DebugAccessConfig.IsDebugEnabled);
      Assert.Equal([true, false], publishedStates);
    }
    finally
    {
      EventAggregator.Unsubscribe(handler);
    }
  }

  public void Dispose()
  {
    RoleAuthorizationConfig.Clear();
  }
}
