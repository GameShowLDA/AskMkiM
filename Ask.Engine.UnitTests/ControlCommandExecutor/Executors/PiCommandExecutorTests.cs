using Ask.Core.Shared.DTO.Protocol;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandExecutor.Executors;

namespace Ask.Engine.UnitTests.ControlCommandExecutor.Executors;

public sealed class PiCommandExecutorTests
{
  [Fact]
  public void AddCommandSummaryToProtocol_AddsVoltageAndTimeWithoutDocumentationKey()
  {
    var command = new PiCommandModel
    {
      CommandNumber = "65",
      VoltageSource = "438В",
      TimeSource = "1с",
    };
    var protocol = new ProtocolModel();

    PiCommandExecutor.AddCommandSummaryToProtocol(command, protocol);

    var message = Assert.Single(protocol.Info["65 ПИ"]);
    Assert.Equal("Проверка прочности изоляции: 438В, 1с", message.Message);
  }
}
