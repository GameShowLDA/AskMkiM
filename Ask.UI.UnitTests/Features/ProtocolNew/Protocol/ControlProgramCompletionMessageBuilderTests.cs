using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Metadata.Enums.ExecutionEnums;
using Ask.UI.Features.ProtocolNew.Protocol;

namespace Ask.UI.UnitTests.Features.ProtocolNew.Protocol;

public sealed class ControlProgramCompletionMessageBuilderTests
{
  [Theory]
  [InlineData("Холостой режим")]
  [InlineData("Рабочий режим")]
  public void Build_WhenExecutionSuccessful_IncludesActualExecutionMode(string mode)
  {
    var settings = CreateSettings(mode, TimeSpan.FromSeconds(1));

    var message = ControlProgramCompletionMessageBuilder.Build(
      settings,
      ExecutionCompletionStatus.Success);

    Assert.Equal(
      $"ЗАВЕРШЕНИЕ ПРОГРАММЫ ({mode})",
      message.Header);

    Assert.True(message.UseSuccessColorForEntireMessage);
    Assert.False(message.CanBeDeleted);
  }

  [Theory]
  [InlineData(0, "Время выполнения: 0 мин 0,0 с")]
  [InlineData(3.359, "Время выполнения: 0 мин 3,4 с")]
  [InlineData(59.96, "Время выполнения: 1 мин 0,0 с")]
  [InlineData(62, "Время выполнения: 1 мин 2,0 с")]
  [InlineData(3665, "Время выполнения: 61 мин 5,0 с")]
  public void Build_WhenExecutionSuccessful_FormatsDurationAsTotalMinutesAndSeconds(
    double durationSeconds,
    string expected)
  {
    var settings = CreateSettings(
      "Рабочий режим",
      TimeSpan.FromSeconds(durationSeconds));

    var message = ControlProgramCompletionMessageBuilder.Build(
      settings,
      ExecutionCompletionStatus.Success);

    Assert.Equal(expected, message.Message);
  }

  [Theory]
  [InlineData("Холостой режим")]
  [InlineData("Рабочий режим")]
  public void Build_WhenExecutionInterrupted_ReturnsInterruptedMessage(string mode)
  {
    var settings = CreateSettings(
      mode,
      TimeSpan.FromSeconds(12.5));

    var message = ControlProgramCompletionMessageBuilder.Build(
      settings,
      ExecutionCompletionStatus.Interrupted);

    Assert.Equal(
      $"ПРОГРАММА ПРЕРВАНА ({mode})",
      message.Header);

    Assert.Equal(
      "Время выполнения: 0 мин 12,5 с",
      message.Message);

    Assert.False(message.UseSuccessColorForEntireMessage);
    Assert.False(message.CanBeDeleted);
  }

  private static ActionSettings CreateSettings(
    string mode,
    TimeSpan duration) => new()
    {
      StartDelegate = (_, _, _, _, _) => Task.CompletedTask,
      Name = "Программа контроля",
      Mode = mode,
      ExecutionDuration = duration,
    };
}