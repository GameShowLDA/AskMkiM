using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Protocol.Messages.EntryPoints;
using Moq;

namespace Ask.Engine.UnitTests.ProtocolMessages;

public sealed class ExecutionMessagesTests
{
  [Fact(DisplayName = "Сообщения выполнения: заголовок локализации начинает отдельный блок")]
  public async Task PublishLocalizationHeaderAsync_PublishesBlockHeader()
  {
    ShowMessageModel? publishedMessage = null;
    bool? isBlockStart = null;
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
        (message, blockStart, _, _, _, _, _, _) =>
        {
          publishedMessage = message;
          isBlockStart = blockStart;
        })
      .Returns(Task.CompletedTask);

    await ExecutionMessages.PublishLocalizationHeaderAsync(outputService.Object);

    Assert.NotNull(publishedMessage);
    Assert.Equal("Локализация неисправной цепи", publishedMessage.Header);
    Assert.Equal(1, publishedMessage.IndentLevel);
    Assert.True(isBlockStart);
  }
}
