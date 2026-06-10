namespace Ask.Diagnostics.Abstractions
{
  public interface ICrashPackageLogSink
  {
    void Information(string message);

    void Error(Exception exception, string message);

    void PackageCreated(string path);
  }
}
