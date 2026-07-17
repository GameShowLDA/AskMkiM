namespace MainWindowProgram.Init;

/// <summary>
/// Creates default application settings and an initial equipment configuration.
/// </summary>
internal sealed class ApplicationAutoConfigurationService
{
  /// <summary>
  /// Applies default application settings and creates an initial equipment configuration.
  /// </summary>
  /// <remarks>The implementation will be added with the auto-configuration rules.</remarks>
  public Task ApplyDefaultConfigurationAsync(CancellationToken cancellationToken = default)
  {
    return Task.CompletedTask;
  }
}
