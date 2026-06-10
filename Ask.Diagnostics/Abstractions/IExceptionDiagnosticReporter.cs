namespace Ask.Diagnostics.Abstractions
{
  public interface IExceptionDiagnosticReporter
  {
    void Report(Exception exception, string source);
  }
}
