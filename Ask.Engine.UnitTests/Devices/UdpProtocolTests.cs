using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Ask.Core.Services.Errors.Device;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.DataBase.Engine.Builder;
using Ask.Device.Communication.Common;
using Ask.Device.Communication.Ethernet.Udp.Protocols;
using Ask.Device.Runtime.Device;
using Moq;
using Xunit.Abstractions;

namespace Ask.Engine.UnitTests.Devices;

public sealed class UdpProtocolTests(ITestOutputHelper output)
{
  private static ValueTask<int> Send(UdpClient client, ReadOnlyMemory<byte> data,
    IPEndPoint endpoint, CancellationToken token) => client.SendAsync(data, endpoint, token);

  [Fact]
  public async Task SequentialAndConcurrentQueries_ReuseOneSocketAndMatchResponses()
  {
    using var fixture = new ExchangeFixture();
    var stopwatch = Stopwatch.StartNew();
    for (int i = 0; i < 2000; i++)
      Assert.Equal($"reply:{i}", await fixture.ExchangeAsync(i.ToString()));

    var server = fixture.EchoAsync(30);
    var queries = Enumerable.Range(2000, 30)
      .Select(i => fixture.QueryAsync(i.ToString())).ToArray();
    var results = await Task.WhenAll(queries);
    await server;

    Assert.Equal(Enumerable.Range(2000, 30).Select(i => $"reply:{i}"), results);
    Assert.Single(fixture.Clients);
    output.WriteLine($"2030 UDP round trips: {stopwatch.ElapsedMilliseconds} ms; sockets: {fixture.Clients.Count}");
  }

  [Fact]
  public async Task NoBufferSpaceOnSend_RetriesOnceWithNewSocket()
  {
    int attempts = 0;
    using var fixture = new ExchangeFixture((client, data, endpoint, token) =>
      ++attempts == 1
        ? ValueTask.FromException<int>(new SocketException(10055))
        : Send(client, data, endpoint, token));

    Assert.Equal("reply:6.183.0.0.", await fixture.ExchangeAsync("6.183.0.0."));
    Assert.Equal(2, attempts);
    Assert.Equal(2, fixture.Clients.Count);
    Assert.True(fixture.Handles[0].IsClosed);
  }

  [Theory]
  [InlineData(10055, 2)]
  [InlineData(10051, 1)]
  public async Task SendFailure_PreservesSocketExceptionAndBoundsRetries(int code, int expectedAttempts)
  {
    int attempts = 0;
    using var fixture = new ExchangeFixture((_, _, _, _) =>
    {
      attempts++;
      return ValueTask.FromException<int>(new SocketException(code));
    });

    var error = await Assert.ThrowsAsync<DeviceTransportException>(() => fixture.QueryAsync("command"));
    Assert.Equal(code, Assert.IsType<SocketException>(error.InnerException).ErrorCode);
    Assert.Equal(expectedAttempts, attempts);
    Assert.All(fixture.Handles, handle => Assert.True(handle.IsClosed));
  }

  [Fact]
  public async Task Timeout_DoesNotResendAndNextQueryRecovers()
  {
    using var fixture = new ExchangeFixture();
    var pending = fixture.QueryAsync("lost", timeout: 100);
    await fixture.ReceiveAsync();
    Assert.Contains("не ответило", await pending);
    Assert.True(fixture.Handles[0].IsClosed);
    Assert.False(fixture.Server.Client.Poll(0, SelectMode.SelectRead));
    Assert.Equal("reply:next", await fixture.ExchangeAsync("next"));
    Assert.Equal(2, fixture.Clients.Count);
  }

  [Fact]
  public async Task CancellationDuringReceive_PropagatesAndNextQueryRecovers()
  {
    using var fixture = new ExchangeFixture();
    using var cancellation = new CancellationTokenSource();
    var pending = fixture.QueryAsync("cancel", token: cancellation.Token);
    await fixture.ReceiveAsync();
    cancellation.Cancel();

    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
    Assert.True(fixture.Handles[0].IsClosed);
    Assert.Equal("reply:next", await fixture.ExchangeAsync("next"));
  }

  [Fact]
  public async Task CancellationWhileQueued_DoesNotCloseActiveSocket()
  {
    using var fixture = new ExchangeFixture();
    var first = fixture.QueryAsync("first");
    var request = await fixture.ReceiveAsync();
    using var cancellation = new CancellationTokenSource();
    var second = fixture.QueryAsync("second", token: cancellation.Token);
    cancellation.Cancel();
    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second);
    await fixture.ReplyAsync(request, "first");
    Assert.Equal("first", await first);
    Assert.Single(fixture.Clients);
    Assert.False(fixture.Handles[0].IsClosed);
  }

  [Fact]
  public async Task CancellationAfterNoBufferSpace_DoesNotRetry()
  {
    using var cancellation = new CancellationTokenSource();
    int attempts = 0;
    using var fixture = new ExchangeFixture((_, _, _, _) =>
    {
      attempts++;
      cancellation.Cancel();
      return ValueTask.FromException<int>(new SocketException(10055));
    });

    await Assert.ThrowsAnyAsync<OperationCanceledException>(
      () => fixture.QueryAsync("cancel", token: cancellation.Token));
    Assert.Equal(1, attempts);
  }

  [Fact]
  public async Task QueuedStaleDatagram_IsDiscardedBeforeNextSend()
  {
    using var fixture = new ExchangeFixture();
    Assert.Equal("reply:first", await fixture.ExchangeAsync("first"));
    var local = (IPEndPoint)fixture.Clients[0].Client.LocalEndPoint!;
    await fixture.Server.SendAsync(Encoding.UTF8.GetBytes("stale"), local);
    Assert.True(fixture.Clients[0].Client.Poll(1_000_000, SelectMode.SelectRead));

    Assert.Equal("reply:second", await fixture.ExchangeAsync("second"));
    Assert.Single(fixture.Clients);
  }

  [Fact]
  public async Task DifferentSender_IsIgnoredWithinOriginalTimeout()
  {
    using var fixture = new ExchangeFixture();
    using var foreign = new UdpClient(new IPEndPoint(IPAddress.Parse("127.0.0.2"), 0));
    var pending = fixture.QueryAsync("command");
    var request = await fixture.ReceiveAsync();
    await foreign.SendAsync(Encoding.UTF8.GetBytes("foreign"), request.RemoteEndPoint);
    await fixture.ReplyAsync(request, "correct");
    Assert.Equal("correct", await pending);
  }

  [Fact]
  public async Task PortChange_ReleasesOldSocket()
  {
    using var fixture = new ExchangeFixture();
    Assert.Equal("reply:first", await fixture.ExchangeAsync("first"));
    using var otherServer = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
    var query = fixture.Protocol.QueryAsync("second", timeout: 2000,
      port: ((IPEndPoint)otherServer.Client.LocalEndPoint!).Port);
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    var request = await otherServer.ReceiveAsync(deadline.Token);
    await otherServer.SendAsync(Encoding.UTF8.GetBytes("second"), request.RemoteEndPoint);
    Assert.Equal("second", await query);
    Assert.Equal(2, fixture.Clients.Count);
    Assert.True(fixture.Handles[0].IsClosed);
  }

  [Fact]
  public async Task SendOnly_ClosesSocketAndNextQueryRecovers()
  {
    using var fixture = new ExchangeFixture();
    Assert.Equal(string.Empty, await fixture.QueryAsync("no-response", timeout: 0));
    await fixture.ReceiveAsync();
    Assert.True(fixture.Handles[0].IsClosed);
    Assert.Equal("reply:next", await fixture.ExchangeAsync("next"));
  }

  [Fact]
  public async Task DisposeDuringReceive_ClosesSocketAndRejectsFurtherQueries()
  {
    using var fixture = new ExchangeFixture();
    var pending = fixture.QueryAsync("pending");
    await fixture.ReceiveAsync();
    fixture.Protocol.Dispose();
    await Assert.ThrowsAsync<DeviceTransportException>(() => pending);
    var error = await Assert.ThrowsAsync<DeviceTransportException>(() => fixture.QueryAsync("after"));
    Assert.IsType<ObjectDisposedException>(error.InnerException);
    Assert.Single(fixture.Clients);
    Assert.True(fixture.Handles[0].IsClosed);
  }

  [Fact]
  public async Task RuntimeDevice_CacheRemovalAndAddressChangeReleaseProtocol()
  {
    using var fixture = new ExchangeFixture();
    Assert.Equal("reply:first", await fixture.ExchangeAsync("first"));
    using var module = new ModuleRelayControl { ConnectionDetails = "127.0.0.1" };
    var watchdog = new HardwareWatchdogProtocol(fixture.Protocol, "test");
    module.DeviceProtocol = watchdog;
    module.ConnectionDetails = "127.0.0.1";
    Assert.Same(watchdog, module.DeviceProtocol);
    Assert.False(fixture.Handles[0].IsClosed);

    module.ConnectionDetails = "127.0.0.2";
    Assert.True(fixture.Handles[0].IsClosed);
    Assert.NotSame(watchdog, module.DeviceProtocol);

    using var second = new ExchangeFixture();
    Assert.Equal("reply:second", await second.ExchangeAsync("second"));
    module.DeviceProtocol = new HardwareWatchdogProtocol(second.Protocol, "test");
    var cache = new DeviceCache();
    cache.Set(1, module);
    cache.Remove(1);
    Assert.True(second.Handles[0].IsClosed);
  }

  private sealed class ExchangeFixture : IDisposable
  {
    public UdpClient Server { get; } = new(new IPEndPoint(IPAddress.Loopback, 0));
    public List<UdpClient> Clients { get; } = [];
    public List<SafeSocketHandle> Handles { get; } = [];
    public UdpProtocol Protocol { get; }
    private readonly CancellationTokenSource _deadline = new(TimeSpan.FromSeconds(30));

    public ExchangeFixture(
      Func<UdpClient, ReadOnlyMemory<byte>, IPEndPoint, CancellationToken, ValueTask<int>>? send = null)
    {
      var device = new Mock<IDevice>();
      device.SetupGet(x => x.Name).Returns("UDP test");
      device.SetupGet(x => x.ConnectionDetails).Returns("127.0.0.1");
      Protocol = new UdpProtocol(device.Object, _ =>
      {
        var client = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        Clients.Add(client);
        Handles.Add(client.Client.SafeHandle);
        return client;
      }, send ?? Send);
    }

    public Task<string> QueryAsync(string command, int timeout = 2000, CancellationToken token = default) =>
      Protocol.QueryAsync(command, timeout: timeout,
        port: ((IPEndPoint)Server.Client.LocalEndPoint!).Port,
        cancellationToken: token == default ? _deadline.Token : token);

    public async Task<string> ExchangeAsync(string command)
    {
      var echo = EchoAsync(1);
      var response = await QueryAsync(command);
      await echo;
      return response;
    }

    public ValueTask<UdpReceiveResult> ReceiveAsync() => Server.ReceiveAsync(_deadline.Token);

    public ValueTask<int> ReplyAsync(UdpReceiveResult request, string response) =>
      Server.SendAsync(Encoding.UTF8.GetBytes(response), request.RemoteEndPoint, _deadline.Token);

    public async Task EchoAsync(int count)
    {
      for (int i = 0; i < count; i++)
      {
        var request = await ReceiveAsync();
        await ReplyAsync(request, "reply:" + Encoding.UTF8.GetString(request.Buffer));
      }
    }

    public void Dispose()
    {
      Protocol.Dispose();
      Server.Dispose();
      _deadline.Dispose();
    }
  }
}
