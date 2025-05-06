using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.Controls.ProtocolController
{
  public class ProtocolExecutionInterceptor
  {
    private readonly ProtocolPauseManager _pauseManager;
    private readonly CancellationToken _token;
    private readonly Protocol _protocol;

    public ProtocolExecutionInterceptor(ProtocolPauseManager pauseManager, CancellationToken token, Protocol protocol)
    {
      _pauseManager = pauseManager;
      _token = token;
      _protocol = protocol;
    }

    public async Task Run(Func<CancellationToken, Task> action)
    {
      _token.ThrowIfCancellationRequested();
      await _pauseManager.WaitWhilePausedAsync(_protocol);
      await action(_token);
    }
  }

}
