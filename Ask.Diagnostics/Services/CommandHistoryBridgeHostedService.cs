using Ask.Diagnostics.Abstractions;
using Microsoft.Extensions.Hosting;

namespace Ask.Diagnostics.Services
{
  internal sealed class CommandHistoryBridgeHostedService : IHostedService
  {
    private readonly ICommandHistoryService _commandHistory;

    public CommandHistoryBridgeHostedService(ICommandHistoryService commandHistory)
    {
      _commandHistory = commandHistory;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
      DiagnosticCommandHistory.Configure(_commandHistory.Add);
      return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
      DiagnosticCommandHistory.Configure(null);
      return Task.CompletedTask;
    }
  }
}
