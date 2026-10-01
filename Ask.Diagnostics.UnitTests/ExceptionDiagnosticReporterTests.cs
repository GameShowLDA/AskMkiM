using Ask.Core.Services.Errors.Device;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Diagnostics.Abstractions;
using Ask.Diagnostics.Extensions;
using Ask.Diagnostics.Models;
using Ask.Diagnostics.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Ask.Diagnostics.UnitTests;

public sealed class ExceptionDiagnosticReporterTests
{
  public static IEnumerable<object[]> Exceptions()
  {
    var noResponse = new DeviceNoResponseException(new TestDevice(), "READ?", 100,
      new TimeoutException());
    yield return new object[] { noResponse, false };
    yield return new object[] { new AggregateException(noResponse), false };
    yield return new object[] { new AggregateException(noResponse, new AggregateException(noResponse)), false };
    yield return new object[] { new AggregateException(noResponse, new InvalidOperationException()), true };
    yield return new object[] { new InvalidOperationException("Unexpected failure", noResponse), true };
    yield return new object[] { new TimeoutException(), true };
  }

  [Theory]
  [MemberData(nameof(Exceptions))]
  public async Task ReportAsync_CreatesPackageOnlyForUnexpectedErrors(Exception exception, bool shouldReport)
  {
    var packages = new RecordingPackageService();
    using var provider = CreateProvider(packages);
    var reporter = provider.GetRequiredService<IExceptionDiagnosticReporter>();

    var result = await reporter.ReportAsync(exception, "test");

    Assert.Equal(shouldReport ? "report.zip" : null, result);
    Assert.Equal(shouldReport ? 1 : 0, packages.Calls);
    Assert.Equal(shouldReport, CrashReportPolicy.ShouldReport(exception));
  }

  [Theory]
  [MemberData(nameof(Exceptions))]
  public async Task Report_QueuesPackageOnlyForUnexpectedErrors(Exception exception, bool shouldReport)
  {
    var packages = new RecordingPackageService();
    var queuedMessages = 0;
    using var provider = CreateProvider(packages, _ => Interlocked.Increment(ref queuedMessages));
    var reporter = provider.GetRequiredService<IExceptionDiagnosticReporter>();

    reporter.Report(exception, "test");

    Assert.Equal(shouldReport ? 1 : 0, queuedMessages);
    if (shouldReport)
    {
      await packages.Created.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }
    Assert.Equal(shouldReport ? 1 : 0, packages.Calls);
  }

  private static ServiceProvider CreateProvider(RecordingPackageService packages, Action<string>? information = null)
  {
    var services = new ServiceCollection();
    services.AddCrashDiagnostics(logInformation: information);
    services.AddSingleton<ICrashPackageService>(packages);
    return services.BuildServiceProvider();
  }

  private sealed class RecordingPackageService : ICrashPackageService
  {
    public int Calls;
    public TaskCompletionSource<bool> Created { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task<string> CreateAsync(Exception exception, IReadOnlyList<CrashReportArtifact>? artifacts = null,
      CancellationToken cancellationToken = default)
    {
      Interlocked.Increment(ref Calls);
      Created.TrySetResult(true);
      return Task.FromResult("report.zip");
    }
  }

  private sealed class TestDevice : IDevice
  {
    public int Id { get; set; }
    public string Name { get; set; } = "Мультиметр";
    public string Description { get; set; } = string.Empty;
    public int Number { get; set; } = 1;
    public string ConnectionDetails { get; set; } = string.Empty;
    public DeviceType DeviceType => default;
    public string DeviceClass { get; set; } = string.Empty;
    public bool IsHardwareFailureSimulationEnabled { get; set; }
    public IConnectable ConnectableManager { get; set; } = null!;
    public IDeviceProtocol DeviceProtocol { get; set; } = null!;
    public IConnectionInfo ConnectionInfo => null!;
  }
}
