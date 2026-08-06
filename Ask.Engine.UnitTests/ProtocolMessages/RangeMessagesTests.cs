using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.UnitEnums;
using Ask.Protocol.Messages.EntryPoints;
using Moq;
using System.Globalization;

namespace Ask.Engine.UnitTests.ProtocolMessages;

public sealed class RangeMessagesTests
{
  [Fact]
  public async Task PublishAllowedRangeAsync_FormatsRangeAndUnit()
  {
    CultureInfo previousCulture = CultureInfo.CurrentCulture;
    try
    {
      CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
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

      await RangeMessages.PublishAllowedRangeAsync(
        ResistanceUnit.Ohm,
        new MeasurementRange(10, 9.5, 10.5),
        outputService.Object,
        indentLevel: 2);

      Assert.NotNull(publishedMessage);
      Assert.Equal("Диапазон допускаемых значений", publishedMessage.Header);
      Assert.Equal("от 9,5 до 10,5 Ом", publishedMessage.Message);
      Assert.Equal(2, publishedMessage.IndentLevel);
    }
    finally
    {
      CultureInfo.CurrentCulture = previousCulture;
    }
  }
}
