using Ask.Core.Services.Devices;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.UiEnums;
using Moq;

namespace Ask.Engine.UnitTests.Services.Devices;

public sealed class DeviceResetServiceTests
{
  [Fact]
  public async Task ResetDevicesAsync_ResetsEveryDeviceAndShowsSuccess()
  {
    var first = CreateDevice(1, true);
    var second = CreateDevice(2, true);
    var interaction = CreateInteractionService();

    await DeviceResetService.ResetDevicesAsync(
      [first.Device.Object, second.Device.Object],
      interaction.Object);

    first.Connectable.Verify(x => x.ResetAsync(null), Times.Once);
    second.Connectable.Verify(x => x.ResetAsync(null), Times.Once);
    interaction.Verify(x => x.WaitRetryOrContinueAsync(), Times.Never);
    interaction.Verify(
      x => x.ShowMessageAsync(
        It.Is<ShowMessageModel>(message =>
          message.Status == ShowMessageModel.MessageType.Success
          && message.IndentLevel == 1
          && message.Message == "Сброс устройства"),
        false,
        false,
        true,
        false,
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<int>()),
      Times.Exactly(2));
  }

  [Fact]
  public async Task ResetDevicesAsync_RetriesCurrentDeviceAndThenContinuesList()
  {
    var first = CreateDevice(1, false, true);
    var second = CreateDevice(2, true);
    var interaction = CreateInteractionService(UserAction.Retry);

    await DeviceResetService.ResetDevicesAsync(
      [first.Device.Object, second.Device.Object],
      interaction.Object);

    first.Connectable.Verify(x => x.ResetAsync(null), Times.Exactly(2));
    second.Connectable.Verify(x => x.ResetAsync(null), Times.Once);
    interaction.Verify(x => x.WaitRetryOrContinueAsync(), Times.Once);
  }

  [Fact]
  public async Task ResetDevicesAsync_ContinuesWithNextDeviceAfterFailure()
  {
    var first = CreateDevice(1, false);
    var second = CreateDevice(2, true);
    var interaction = CreateInteractionService(UserAction.Continue);

    await DeviceResetService.ResetDevicesAsync(
      [first.Device.Object, second.Device.Object],
      interaction.Object);

    first.Connectable.Verify(x => x.ResetAsync(null), Times.Once);
    second.Connectable.Verify(x => x.ResetAsync(null), Times.Once);
    interaction.Verify(x => x.WaitRetryOrContinueAsync(), Times.Once);
  }

  [Fact]
  public async Task ResetDevicesAsync_ResetsDuplicateDeviceOnlyOnce()
  {
    var device = CreateDevice(1, true);

    await DeviceResetService.ResetDevicesAsync(
      [device.Device.Object, device.Device.Object]);

    device.Connectable.Verify(x => x.ResetAsync(null), Times.Once);
  }

  [Fact]
  public async Task ResetDevicesAsync_ShowsBlankLineAndCompletionHeaderBeforeReset()
  {
    var device = CreateDevice(1, true);
    var interaction = CreateInteractionService();

    await DeviceResetService.ResetDevicesAsync(
      [device.Device.Object],
      interaction.Object,
      showTestCompletionHeader: true);

    var output = interaction.Invocations
      .Where(invocation =>
        invocation.Method.Name is nameof(IUserInteractionService.AppendEmptyLineAsync)
          or nameof(IUserInteractionService.ShowMessageAsync))
      .Select(invocation =>
        invocation.Method.Name == nameof(IUserInteractionService.AppendEmptyLineAsync)
          ? "<пустая строка>"
          : ((ShowMessageModel)invocation.Arguments[0]).Header)
      .ToArray();

    Assert.Equal(
      ["<пустая строка>", "Завершение теста", "Устройство 1(1)"],
      output);
  }

  private static (
    Mock<IDevice> Device,
    Mock<IConnectable> Connectable) CreateDevice(
      int number,
      params bool[] resetResults)
  {
    var results = new Queue<bool>(resetResults);
    var connectable = new Mock<IConnectable>();
    connectable
      .Setup(x => x.ResetAsync(null))
      .ReturnsAsync(() => results.Dequeue());

    var device = new Mock<IDevice>();
    device.SetupProperty(x => x.Number, number);
    device.SetupProperty(x => x.Name, $"Устройство {number}");
    device.SetupProperty(x => x.ConnectionDetails, $"192.168.1.{number}");
    device.SetupGet(x => x.DeviceType).Returns(DeviceType.Unknown);
    device.SetupProperty(x => x.ConnectableManager, connectable.Object);

    return (device, connectable);
  }

  private static Mock<IUserInteractionService> CreateInteractionService(
    params UserAction[] actions)
  {
    var interaction = new Mock<IUserInteractionService>();
    var buttonService = new Mock<IButtonService>();
    var queue = new Queue<UserAction>(actions);

    interaction.SetupProperty(x => x.ButtonService, buttonService.Object);
    interaction
      .Setup(x => x.WaitRetryOrContinueAsync())
      .ReturnsAsync(() => queue.Dequeue());
    interaction
      .Setup(x => x.ShowMessageAsync(
        It.IsAny<ShowMessageModel>(),
        It.IsAny<bool>(),
        It.IsAny<bool>(),
        It.IsAny<bool>(),
        It.IsAny<bool>(),
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<int>()))
      .Returns(Task.CompletedTask);
    interaction
      .Setup(x => x.AppendEmptyLineAsync(It.IsAny<int>()))
      .Returns(Task.CompletedTask);

    return interaction;
  }
}
