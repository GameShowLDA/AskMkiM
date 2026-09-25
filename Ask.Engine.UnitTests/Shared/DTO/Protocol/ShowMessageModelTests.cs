using Ask.Core.Shared.DTO.Protocol;

namespace Ask.Engine.UnitTests.Shared.DTO.Protocol;

public sealed class ShowMessageModelTests
{
  [Theory]
  [InlineData(ShowMessageModel.MessageType.Success, false, "[ОК]")]
  [InlineData(ShowMessageModel.MessageType.Error, false, "[ERR]")]
  [InlineData(ShowMessageModel.MessageType.Success, true, "[НОРМА]")]
  [InlineData(ShowMessageModel.MessageType.Error, true, "[БРАК]")]
  public void GetQualityPrefix_UsesResultKindOfCurrentMessage(
    ShowMessageModel.MessageType status,
    bool isMeasurement,
    string expected)
  {
    var message = new ShowMessageModel
    {
      Status = status,
      IsMeasurement = isMeasurement,
    };

    Assert.Equal(expected, message.GetQualityPrefix());
  }

  [Fact]
  public void GetQualityPrefix_DoesNotDependOnOtherMessages()
  {
    var equipmentMessage = new ShowMessageModel
    {
      Status = ShowMessageModel.MessageType.Success,
    };
    var measurementMessage = new ShowMessageModel
    {
      Status = ShowMessageModel.MessageType.Error,
      IsMeasurement = true,
    };

    Assert.Equal("[ОК]", equipmentMessage.GetQualityPrefix());
    Assert.Equal("[БРАК]", measurementMessage.GetQualityPrefix());
  }
}
