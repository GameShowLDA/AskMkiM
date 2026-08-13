using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Engine.Tests.SelfControl;
using Moq;

namespace Ask.Engine.UnitTests.Tests.SelfControl;

public sealed class SelfControlPowerSettingsTests
{
  [Fact]
  public void SystemSelfExecutor_InitializeSettings_EnablesPowerCheck()
  {
    var controller = new Mock<IExecutionController>();
    ActionSettings? settings = null;
    controller
      .Setup(item => item.SetSettings(It.IsAny<ActionSettings>()))
      .Callback<ActionSettings>(value => settings = value);

    new SystemSelfExecutor().InitializeSettings(controller.Object);

    Assert.NotNull(settings);
    Assert.True(settings.CheckPower);
  }

  [Fact]
  public void ModuleSelfExecutor_InitializeSettings_EnablesPowerCheck()
  {
    var controller = new Mock<IExecutionController>();
    ActionSettings? settings = null;
    controller
      .Setup(item => item.SetSettings(It.IsAny<ActionSettings>()))
      .Callback<ActionSettings>(value => settings = value);
    var selectorProvider = new Mock<IDeviceSelectorProvider>();

    new ModuleSelfExecutor(selectorProvider.Object).InitializeSettings(controller.Object);

    Assert.NotNull(settings);
    Assert.True(settings.CheckPower);
  }
}
