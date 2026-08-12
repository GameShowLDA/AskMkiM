using System.Text.Json;

namespace Ask.Device.ResponseProcessor.ModuleRelayControl.ResponseProcessing.Checkers;

internal static class ResponseDeserializer
{
  internal static TResponse? Deserialize<TResponse>(string response)
  {
    if (string.IsNullOrWhiteSpace(response))
    {
      return default;
    }

    try
    {
      return JsonSerializer.Deserialize<TResponse>(response);
    }
    catch (JsonException)
    {
      return default;
    }
  }
}
