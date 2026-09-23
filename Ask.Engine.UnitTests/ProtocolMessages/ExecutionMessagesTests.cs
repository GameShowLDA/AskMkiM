using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Static.Delays;
using Ask.Protocol.Messages.EntryPoints;
using Moq;

namespace Ask.Engine.UnitTests.ProtocolMessages;

public sealed class ExecutionMessagesTests
{
  [Fact]
  public async Task PublishDelayAsync_FormatsNameAndMilliseconds()
  {
    ShowMessageModel? publishedMessage = null;
    var outputService = new Mock<IMessageOutputService>();
    outputService
      .Setup(service => service.ShowMessageAsync(
        It.IsAny<ShowMessageModel>(),
        It.IsAny<bool>(),
        It.IsAny<bool>(),
        It.IsAny<bool>(),
        It.IsAny<bool>(),
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<int>()))
      .Callback<ShowMessageModel, bool, bool, bool, bool, string, string, int>(
        (message, _, _, _, _, _, _, _) => publishedMessage = message)
      .Returns(Task.CompletedTask);
    var delay = new DelayModel
    {
      Name = "Задержка после испытания напряжения",
      Delay = 100,
    };

    await ExecutionMessages.PublishDelayAsync(delay, outputService.Object);

    Assert.NotNull(publishedMessage);
    Assert.Equal("Задержка после испытания напряжения", publishedMessage.Header);
    Assert.Equal("100мс", publishedMessage.Message);
  }

  [Fact]
  public async Task PublishDelayAsync_NullDelay_ThrowsArgumentNullException()
  {
    await Assert.ThrowsAsync<ArgumentNullException>(
      () => ExecutionMessages.PublishDelayAsync(null!, outputService: null));
  }
}
