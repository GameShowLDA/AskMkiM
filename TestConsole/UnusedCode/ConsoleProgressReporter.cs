namespace TestConsole.UnusedCode;

/// <summary>
/// Writes analyzer progress to the console.
/// </summary>
internal sealed class ConsoleProgressReporter : IProgress<UnusedCodeProgress>
{
  private DateTimeOffset _lastWrite = DateTimeOffset.MinValue;

  /// <inheritdoc />
  public void Report(UnusedCodeProgress value)
  {
    if (DateTimeOffset.UtcNow - _lastWrite < TimeSpan.FromMilliseconds(300) &&
      value.ProcessedDocuments < value.TotalDocuments)
    {
      return;
    }

    _lastWrite = DateTimeOffset.UtcNow;
    var percent = value.TotalDocuments == 0
      ? 100
      : Math.Round(value.ProcessedDocuments * 100d / value.TotalDocuments, 1);

    var remaining = EstimateRemaining(value);
    Console.WriteLine(
      $"[{percent,5:0.0}%] Project: {value.Project}; Document: {value.Document}; " +
      $"Documents: {value.ProcessedDocuments}/{value.TotalDocuments}; ETA: {remaining:hh\\:mm\\:ss}");
  }

  private static TimeSpan EstimateRemaining(UnusedCodeProgress progress)
  {
    if (progress.ProcessedDocuments <= 0 || progress.TotalDocuments <= progress.ProcessedDocuments)
    {
      return TimeSpan.Zero;
    }

    var averageTicks = progress.Elapsed.Ticks / progress.ProcessedDocuments;
    return TimeSpan.FromTicks(averageTicks * (progress.TotalDocuments - progress.ProcessedDocuments));
  }
}
