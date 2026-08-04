using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Protocol.Messages.EntryPoints;

namespace Ask.Engine.UnitTests.ProtocolMessages;

public sealed class MeasurementMessagesTests
{
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
}
