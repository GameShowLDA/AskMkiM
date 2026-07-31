using Ask.Core.Services.Devices;
using Ask.Core.Services.UI;
using Ask.Core.Services.Config.AppSettings;
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

    var completionHeader = interaction.Invocations
      .Where(invocation => invocation.Method.Name == nameof(IUserInteractionService.ShowMessageAsync))
      .Select(invocation => (ShowMessageModel)invocation.Arguments[0])
      .Single(message => message.Header == "Завершение теста");

    Assert.Equal(ShowMessageModel.MessageType.Command, completionHeader.Status);
  }

  [Fact]
  public async Task ResetDevicesAsync_MandatoryFinalizationContinuesWithoutInteraction()
  {
    var first = CreateDevice(1, false);
    var second = CreateDevice(2, false);
    var third = CreateDevice(3, true);
    var interaction = CreateInteractionService();

    using (EquipmentExecutionContext.EnterMandatoryFinalization())
    {
      await DeviceResetService.ResetDevicesAsync(
        [first.Device.Object, second.Device.Object, third.Device.Object],
        interaction.Object);
    }

    first.Connectable.Verify(x => x.ResetAsync(null), Times.Once);
    second.Connectable.Verify(x => x.ResetAsync(null), Times.Once);
    third.Connectable.Verify(x => x.ResetAsync(null), Times.Once);
    interaction.Verify(x => x.WaitRetryOrContinueAsync(), Times.Never);
    interaction.Verify(x => x.WaitUserActionAsync(
      It.IsAny<bool>(),
      It.IsAny<bool>(),
      It.IsAny<bool>()), Times.Never);
  }

  [Fact]
  public async Task ResetDevicesAsync_MandatoryFinalizationIsolatesResetExceptions()
  {
    var first = CreateThrowingDevice(1);
    var second = CreateThrowingDevice(2);
    var third = CreateDevice(3, true);

    using (EquipmentExecutionContext.EnterMandatoryFinalization())
    {
      await DeviceResetService.ResetDevicesAsync(
        [first.Device.Object, second.Device.Object, third.Device.Object]);
    }

    first.Connectable.Verify(x => x.ResetAsync(null), Times.Once);
    second.Connectable.Verify(x => x.ResetAsync(null), Times.Once);
    third.Connectable.Verify(x => x.ResetAsync(null), Times.Once);
  }

  [Fact]
  public async Task ResetDevicesAsync_MandatoryFinalizationIgnoresCanceledToken()
  {
    var device = CreateDevice(1, true);
    using var cancellation = new CancellationTokenSource();
    cancellation.Cancel();

    using (EquipmentExecutionContext.EnterMandatoryFinalization())
    {
      await DeviceResetService.ResetDevicesAsync(
        [device.Device.Object],
        cancellationToken: cancellation.Token);
    }

    device.Connectable.Verify(x => x.ResetAsync(null), Times.Once);
  }

  [Fact]
  public async Task ResetDevicesAsync_FinalResetRepeatsEarlierAlgorithmReset()
  {
    var device = CreateDevice(1, true, true);

    await DeviceResetService.ResetDevicesAsync([device.Device.Object]);

    using (EquipmentExecutionContext.EnterMandatoryFinalization())
    {
      await DeviceResetService.ResetDevicesAsync([device.Device.Object]);
    }

    device.Connectable.Verify(x => x.ResetAsync(null), Times.Exactly(2));
  }

  [Fact]
  public async Task ResetDevicesAsync_IdleMandatoryFinalizationStillResetsDevice()
  {
    bool originalIdleMode = ExecutionConfig.GetIsIdleModeEnabled();
    var device = CreateDevice(1, true);

    try
    {
      ExecutionConfig.SetIdleMode(true);
      using (EquipmentExecutionContext.EnterMandatoryFinalization())
      {
        await DeviceResetService.ResetDevicesAsync([device.Device.Object]);
      }
    }
    finally
    {
      ExecutionConfig.SetIdleMode(originalIdleMode);
    }

    device.Connectable.Verify(x => x.ResetAsync(null), Times.Once);
  }

  [Fact]
  public async Task ResetDevicesAsync_MandatoryFinalizationContinuesAfterProtocolOutputFailure()
  {
    var first = CreateDevice(1, true);
    var second = CreateDevice(2, true);
    var interaction = CreateInteractionService();
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
      .ThrowsAsync(new InvalidOperationException("Ошибка протокола"));

    using (EquipmentExecutionContext.EnterMandatoryFinalization())
    {
      await DeviceResetService.ResetDevicesAsync(
        [first.Device.Object, second.Device.Object],
        interaction.Object,
        showTestCompletionHeader: true);
    }

    first.Connectable.Verify(x => x.ResetAsync(null), Times.Once);
    second.Connectable.Verify(x => x.ResetAsync(null), Times.Once);
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

  private static (
    Mock<IDevice> Device,
    Mock<IConnectable> Connectable) CreateThrowingDevice(int number)
  {
    var result = CreateDevice(number, true);
    result.Connectable
      .Setup(x => x.ResetAsync(null))
      .ThrowsAsync(new InvalidOperationException($"Ошибка {number}"));
    return result;
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
