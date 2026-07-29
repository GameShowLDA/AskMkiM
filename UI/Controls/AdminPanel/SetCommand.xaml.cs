using Ask.LogLib;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using UI.Controls.AdminPanel.Commands;

namespace UI.Controls.AdminPanel
{
  /// <summary>
  /// Предоставляет сервисную консоль, журнал приложения и ручной обмен UDP-командами.
  /// </summary>
  public partial class SetCommand : UserControl
  {
    private const int MaxConsoleLines = 2000;
    private const int MaxCommandOutputLength = 65536;
    private readonly List<string> history = [];
    private readonly List<ServiceDeviceAddressInfo> configuredDevices = [];
    private readonly Func<Task<IReadOnlyList<ServiceDeviceAddressInfo>>> addressesProvider;
    private int historyIndex = -1;
    private bool suppressAutocomplete;
    private bool commandInProgress;

    /// <summary>
    /// Инициализирует сервисную командную консоль.
    /// </summary>
    /// <param name="addressesProvider">Функция получения адресов настроенного оборудования.</param>
    public SetCommand(Func<Task<IReadOnlyList<ServiceDeviceAddressInfo>>> addressesProvider)
    {
      this.addressesProvider = addressesProvider
        ?? throw new ArgumentNullException(nameof(addressesProvider));
      InitializeComponent();
      Loaded += SetCommand_Loaded;
      Unloaded += SetCommand_Unloaded;
    }

    private async void SetCommand_Loaded(object sender, RoutedEventArgs e)
    {
      LoggerUtility.LogMessageWritten -= LoggerUtility_LogMessageWritten;
      LoggerUtility.LogMessageWritten += LoggerUtility_LogMessageWritten;
      try
      {
        configuredDevices.Clear();
        configuredDevices.AddRange((await addressesProvider())
          .Where(device => IPAddress.TryParse(device.Address, out _))
          .DistinctBy(device => (
            device.Address,
            device.ChassisNumber,
            device.ModuleNumber)));
      }
      catch (Exception exception)
      {
        AddConsoleLine($"Не удалось загрузить адреса оборудования: {exception.Message}", Brushes.Khaki);
      }
    }

    private void SetCommand_Unloaded(object sender, RoutedEventArgs e)
    {
      LoggerUtility.LogMessageWritten -= LoggerUtility_LogMessageWritten;
    }

    private void LoggerUtility_LogMessageWritten(object? sender, ApplicationLogMessageEventArgs e)
    {
      string levelLabel = e.Level switch
      {
        ApplicationLogLevel.Debug => "DBG",
        ApplicationLogLevel.Information => "INF",
        ApplicationLogLevel.Warning => "WRN",
        ApplicationLogLevel.Error => "ERR",
        _ => "LOG"
      };
      Brush color = e.Level switch
      {
        ApplicationLogLevel.Debug => Brushes.SlateGray,
        ApplicationLogLevel.Information => Brushes.LightGray,
        ApplicationLogLevel.Warning => Brushes.Khaki,
        ApplicationLogLevel.Error => Brushes.LightCoral,
        _ => Brushes.LightGray
      };
      string deviceLabel = e.IsDeviceLog ? " DEV" : string.Empty;
      AddConsoleLine($"{e.Timestamp:HH:mm:ss.fff} [{levelLabel}{deviceLabel}] {e.Message}", color);
    }

    private void CommandInput_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (suppressAutocomplete)
      {
        return;
      }

      IReadOnlyList<ConsoleSuggestion> suggestions = BuildSuggestions(CommandInput.Text);
      AutocompleteBox.ItemsSource = suggestions;
      AutocompleteBox.Visibility = suggestions.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
      AutocompleteBox.SelectedIndex = suggestions.Count > 0 ? 0 : -1;
    }

    private IReadOnlyList<ConsoleSuggestion> BuildSuggestions(string input)
    {
      string text = input.TrimStart();
      var suggestions = new List<ConsoleSuggestion>();

      if (string.IsNullOrWhiteSpace(text))
      {
        suggestions.Add(new("help — список устройств", "help"));
        suggestions.Add(new("clear — очистить консоль", "clear"));
        suggestions.Add(new("ping IP — проверить доступность устройства", "ping "));
        suggestions.Add(new("cmd COMMAND — выполнить системную команду", "cmd "));
        suggestions.AddRange(configuredDevices
          .Select(device => new ConsoleSuggestion(device.DisplayText, $"{device.Address} ")));
        suggestions.AddRange(history.TakeLast(6).Reverse()
          .Select(item => new ConsoleSuggestion($"История: {item}", item)));
        return suggestions;
      }

      if ("help".StartsWith(text, StringComparison.OrdinalIgnoreCase))
      {
        suggestions.Add(new("help — список устройств", "help"));
      }

      if ("ping".StartsWith(text, StringComparison.OrdinalIgnoreCase))
      {
        suggestions.Add(new("ping IP — проверить доступность устройства", "ping "));
      }

      if ("cmd".StartsWith(text, StringComparison.OrdinalIgnoreCase))
      {
        suggestions.Add(new("cmd COMMAND — выполнить системную команду", "cmd "));
      }

      if (text.StartsWith("ping ", StringComparison.OrdinalIgnoreCase))
      {
        string filter = text[5..].Trim();
        suggestions.AddRange(configuredDevices
          .Where(device =>
            device.Address.StartsWith(filter, StringComparison.OrdinalIgnoreCase)
            || device.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || device.ModuleNumber.ToString().StartsWith(filter, StringComparison.OrdinalIgnoreCase))
          .Select(device => new ConsoleSuggestion(
            $"Проверить доступность · {device.DisplayText}",
            $"ping {device.Address}")));
        return suggestions;
      }

      if (text.StartsWith("help ", StringComparison.OrdinalIgnoreCase))
      {
        string filter = text[5..].Trim();
        suggestions.AddRange(new[] { "console", "all" }
          .Concat(DeviceCommandCatalog.DeviceAliases)
          .Where(alias => alias.StartsWith(filter, StringComparison.OrdinalIgnoreCase))
          .Select(alias => new ConsoleSuggestion(
            alias.Equals("all", StringComparison.OrdinalIgnoreCase)
              ? "Полная справка по всем функциям"
              : $"Справка по {alias}",
            $"help {alias}")));
        return suggestions;
      }

      string[] parts = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
      if (parts.Length == 1 && !IPAddress.TryParse(parts[0], out _))
      {
        suggestions.AddRange(configuredDevices
          .Where(device =>
            device.Address.StartsWith(text, StringComparison.OrdinalIgnoreCase)
            || device.Name.Contains(text, StringComparison.OrdinalIgnoreCase))
          .Select(device => new ConsoleSuggestion(device.DisplayText, $"{device.Address} ")));
        suggestions.AddRange(history
          .Where(item => item.StartsWith(text, StringComparison.OrdinalIgnoreCase))
          .TakeLast(8)
          .Reverse()
          .Select(item => new ConsoleSuggestion($"История: {item}", item)));
        return suggestions.Take(10).ToList();
      }

      if (IPAddress.TryParse(parts[0], out _))
      {
        string commandFilter = parts.Length == 2 ? parts[1] : string.Empty;
        suggestions.AddRange(DeviceCommandCatalog.GetCommands()
          .Where(item =>
            string.IsNullOrEmpty(commandFilter)
            || item.Command.Syntax.StartsWith(commandFilter, StringComparison.OrdinalIgnoreCase)
            || item.Command.Name.Contains(commandFilter, StringComparison.OrdinalIgnoreCase))
          .Take(12)
          .Select(item => new ConsoleSuggestion(
            $"{item.Alias} · {item.Command.Name} · {item.Command.Syntax}",
            $"{parts[0]} {item.Command.Syntax}")));
      }

      return suggestions;
    }

    private async void CommandInput_KeyDown(object sender, KeyEventArgs e)
    {
      if (AutocompleteBox.Visibility == Visibility.Visible)
      {
        if (e.Key == Key.Down)
        {
          AutocompleteBox.SelectedIndex = Math.Min(
            AutocompleteBox.Items.Count - 1,
            AutocompleteBox.SelectedIndex + 1);
          AutocompleteBox.ScrollIntoView(AutocompleteBox.SelectedItem);
          e.Handled = true;
          return;
        }

        if (e.Key == Key.Up && AutocompleteBox.SelectedIndex > 0)
        {
          AutocompleteBox.SelectedIndex--;
          AutocompleteBox.ScrollIntoView(AutocompleteBox.SelectedItem);
          e.Handled = true;
          return;
        }

        if (e.Key == Key.Tab)
        {
          ApplySelectedSuggestion();
          e.Handled = true;
          return;
        }

        if (e.Key == Key.Escape)
        {
          AutocompleteBox.Visibility = Visibility.Collapsed;
          e.Handled = true;
          return;
        }
      }

      if (e.Key == Key.Up)
      {
        NavigateHistory(-1);
        e.Handled = true;
        return;
      }

      if (e.Key == Key.Down && AutocompleteBox.Visibility != Visibility.Visible)
      {
        NavigateHistory(1);
        e.Handled = true;
        return;
      }

      if (e.Key != Key.Enter || commandInProgress)
      {
        return;
      }

      string text = CommandInput.Text.Trim();
      if (string.IsNullOrWhiteSpace(text))
      {
        return;
      }

      history.Add(text);
      historyIndex = -1;
      suppressAutocomplete = true;
      CommandInput.Clear();
      suppressAutocomplete = false;
      AutocompleteBox.Visibility = Visibility.Collapsed;
      e.Handled = true;
      await ProcessCommandAsync(text);
    }

    private void NavigateHistory(int direction)
    {
      if (history.Count == 0)
      {
        return;
      }

      historyIndex = historyIndex < 0
        ? history.Count - 1
        : Math.Clamp(historyIndex + direction, 0, history.Count - 1);
      SetCommandInput(history[historyIndex]);
    }

    private void AutocompleteBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key is Key.Enter or Key.Tab)
      {
        ApplySelectedSuggestion();
        CommandInput.Focus();
        e.Handled = true;
      }
      else if (e.Key == Key.Escape)
      {
        AutocompleteBox.Visibility = Visibility.Collapsed;
        CommandInput.Focus();
        e.Handled = true;
      }
    }

    private void AutocompleteBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      ApplySelectedSuggestion();
      CommandInput.Focus();
    }

    private void ApplySelectedSuggestion()
    {
      if (AutocompleteBox.SelectedItem is ConsoleSuggestion suggestion)
      {
        SetCommandInput(suggestion.InsertText);
        AutocompleteBox.Visibility = Visibility.Collapsed;
      }
    }

    private void SetCommandInput(string text)
    {
      suppressAutocomplete = true;
      CommandInput.Text = text;
      CommandInput.CaretIndex = text.Length;
      suppressAutocomplete = false;
    }

    private async Task ProcessCommandAsync(string input)
    {
      if (input.Equals("clear", StringComparison.OrdinalIgnoreCase))
      {
        ConsolePanel.Children.Clear();
        EmptyConsoleHint.Visibility = Visibility.Visible;
        return;
      }

      if (input.StartsWith("help", StringComparison.OrdinalIgnoreCase))
      {
        string[] helpParts = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        ShowHelp(helpParts.Length == 2 ? helpParts[1] : null);
        return;
      }

      if (input.StartsWith("ping ", StringComparison.OrdinalIgnoreCase))
      {
        string addressText = input[5..].Trim();
        if (!IPAddress.TryParse(addressText, out IPAddress? pingAddress))
        {
          AddConsoleLine("Формат: ping IP. Пример: ping 192.168.1.20", Brushes.OrangeRed);
          return;
        }

        await PingAsync(pingAddress);
        return;
      }

      if (input.StartsWith("cmd ", StringComparison.OrdinalIgnoreCase))
      {
        string command = input[4..].Trim();
        if (string.IsNullOrWhiteSpace(command))
        {
          AddConsoleLine("Формат: cmd COMMAND. Пример: cmd ipconfig", Brushes.OrangeRed);
          return;
        }

        await ExecuteSystemCommandAsync(command);
        return;
      }

      string[] parts = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
      if (parts.Length < 2 || !IPAddress.TryParse(parts[0], out IPAddress? address))
      {
        AddConsoleLine("Формат: IP COMMAND. Пример: 192.168.1.20 4.2.21.1.", Brushes.OrangeRed);
        return;
      }

      await SendCommandAsync(address, parts[1]);
    }

    /// <summary>
    /// Выполняет команду через системный интерпретатор Windows и отображает её вывод.
    /// </summary>
    /// <param name="command">Командная строка, передаваемая в <c>cmd.exe</c>.</param>
    private async Task ExecuteSystemCommandAsync(string command)
    {
      var exchange = new CommandExchangeViewModel
      {
        Endpoint = $"Локальный CMD · {Environment.MachineName}",
        Command = command
      };
      AddExchange(exchange);
      var stopwatch = Stopwatch.StartNew();
      commandInProgress = true;

      try
      {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding commandEncoding = Encoding.GetEncoding(
          System.Globalization.CultureInfo.CurrentCulture.TextInfo.OEMCodePage);
        var startInfo = new ProcessStartInfo
        {
          FileName = "cmd.exe",
          UseShellExecute = false,
          RedirectStandardOutput = true,
          RedirectStandardError = true,
          StandardOutputEncoding = commandEncoding,
          StandardErrorEncoding = commandEncoding,
          CreateNoWindow = true,
          WorkingDirectory = Environment.CurrentDirectory
        };
        startInfo.ArgumentList.Add("/d");
        startInfo.ArgumentList.Add("/s");
        startInfo.ArgumentList.Add("/c");
        startInfo.ArgumentList.Add(command);

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
          throw new InvalidOperationException("Не удалось запустить cmd.exe.");
        }

        Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
        Task<string> errorTask = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
          await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
          if (!process.HasExited)
          {
            process.Kill(entireProcessTree: true);
          }

          await process.WaitForExitAsync();
          exchange.Response = "Выполнение остановлено: превышен таймаут 30 секунд.";
          exchange.StatusBrush = Brushes.Khaki;
          return;
        }

        string output = (await outputTask).Trim();
        string error = (await errorTask).Trim();
        string response = string.Join(
          Environment.NewLine,
          new[] { output, error }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        if (string.IsNullOrWhiteSpace(response))
        {
          response = "Команда завершена без текстового вывода.";
        }

        exchange.Response = LimitOutput(response);
        exchange.StatusBrush = process.ExitCode == 0
          ? Brushes.LightGreen
          : Brushes.LightCoral;
        exchange.Duration = $"код {process.ExitCode} · {stopwatch.ElapsedMilliseconds} мс";
      }
      catch (Exception exception)
      {
        exchange.Response = $"Ошибка CMD: {exception.Message}";
        exchange.StatusBrush = Brushes.LightCoral;
      }
      finally
      {
        stopwatch.Stop();
        if (string.IsNullOrEmpty(exchange.Duration))
        {
          exchange.Duration = $"{stopwatch.ElapsedMilliseconds} мс";
        }

        commandInProgress = false;
        RefreshExchangeList();
      }
    }

    private static string LimitOutput(string output)
    {
      if (output.Length <= MaxCommandOutputLength)
      {
        return output;
      }

      return $"{output[..MaxCommandOutputLength]}{Environment.NewLine}… вывод сокращён";
    }

    /// <summary>
    /// Проверяет сетевую доступность устройства и отображает результат в карточке обмена.
    /// </summary>
    /// <param name="address">IP-адрес проверяемого устройства.</param>
    private async Task PingAsync(IPAddress address)
    {
      var exchange = new CommandExchangeViewModel
      {
        Endpoint = address.ToString(),
        Command = $"ping {address}"
      };
      AddExchange(exchange);
      var stopwatch = Stopwatch.StartNew();
      commandInProgress = true;

      try
      {
        using var ping = new Ping();
        PingReply reply = await ping.SendPingAsync(address, 3000);
        if (reply.Status == IPStatus.Success)
        {
          exchange.Response = $"Доступен · {reply.RoundtripTime} мс · TTL {reply.Options?.Ttl}";
          exchange.StatusBrush = Brushes.LightGreen;
        }
        else
        {
          exchange.Response = $"Недоступен · {reply.Status}";
          exchange.StatusBrush = Brushes.Khaki;
        }
      }
      catch (Exception exception)
      {
        exchange.Response = $"Ошибка ping: {exception.Message}";
        exchange.StatusBrush = Brushes.LightCoral;
      }
      finally
      {
        stopwatch.Stop();
        exchange.Duration = $"{stopwatch.ElapsedMilliseconds} мс";
        commandInProgress = false;
        RefreshExchangeList();
      }
    }

    private async Task SendCommandAsync(IPAddress address, string command)
    {
      byte lastOctet = address.GetAddressBytes()[^1];
      int outputPort = 8888 + lastOctet;
      int inputPort = 8800 + lastOctet;
      var exchange = new CommandExchangeViewModel
      {
        Endpoint = $"{address}:{outputPort}",
        Command = command
      };
      AddExchange(exchange);
      var stopwatch = Stopwatch.StartNew();
      commandInProgress = true;

      try
      {
        using var receiver = new UdpClient(inputPort);
        using var sender = new UdpClient();
        byte[] buffer = Encoding.UTF8.GetBytes(command);
        Task<UdpReceiveResult> receiveTask = receiver.ReceiveAsync();
        await sender.SendAsync(buffer, buffer.Length, new IPEndPoint(address, outputPort));

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        UdpReceiveResult response = await receiveTask.WaitAsync(timeout.Token);
        exchange.Response = Encoding.UTF8.GetString(response.Buffer);
        exchange.StatusBrush = Brushes.LightGreen;
      }
      catch (OperationCanceledException)
      {
        exchange.Response = "Устройство не ответило за 3 секунды.";
        exchange.StatusBrush = Brushes.Khaki;
      }
      catch (Exception exception)
      {
        exchange.Response = $"Ошибка: {exception.Message}";
        exchange.StatusBrush = Brushes.LightCoral;
      }
      finally
      {
        stopwatch.Stop();
        exchange.Duration = $"{stopwatch.ElapsedMilliseconds} мс";
        commandInProgress = false;
        RefreshExchangeList();
      }
    }

    private void AddExchange(CommandExchangeViewModel exchange)
    {
      var card = new ContentControl
      {
        Content = exchange,
        ContentTemplate = (DataTemplate)FindResource("CommandExchangeTemplate")
      };
      ConsolePanel.Children.Add(card);
      TrimConsole();

      EmptyConsoleHint.Visibility = Visibility.Collapsed;
      ConsoleScroll.ScrollToEnd();
    }

    private void RefreshExchangeList()
    {
      ConsoleScroll.ScrollToEnd();
    }

    private void AddConsoleLine(string text, Brush color)
    {
      if (!Dispatcher.CheckAccess())
      {
        Dispatcher.BeginInvoke(() => AddConsoleLine(text, color));
        return;
      }

      EmptyConsoleHint.Visibility = Visibility.Collapsed;
      var line = new TextBlock
      {
        Text = text,
        Foreground = color,
        FontFamily = new FontFamily("Consolas"),
        FontSize = 16,
        Margin = new Thickness(0, 0, 0, 4),
        TextWrapping = TextWrapping.Wrap
      };
      ConsolePanel.Children.Add(line);
      TrimConsole();

      ConsoleScroll.ScrollToEnd();
    }

    private void TrimConsole()
    {
      while (ConsolePanel.Children.Count > MaxConsoleLines)
      {
        ConsolePanel.Children.RemoveAt(0);
      }
    }

    private void ShowHelp(string? device = null)
    {
      if (string.IsNullOrWhiteSpace(device))
      {
        ShowConsoleHelp();
        AddConsoleLine("", Brushes.Gray);
        AddConsoleLine("Справочники оборудования", Brushes.LightSkyBlue);
        foreach (string alias in DeviceCommandCatalog.DeviceAliases)
        {
          if (DeviceCommandCatalog.TryGetDevice(alias, out var help))
          {
            AddConsoleLine(
              $"  help {alias,-4} — {help.DeviceName}, функций: {help.Commands.Count}",
              Brushes.LightGray);
          }
        }

        AddConsoleLine("  help all  — все функции всех устройств", Brushes.LightGray);
        return;
      }

      if (device.Equals("console", StringComparison.OrdinalIgnoreCase))
      {
        ShowConsoleHelp();
        return;
      }

      if (device.Equals("all", StringComparison.OrdinalIgnoreCase))
      {
        ShowConsoleHelp();
        foreach (string alias in DeviceCommandCatalog.DeviceAliases)
        {
          AddConsoleLine("", Brushes.Gray);
          ShowDeviceHelp(alias);
        }

        return;
      }

      ShowDeviceHelp(device);
    }

    private void ShowConsoleHelp()
    {
      AddConsoleLine("Функции командной консоли", Brushes.LightSkyBlue);
      AddConsoleLine("  help                 — краткая справка", Brushes.LightGreen);
      AddConsoleLine("  help console         — команды и горячие клавиши консоли", Brushes.LightGreen);
      AddConsoleLine("  help <MKR|DBC|MS>    — функции выбранного устройства", Brushes.LightGreen);
      AddConsoleLine("  help all             — полная справка", Brushes.LightGreen);
      AddConsoleLine("  clear                — очистить журнал и карточки обмена", Brushes.LightGreen);
      AddConsoleLine("  ping <IP>            — проверить доступность, задержку и TTL", Brushes.LightGreen);
      AddConsoleLine("  cmd <COMMAND>        — выполнить локальную команду Windows", Brushes.LightGreen);
      AddConsoleLine("  <IP> <COMMAND>       — отправить низкоуровневую UDP-команду", Brushes.LightGreen);
      AddConsoleLine("", Brushes.Gray);
      AddConsoleLine("Горячие клавиши", Brushes.LightSkyBlue);
      AddConsoleLine("  Tab                  — применить выбранную подсказку", Brushes.Gray);
      AddConsoleLine("  ↑ / ↓                — подсказки или история команд", Brushes.Gray);
      AddConsoleLine("  Esc                  — закрыть автодополнение", Brushes.Gray);
      AddConsoleLine("  Enter                — выполнить команду", Brushes.Gray);
    }

    private void ShowDeviceHelp(string device)
    {
      if (!DeviceCommandCatalog.TryGetDevice(device, out var help))
      {
        AddConsoleLine($"Устройство «{device}» отсутствует в каталоге.", Brushes.OrangeRed);
        return;
      }

      AddConsoleLine($"Команды устройства {help.DeviceName}", Brushes.LightSkyBlue);
      foreach (var command in help.Commands)
      {
        AddConsoleLine($"[{command.Id}] {command.Name}", Brushes.LightGreen);
        AddConsoleLine($"  Синтаксис: {command.Syntax}", Brushes.Gray);
        if (command.Variables != "-")
        {
          AddConsoleLine($"  Параметры: {command.Variables}", Brushes.DarkGray);
        }

        if (command.Response != "-")
        {
          AddConsoleLine($"  Ответ: {command.Response}", Brushes.DarkGray);
        }
      }
    }
  }
}
