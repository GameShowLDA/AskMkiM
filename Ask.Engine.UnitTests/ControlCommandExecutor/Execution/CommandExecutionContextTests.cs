using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandExecutor.Execution;
using Ask.Protocol.Messages.EntryPoints;

namespace Ask.Engine.UnitTests.ControlCommandExecutor.Execution;

public sealed class CommandExecutionContextTests
{
  [Fact]
  public void ProtocolSourceLines_DefaultToExecutedCommandSourceLines()
  {
    var command = new SiCommandModel
    {
      SourceLines = ["820 СИ 500В, 10МОм, A1-B2"],
    };

    var context = CreateContext(command);

    Assert.Same(command.SourceLines, context.ProtocolSourceLines);
  }

  [Fact]
  public void ProtocolSourceLines_CanUseFullParentCommandWithoutChangingNestedCommand()
  {
    var command = new SiCommandModel
    {
      SourceLines = ["СИ 500В, 10МОм"],
    };
    string[] parentSourceLines =
    [
      "820 ПИ 1000В, 5с, СИ 500В, 10МОм",
      " A1-B2, C3-D4",
    ];
    var context = CreateContext(command);

    context.ProtocolSourceLines = parentSourceLines;

    Assert.Equal(parentSourceLines, context.ProtocolSourceLines);
    Assert.Equal(["СИ 500В, 10МОм"], command.SourceLines);
    Assert.Equal(
      "  820 ПИ 1000В, 5с, СИ 500В, 10МОм\r\n   A1-B2, C3-D4",
      CommandMessages.FormatSourceLines(context.ProtocolSourceLines));
  }

  [Fact]
  public void FormatSourceLinesWithHeader_ReplacesOriginalHeaderAndKeepsParametersAndPoints()
  {
    string[] sourceLines =
    [
      "460 ПИ 100В, 100<МОм, 1с, К 500В, 1с, К",
      " *К4/41",
      " *К4/71",
    ];

    string result = CommandMessages.FormatSourceLinesWithHeader("460 ПИ/СИ1", sourceLines);

    Assert.Equal(
      "460 ПИ/СИ1  100В, 100<МОм, 1с, К 500В, 1с, К\r\n   *К4/41\r\n   *К4/71",
      result);
  }

  private static CommandExecutionContext CreateContext(BaseCommandModel command)
    => new(null!, command, null!, null!, string.Empty);
}
