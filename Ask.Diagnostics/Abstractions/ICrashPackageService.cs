namespace Ask.Diagnostics.Abstractions
{
  public interface ICrashPackageService
  {
    Task<string> CreateAsync(Exception exception, CancellationToken cancellationToken = default);
  }
}
