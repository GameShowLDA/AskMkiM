using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Protocol.Messages.EntryPoints;
using Ask.Protocol.Messages.Models;
using Ask.Protocol.Messages.Show;
using Moq;
using System.Windows.Media;

namespace Ask.Engine.UnitTests.ProtocolMessages;

public sealed class MeasurementMessagesTests
{
  [Fact]
  public async Task PublishResultAsync_MetrologyAndResultsHidden_PublishesMessage()
  {
    var outputService = CreateOutputService();

    await MeasurementMessagePublisher.PublishAsync(
      CreateSuccessfulMessage(),
      CheckType.Metrology,
      outputService.Object,
      callerName: nameof(PublishResultAsync_MetrologyAndResultsHidden_PublishesMessage),
      callerFile: string.Empty,
      callerLine: 0,
      isVisible: false);

    VerifyPublished(outputService, Times.Once());
  }

  [Fact]
  public async Task PublishResultAsync_ControlProgramAndResultsHidden_DoesNotPublishMessage()
  {
    var outputService = CreateOutputService();

    await MeasurementMessagePublisher.PublishAsync(
      CreateSuccessfulMessage(),
      CheckType.ControlProgram,
      outputService.Object,
      callerName: nameof(PublishResultAsync_ControlProgramAndResultsHidden_DoesNotPublishMessage),
      callerFile: string.Empty,
      callerLine: 0,
      isVisible: false);

    VerifyPublished(outputService, Times.Never());
  }

  [Fact]
  public void BuildMeasurementResultMessage_NormalValue_ContainsChainAndValue()
  {
    var message = MeasurementMessages.BuildMeasurementResultMessage(
      MeasurementTypeCommand.KC,
      new MeasurementRange(15, 10, 20),
      "A1,B2");

    Assert.Contains("A1,B2", message.Header);
    Assert.Contains("15", message.Message);
  }

  [Fact]
  public void BuildMeasurementResultMessage_ResistanceOverload_ContainsOverload()
  {
    var message = MeasurementMessages.BuildMeasurementResultMessage(
      MeasurementTypeCommand.KC,
      new MeasurementRange(9.9E+37, 10, 20),
      "A1,B2");

    Assert.Contains("Overload", message.Message);
  }

  [Fact]
  public void BuildMeasurementResultMessage_BreakdownValue_ContainsBreakdown()
  {
    var message = MeasurementMessages.BuildMeasurementResultMessage(
      MeasurementTypeCommand.PI_ACW,
      new MeasurementRange(25, 10, 20),
      "A1,B2");

    Assert.Contains("ПРОБОЙ", message.Message);
  }

  [Fact]
  public void BuildMeasurementResultMessage_CapacitanceOverload_ContainsOverload()
  {
    var message = MeasurementMessages.BuildMeasurementResultMessage(
      MeasurementTypeCommand.IE,
      new MeasurementRange(9.9E+46, 10, 20),
      "A1,B2");

    Assert.Contains("Overload", message.Message);
  }

  [Fact]
  public void BuildMeasurementResultMessage_CanonicalOverload_ContainsOnlyOverloadState()
  {
    var message = MeasurementMessages.BuildMeasurementResultMessage(
      MeasurementTypeCommand.IE,
      new MeasurementRange(double.PositiveInfinity, 10, 20),
      "A1,B2");

    Assert.Contains("Overload", message.Message);
    Assert.DoesNotContain("Infinity", message.Message);
  }

  [Fact]
  public void BuildMeasurementResultMessage_UpperLimit_FormatsUnitBeforeLimit()
  {
    var message = MeasurementMessages.BuildMeasurementResultMessage(
      MeasurementTypeCommand.KC,
      new MeasurementRange(5, 0, 10),
      "A1");

    Assert.Contains("A1 (Ом<10)", message.Header);
  }

  [Fact]
  public void BuildMeasurementResultMessage_Range_FormatsUnitBetweenLimits()
  {
    var message = MeasurementMessages.BuildMeasurementResultMessage(
      MeasurementTypeCommand.KC,
      new MeasurementRange(9, 8, 10),
      "A1,B2");

    Assert.Contains("A1,B2 (8<Ом<10)", message.Header);
  }

  [Fact]
  public void BuildMeasurementResultMessage_LowerLimit_FormatsLimitBeforeUnit()
  {
    var message = MeasurementMessages.BuildMeasurementResultMessage(
      MeasurementTypeCommand.SI,
      new MeasurementRange(9, 8, -1),
      "A1");

    Assert.Contains("A1 (8<МОм)", message.Header);
  }

  private static Mock<IMessageOutputService> CreateOutputService()
  {
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
      .Returns(Task.CompletedTask);
    return outputService;
  }

  private static ShowMessageModel CreateSuccessfulMessage()
  {
    var message = new ShowMessageModel
    {
      Header = "Measurement",
      Message = "15 Ohm",
      MessageColor = Colors.Green,
    };
    message.Status = ShowMessageModel.MessageType.Success;
    return message;
  }

  private static void VerifyPublished(Mock<IMessageOutputService> outputService, Times times)
  {
    outputService.Verify(
      service => service.ShowMessageAsync(
        It.IsAny<ShowMessageModel>(),
        It.IsAny<bool>(),
        It.IsAny<bool>(),
        It.IsAny<bool>(),
        It.IsAny<bool>(),
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<int>()),
      times);
  }
}
