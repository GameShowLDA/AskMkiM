using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NewCore.Communication
{
  internal sealed class ViewPowerClient : IDisposable
  {
    private static readonly Uri BaseAddress = new("http://localhost:15178/ViewPower/");
    private static readonly Regex PortNameRegex = new(@"var\s+portName\s*=\s*""(?<value>[^""]+)""", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex ProtocolTypeRegex = new(@"var\s+protocolType\s*=\s*""(?<value>[^""]+)""", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly HttpClient _httpClient;

    public ViewPowerClient()
    {
      var handler = new HttpClientHandler
      {
        UseCookies = true,
        CookieContainer = new CookieContainer(),
      };

      _httpClient = new HttpClient(handler)
      {
        BaseAddress = BaseAddress,
        Timeout = TimeSpan.FromSeconds(10),
      };
    }

    public async Task<ViewPowerSessionContext> OpenSessionAsync(CancellationToken cancellationToken = default)
    {
      string html = await GetTextAsync($"monitor?{CreateNonce()}", cancellationToken).ConfigureAwait(false);
      string portName = ParseRequired(html, PortNameRegex, "ViewPower portName");
      string protocolType = ParseRequired(html, ProtocolTypeRegex, "ViewPower protocolType");

      return new ViewPowerSessionContext(portName, protocolType);
    }

    public async Task<ViewPowerMonitorSnapshot> GetMonitorDataAsync(string portName, CancellationToken cancellationToken = default)
    {
      string payload = await PostFormAsync(
        $"workstatus/reqMonitorData?{CreateNonce()}",
        new[]
        {
          new KeyValuePair<string, string>("portName", portName),
        },
        cancellationToken).ConfigureAwait(false);

      using var json = JsonDocument.Parse(payload);
      JsonElement root = json.RootElement;
      JsonElement workInfo = root.TryGetProperty("workInfo", out var workInfoElement)
        ? workInfoElement
        : default;

      return new ViewPowerMonitorSnapshot(
        portName,
        GetString(root, "protocolType"),
        GetString(workInfo, "workMode"),
        GetBoolean(workInfo, "outputON"),
        GetBoolean(workInfo, "buzzerCtrl"),
        GetBoolean(workInfo, "enableOutlet1"),
        GetString(workInfo, "deviceId"),
        GetString(workInfo, "outputVoltage"),
        GetString(workInfo, "inputVoltage"));
    }

    public async Task<ViewPowerCommandResult> SendRealTimeControlAsync(
      string portName,
      string type,
      string minute,
      CancellationToken cancellationToken = default)
    {
      string response = await PostFormAsync(
        $"control/realTimeCtrl?{CreateNonce()}",
        new[]
        {
          new KeyValuePair<string, string>("portName", portName),
          new KeyValuePair<string, string>("type", type),
          new KeyValuePair<string, string>("minute", minute),
        },
        cancellationToken).ConfigureAwait(false);

      return new ViewPowerCommandResult(response, IsAcceptedResponse(response));
    }

    public async Task InitializeRealTimeControlAsync(
      string portName,
      string protocolType,
      CancellationToken cancellationToken = default)
    {
      string action = ResolveRealTimeControlAction(protocolType);
      _ = await GetTextAsync($"control/{action}?portName={Uri.EscapeDataString(portName)}&{CreateNonce()}", cancellationToken).ConfigureAwait(false);
    }

    public async Task<ViewPowerMonitorSnapshot> WaitForMonitorStateAsync(
      string portName,
      Func<ViewPowerMonitorSnapshot, bool> statePredicate,
      TimeSpan timeout,
      CancellationToken cancellationToken = default)
    {
      ArgumentNullException.ThrowIfNull(statePredicate);

      ViewPowerMonitorSnapshot snapshot = await GetMonitorDataAsync(portName, cancellationToken).ConfigureAwait(false);
      if (statePredicate(snapshot))
      {
        return snapshot;
      }

      DateTime deadline = DateTime.UtcNow + timeout;
      while (DateTime.UtcNow < deadline)
      {
        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
        snapshot = await GetMonitorDataAsync(portName, cancellationToken).ConfigureAwait(false);

        if (statePredicate(snapshot))
        {
          return snapshot;
        }
      }

      return snapshot;
    }

    public void Dispose()
    {
      _httpClient.Dispose();
    }

    private async Task<string> GetTextAsync(string relativeUrl, CancellationToken cancellationToken)
    {
      using var response = await _httpClient.GetAsync(relativeUrl, cancellationToken).ConfigureAwait(false);
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> PostFormAsync(
      string relativeUrl,
      IEnumerable<KeyValuePair<string, string>> formData,
      CancellationToken cancellationToken)
    {
      using var response = await _httpClient.PostAsync(relativeUrl, new FormUrlEncodedContent(formData), cancellationToken).ConfigureAwait(false);
      response.EnsureSuccessStatusCode();

      byte[] bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
      return NormalizeResponse(bytes);
    }

    private static string ParseRequired(string html, Regex regex, string name)
    {
      Match match = regex.Match(html);
      if (!match.Success)
      {
        throw new InvalidOperationException($"{name} was not found in ViewPower monitor page.");
      }

      return match.Groups["value"].Value;
    }

    private static string GetString(JsonElement element, string propertyName)
    {
      if (element.ValueKind != JsonValueKind.Object ||
          !element.TryGetProperty(propertyName, out var value))
      {
        return string.Empty;
      }

      return value.ValueKind switch
      {
        JsonValueKind.String => value.GetString() ?? string.Empty,
        JsonValueKind.Number => value.GetRawText(),
        JsonValueKind.True => bool.TrueString,
        JsonValueKind.False => bool.FalseString,
        _ => string.Empty,
      };
    }

    private static bool GetBoolean(JsonElement element, string propertyName)
    {
      if (element.ValueKind != JsonValueKind.Object ||
          !element.TryGetProperty(propertyName, out var value))
      {
        return false;
      }

      return value.ValueKind switch
      {
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.String => bool.TryParse(value.GetString(), out bool parsed) && parsed,
        JsonValueKind.Number => value.TryGetInt32(out int number) && number != 0,
        _ => false,
      };
    }

    private static bool IsAcceptedResponse(string response)
    {
      if (string.IsNullOrWhiteSpace(response))
      {
        return false;
      }

      string normalized = response.Trim().TrimStart('\uFEFF');
      if (normalized.Equals("nologin", StringComparison.OrdinalIgnoreCase))
      {
        return false;
      }

      return normalized.Equals("success", StringComparison.OrdinalIgnoreCase) ||
             normalized.Equals("!success", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeResponse(byte[] responseBytes)
    {
      return Encoding.UTF8.GetString(responseBytes).TrimStart('\uFEFF').Trim();
    }

    private static string CreateNonce()
    {
      return Random.Shared.NextDouble().ToString(CultureInfo.InvariantCulture);
    }

    private static string ResolveRealTimeControlAction(string protocolType)
    {
      return protocolType switch
      {
        "P05" or "P00" or "P01" or "P02" or "P03" or "P13" or "P14" => "initRealTimeCtrlP00",
        "P08" => "initRealTimeCtrlP08",
        "P09" => "initRealTimeCtrlP09",
        "P10" => "initRealTimeCtrlP10",
        "P31" => "initRealTimeCtrlP31",
        "P33" or "P39" => "initRealTimeCtrlP33",
        "P35" or "P36" or "P38" => "initRealTimeCtrlP35",
        "P40" => "initRealTimeCtrlP40",
        "P71" => "initRealTimeCtrlP71",
        "P98" => "initRealTimeCtrlP98",
        "PMV" => "initRealTimeCtrlPMV",
        "MODBUS" or "SEC" => "initRealTimeCtrlTAURUS",
        _ => "initRealTimeCtrlP00",
      };
    }
  }

  internal readonly record struct ViewPowerSessionContext(string PortName, string ProtocolType);

  internal readonly record struct ViewPowerCommandResult(string ResponseText, bool Accepted);

  internal readonly record struct ViewPowerMonitorSnapshot(
    string PortName,
    string ProtocolType,
    string WorkMode,
    bool OutputOn,
    bool BuzzerOn,
    bool EnableOutlet1,
    string DeviceId,
    string OutputVoltage,
    string InputVoltage);
}
