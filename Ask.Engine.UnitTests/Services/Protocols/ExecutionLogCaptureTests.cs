using Ask.Core.Services.Protocols;
using Ask.Core.Shared.DTO.Protocol;
using Ask.LogLib;

namespace Ask.Engine.UnitTests.Services.Protocols;

public class ExecutionLogCaptureTests
{
  [Fact]
  public void Capture_PreservesAnchorsAndStopsAfterDispose()
  {
    using var capture = new ExecutionLogCapture();
    capture.Start();
    string marker = Guid.NewGuid().ToString();
    var first = new ShowMessageModel("Первое");
    var removed = new ShowMessageModel("Удалённое");
    capture.RecordMessage(first);
    LoggerUtility.LogInformation(marker + "-first");
    capture.RecordMessage(removed);
    LoggerUtility.LogInformation(marker + "-removed");
    capture.Dispose();
    LoggerUtility.LogInformation(marker + "-stopped");
    var entries = capture.Snapshot(new[] { first }).Where(e => e.Text.Contains(marker)).ToArray();
    Assert.Equal(2, entries.Length);
    Assert.All(entries, entry => Assert.Equal(1, entry.BeforeMessage));
    Assert.Contains("-first", entries[0].Text);
    Assert.Contains("-removed", entries[1].Text);
    capture.Start();
    Assert.DoesNotContain(capture.Snapshot(new[] { first }), e => e.Text.Contains(marker));
  }
}
