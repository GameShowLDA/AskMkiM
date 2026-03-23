using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Ask.Device.Runtime.Function.MikUps1101rRm.ViewPower
{
  /// <summary>
  /// Предоставляет клиент для взаимодействия с локальным HTTP-интерфейсом ViewPower.
  /// </summary>
  internal sealed class ViewPowerClient : IDisposable
  {
    /// <summary>
    /// Базовый адрес локального интерфейса ViewPower.
    /// </summary>
    private static readonly Uri BaseAddress = new("http://localhost:15178/ViewPower/");

    /// <summary>
    /// Регулярное выражение для извлечения имени порта из HTML ViewPower.
    /// </summary>
    private static readonly Regex PortNameRegex = new(@"var\s+portName\s*=\s*""(?<value>[^""]+)""", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Регулярное выражение для извлечения типа протокола из HTML ViewPower.
    /// </summary>
    private static readonly Regex ProtocolTypeRegex = new(@"var\s+protocolType\s*=\s*""(?<value>[^""]+)""", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// HTTP-клиент, через который выполняются запросы к ViewPower.
    /// </summary>
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="ViewPowerClient"/>.
    /// </summary>
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

    /// <summary>
    /// Открывает сессию ViewPower и извлекает параметры текущего порта.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Контекст текущей сессии ViewPower.</returns>
    public async Task<ViewPowerSessionContext> OpenSessionAsync(CancellationToken cancellationToken = default)
    {
      string html = await GetTextAsync($"monitor?{CreateNonce()}", cancellationToken).ConfigureAwait(false);
      string portName = ParseRequired(html, PortNameRegex, "ViewPower portName");
      string protocolType = ParseRequired(html, ProtocolTypeRegex, "ViewPower protocolType");

      return new ViewPowerSessionContext(portName, protocolType);
    }

    /// <summary>
    /// Получает текущие данные мониторинга из ViewPower.
    /// </summary>
    /// <param name="portName">Имя порта UPS в ViewPower.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Снимок состояния UPS.</returns>
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

    /// <summary>
    /// Отправляет команду real-time control в ViewPower.
    /// </summary>
    /// <param name="portName">Имя порта UPS в ViewPower.</param>
    /// <param name="type">Тип команды управления.</param>
    /// <param name="minute">Задержка выполнения команды в минутах.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Результат выполнения команды ViewPower.</returns>
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

    /// <summary>
    /// Инициализирует страницу real-time control для выбранного типа протокола.
    /// </summary>
    /// <param name="portName">Имя порта UPS в ViewPower.</param>
    /// <param name="protocolType">Тип протокола UPS.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task InitializeRealTimeControlAsync(
      string portName,
      string protocolType,
      CancellationToken cancellationToken = default)
    {
      string action = ResolveRealTimeControlAction(protocolType);
      _ = await GetTextAsync($"control/{action}?portName={Uri.EscapeDataString(portName)}&{CreateNonce()}", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Ожидает, пока состояние UPS в ViewPower не удовлетворит заданному условию.
    /// </summary>
    /// <param name="portName">Имя порта UPS в ViewPower.</param>
    /// <param name="statePredicate">Предикат требуемого состояния.</param>
    /// <param name="timeout">Максимальное время ожидания.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Последний полученный снимок состояния UPS.</returns>
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

    /// <summary>
    /// Освобождает HTTP-клиент ViewPower.
    /// </summary>
    public void Dispose()
    {
      _httpClient.Dispose();
    }

    /// <summary>
    /// Выполняет GET-запрос и возвращает текст ответа.
    /// </summary>
    /// <param name="relativeUrl">Относительный URL в интерфейсе ViewPower.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Текст ответа сервера.</returns>
    private async Task<string> GetTextAsync(string relativeUrl, CancellationToken cancellationToken)
    {
      using var response = await _httpClient.GetAsync(relativeUrl, cancellationToken).ConfigureAwait(false);
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Выполняет POST-запрос формы и возвращает нормализованный текст ответа.
    /// </summary>
    /// <param name="relativeUrl">Относительный URL в интерфейсе ViewPower.</param>
    /// <param name="formData">Набор значений формы.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Нормализованный текст ответа сервера.</returns>
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

    /// <summary>
    /// Извлекает обязательное значение из HTML по регулярному выражению.
    /// </summary>
    /// <param name="html">HTML-документ ViewPower.</param>
    /// <param name="regex">Регулярное выражение поиска.</param>
    /// <param name="name">Имя логической переменной для текста ошибки.</param>
    /// <returns>Найденное значение.</returns>
    private static string ParseRequired(string html, Regex regex, string name)
    {
      Match match = regex.Match(html);
      if (!match.Success)
      {
        throw new InvalidOperationException($"{name} was not found in ViewPower monitor page.");
      }

      return match.Groups["value"].Value;
    }

    /// <summary>
    /// Получает строковое значение свойства JSON.
    /// </summary>
    /// <param name="element">JSON-объект.</param>
    /// <param name="propertyName">Имя свойства.</param>
    /// <returns>Строковое значение свойства либо пустая строка.</returns>
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

    /// <summary>
    /// Получает логическое значение свойства JSON.
    /// </summary>
    /// <param name="element">JSON-объект.</param>
    /// <param name="propertyName">Имя свойства.</param>
    /// <returns>Логическое значение свойства либо <see langword="false"/>.</returns>
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

    /// <summary>
    /// Проверяет, был ли ответ ViewPower принят как успешный.
    /// </summary>
    /// <param name="response">Текст ответа ViewPower.</param>
    /// <returns><see langword="true"/>, если ViewPower принял команду.</returns>
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

    /// <summary>
    /// Нормализует байтовый ответ ViewPower в строку.
    /// </summary>
    /// <param name="responseBytes">Байты ответа ViewPower.</param>
    /// <returns>Нормализованная строка ответа.</returns>
    private static string NormalizeResponse(byte[] responseBytes)
    {
      return Encoding.UTF8.GetString(responseBytes).TrimStart('\uFEFF').Trim();
    }

    /// <summary>
    /// Создаёт случайный параметр для обхода кеширования запросов.
    /// </summary>
    /// <returns>Строка со случайным значением.</returns>
    private static string CreateNonce()
    {
      return Random.Shared.NextDouble().ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Разрешает маршрут инициализации real-time control в зависимости от протокола UPS.
    /// </summary>
    /// <param name="protocolType">Тип протокола UPS.</param>
    /// <returns>Имя действия ViewPower для инициализации real-time control.</returns>
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

  /// <summary>
  /// Содержит данные текущей сессии ViewPower.
  /// </summary>
  internal readonly record struct ViewPowerSessionContext
  {
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="ViewPowerSessionContext"/>.
    /// </summary>
    /// <param name="portName">Имя порта UPS в ViewPower.</param>
    /// <param name="protocolType">Тип протокола UPS.</param>
    public ViewPowerSessionContext(string portName, string protocolType)
    {
      PortName = portName;
      ProtocolType = protocolType;
    }

    /// <summary>
    /// Получает имя порта UPS в ViewPower.
    /// </summary>
    public string PortName { get; init; }

    /// <summary>
    /// Получает тип протокола UPS.
    /// </summary>
    public string ProtocolType { get; init; }
  }

  /// <summary>
  /// Описывает результат выполнения команды в ViewPower.
  /// </summary>
  internal readonly record struct ViewPowerCommandResult
  {
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="ViewPowerCommandResult"/>.
    /// </summary>
    /// <param name="responseText">Сырой текст ответа ViewPower.</param>
    /// <param name="accepted">Признак принятия команды ViewPower.</param>
    public ViewPowerCommandResult(string responseText, bool accepted)
    {
      ResponseText = responseText;
      Accepted = accepted;
    }

    /// <summary>
    /// Получает сырой текст ответа ViewPower.
    /// </summary>
    public string ResponseText { get; init; }

    /// <summary>
    /// Получает признак принятия команды ViewPower.
    /// </summary>
    public bool Accepted { get; init; }
  }

  /// <summary>
  /// Описывает снимок состояния UPS, полученный из ViewPower.
  /// </summary>
  internal readonly record struct ViewPowerMonitorSnapshot
  {
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="ViewPowerMonitorSnapshot"/>.
    /// </summary>
    /// <param name="portName">Имя порта UPS в ViewPower.</param>
    /// <param name="protocolType">Тип протокола UPS.</param>
    /// <param name="workMode">Текущий режим работы UPS.</param>
    /// <param name="outputOn">Признак включенного выходного питания.</param>
    /// <param name="buzzerOn">Признак включенного звукового сигнала.</param>
    /// <param name="enableOutlet1">Признак активной первой розетки.</param>
    /// <param name="deviceId">Идентификатор устройства в ViewPower.</param>
    /// <param name="outputVoltage">Текущее выходное напряжение.</param>
    /// <param name="inputVoltage">Текущее входное напряжение.</param>
    public ViewPowerMonitorSnapshot(
      string portName,
      string protocolType,
      string workMode,
      bool outputOn,
      bool buzzerOn,
      bool enableOutlet1,
      string deviceId,
      string outputVoltage,
      string inputVoltage)
    {
      PortName = portName;
      ProtocolType = protocolType;
      WorkMode = workMode;
      OutputOn = outputOn;
      BuzzerOn = buzzerOn;
      EnableOutlet1 = enableOutlet1;
      DeviceId = deviceId;
      OutputVoltage = outputVoltage;
      InputVoltage = inputVoltage;
    }

    /// <summary>
    /// Получает имя порта UPS в ViewPower.
    /// </summary>
    public string PortName { get; init; }

    /// <summary>
    /// Получает тип протокола UPS.
    /// </summary>
    public string ProtocolType { get; init; }

    /// <summary>
    /// Получает текущий режим работы UPS.
    /// </summary>
    public string WorkMode { get; init; }

    /// <summary>
    /// Получает признак включенного выходного питания.
    /// </summary>
    public bool OutputOn { get; init; }

    /// <summary>
    /// Получает признак включенного звукового сигнала.
    /// </summary>
    public bool BuzzerOn { get; init; }

    /// <summary>
    /// Получает признак активной первой розетки.
    /// </summary>
    public bool EnableOutlet1 { get; init; }

    /// <summary>
    /// Получает идентификатор устройства в ViewPower.
    /// </summary>
    public string DeviceId { get; init; }

    /// <summary>
    /// Получает текущее выходное напряжение.
    /// </summary>
    public string OutputVoltage { get; init; }

    /// <summary>
    /// Получает текущее входное напряжение.
    /// </summary>
    public string InputVoltage { get; init; }
  }
}
