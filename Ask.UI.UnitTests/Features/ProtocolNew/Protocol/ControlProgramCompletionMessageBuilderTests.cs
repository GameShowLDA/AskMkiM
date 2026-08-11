using Ask.Core.Shared.DTO.Executor;
using Ask.UI.Features.ProtocolNew.Protocol;

namespace Ask.UI.UnitTests.Features.ProtocolNew.Protocol;

public sealed class ControlProgramCompletionMessageBuilderTests
{
  [Theory]
  [InlineData("Холостой режим")]
  [InlineData("Рабочий режим")]
  public void Build_IncludesActualExecutionMode(string mode)
  {
    var settings = CreateSettings(mode, TimeSpan.FromSeconds(1));

    var message = ControlProgramCompletionMessageBuilder.Build(settings);

    Assert.Equal($"ЗАВЕРШЕНИЕ ПРОГРАММЫ ({mode})", message.Header);
    Assert.True(message.UseSuccessColorForEntireMessage);
    Assert.False(message.CanBeDeleted);
  }

  [Theory]
  [InlineData(0, "Время выполнения: 0 мин 0,0 с")]
  [InlineData(3.359, "Время выполнения: 0 мин 3,4 с")]
  [InlineData(59.96, "Время выполнения: 1 мин 0,0 с")]
  [InlineData(62, "Время выполнения: 1 мин 2,0 с")]
  [InlineData(3665, "Время выполнения: 61 мин 5,0 с")]
  public void Build_FormatsDurationAsTotalMinutesAndSeconds(
    double durationSeconds,
    string expected)
  {
    var settings = CreateSettings("Рабочий режим", TimeSpan.FromSeconds(durationSeconds));

    var message = ControlProgramCompletionMessageBuilder.Build(settings);

    Assert.Equal(expected, message.Message);
  }

  private static ActionSettings CreateSettings(string mode, TimeSpan duration) => new()
  {
    StartDelegate = (_, _, _, _, _) => Task.CompletedTask,
    Name = "Программа контроля",
    Mode = mode,
    ExecutionDuration = duration,
  };
}
