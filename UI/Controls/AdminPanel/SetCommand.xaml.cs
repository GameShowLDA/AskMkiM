using Ask.Core.Shared.DTO.Devices;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace UI.Controls.AdminPanel
{
  public partial class SetCommand : UserControl
  {
    #region Data.
    private static readonly DeviceHelpInfo MkrHelp = new()
    {
      DeviceName = "MKR",
      Commands =
      [
        new()
        {
          Id = 1,
          Name = "Инициализация",
          Syntax = "1.0.0.a.",
          Variables = "a – состояние модуля (0 – инициализация, 1 – true, 2 – false)",
          Response = """
                      JSON:
                        ModuleName       – тип модуля (MKR)
                        NumberDevice     – номер устройства
                        NumberChassis    – номер шасси
                        NotDefaultState  – состояние устройства:
                                           false – устройство перезагрузилось (состояние по умолчанию)
                                           true  – начата работа с устройством
                      """,
          ResponseExample = "{\"ModuleName\":\"MKR\",\"NumberDevice\":6,\"NumberChassis\":1, \"NotDefaultState\": false }"
        },
        new()
        {
          Id = 2,
          Name = "Разомкнуть все реле",
          Syntax = "2.0.0.0.",
          Response = "-"
        },
        new()
        {
          Id = 4,
          Name = "Замкнуть или разомкнуть шину",
          Syntax = "4.a.b.c.",
          Variables = """
          a – выбор шины А или/и В (1,2,3)
          b – номер шины (1-4 низковольтные, 11-14 высоковольтные)
          c – замкнуть/разомкнуть (1,2)
          """,
          Response = "4.a.b.c."
        },
        new()
        {
          Id = 5,
          Name = "Замкнуть или разомкнуть измеритель",
          Syntax = "5.a.0.0.",
          Variables = "a – замкнуть/разомкнуть (1,2)",
          Response = "5.a."
        },
        new()
        {
          Id = 6,
          Name = "Самоконтроль точки МКР",
          Syntax = "6.a.0.0.",
          Variables = "a – номер точки МКР",
          Response = "JSON с отчетом"
        },
        new()
        {
          Id = 7,
          Name = "Получить ответ от измерителя",
          Syntax = "7.0.0.0.",
          Response = """
          7.1 – есть напряжение
          7.2 – нет напряжения
          """
        },
        new()
        {
          Id = 8,
          Name = "Замкнуть/разомкнуть точку",
          Syntax = "8.a.b.c.",
          Variables = """
          a – номер точки
          b – шина A/B/AB (1,2,3)
          c – замкнуть/разомкнуть (1,2)
          """,
          Response = "8.a.b.c."
        },
        new()
        {
          Id = 81,
          Name = "Перекинуть точку с одной шины на другую",
          Syntax = "81.a.b.0.",
          Variables = """
          a – номер точки
          b – шина, на которую замкнуть
          """,
          Response = "81.a.b.0."
        },
        new()
        {
          Id = 9,
          Name = "Подключить/отключить все точки к шине",
          Syntax = "9.a.b.0.",
          Variables = """
          a – номер шины A/B (1,2)
          b – замкнуть/разомкнуть (1,2)
          """,
          Response = "9.a.b.0."
        },
        new()
        {
          Id = 10,
          Name = "Самоконтроль внешних шин",
          Syntax = "10.a.0.0.",
          Variables = "a – номер шины (1-4)",
          Response = "JSON с отчетом"
        },
        new()
        {
          Id = 11,
          Name = "Подключить/отключить группу точек",
          Syntax = "11.a.b.c.",
          Variables = """
          a – начальная точка
          b – конечная точка
          c – AB + состояние (например 21 — подключить к B)
          """
        }
      ]
    };

    #endregion

    private string portInput = "8800";
    private string portOutput = "8888";
    private readonly Socket socket;

    private static readonly Dictionary<string, DeviceHelpInfo> HelpRegistry =
      new(StringComparer.OrdinalIgnoreCase)
      {
        ["MKR"] = MkrHelp
      };

    // История команд
    private readonly List<string> _history = new();
    private int _historyIndex = -1;
    public SetCommand()
    {
      InitializeComponent();

      // UDP-сокет один на весь класс
      socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    }



    private async void CommandInput_KeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Up)
      {
        if (_history.Count == 0)
          return;

        if (_historyIndex < 0)
          _historyIndex = _history.Count - 1;
        else if (_historyIndex > 0)
          _historyIndex--;

        CommandInput.Text = _history[_historyIndex];
        CommandInput.CaretIndex = CommandInput.Text.Length; // курсор в конец
        e.Handled = true;
        return;
      }

      if (e.Key == Key.Down)
      {
        if (_history.Count == 0)
          return;

        if (_historyIndex >= 0 && _historyIndex < _history.Count - 1)
          _historyIndex++;
        else
        {
          // после последней — очистить поле
          _historyIndex = -1;
          CommandInput.Text = "";
          e.Handled = true;
          return;
        }

        CommandInput.Text = _history[_historyIndex];
        CommandInput.CaretIndex = CommandInput.Text.Length;
        e.Handled = true;
        return;
      }

      // Ввод команды — ENTER
      if (e.Key != Key.Enter)
        return;

      var text = CommandInput.Text.Trim();
      if (string.IsNullOrWhiteSpace(text))
        return;

      // Добавляем в историю
      _history.Add(text);
      _historyIndex = -1;

      AddConsoleLine("> " + text, Brushes.DeepSkyBlue);

      CommandInput.Clear();
      e.Handled = true;

      await ProcessCommand(text);
    }

    private async Task ProcessCommand(string input)
    {
      try
      {
        if (string.IsNullOrWhiteSpace(input))
          return;

        input = input.Trim();

        if (input.StartsWith("help", StringComparison.OrdinalIgnoreCase))
        {
          var parts = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

          if (parts.Length == 1)
            ShowHelp();
          else
            ShowHelp(parts[1]); 
          return;
        }
        
        var partsCmd = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

        if (partsCmd.Length < 2)
        {
          AddConsoleLine("Неверный формат. Используй: IP COMMAND или help", Brushes.OrangeRed);
          return;
        }

        string ip = partsCmd[0];
        string command = partsCmd[1];

        await SendCommandAsync(ip, command);
      }
      catch (Exception ex)
      {
        AddConsoleLine("Ошибка: " + ex.Message, Brushes.Red);
      }
    }


    private async Task SendCommandAsync(string ip, string command)
    {
      try
      {
        // рассчитываем порты
        string lastByte = ip.Substring(ip.LastIndexOf('.') + 1);

        int po = 8888 + int.Parse(lastByte);
        int pi = 8800 + int.Parse(lastByte);

        portOutput = po.ToString();
        portInput = pi.ToString();

        AddConsoleLine($"PortOutput = {po}", Brushes.Gray);
        AddConsoleLine($"PortInput = {pi}", Brushes.Gray);

        await SendUdpAsync(command, ip);
      }
      catch (Exception e)
      {
        AddConsoleLine("Incorrect IP", Brushes.Red);
      }
    }

    private async Task SendUdpAsync(string msg, string ip)
    {
      IPAddress.TryParse(ip, out var address);

      var endpoint = new IPEndPoint(address, Convert.ToInt32(portOutput));
      var buffer = Encoding.UTF8.GetBytes(msg);

      // отправка команды
      await socket.SendToAsync(new ArraySegment<byte>(buffer), SocketFlags.None, endpoint);

      // получаем ответ
      string response = await GetMessageDeviceAsync();

      AddConsoleLine(response, Brushes.LightGreen);
    }

    private async Task<string> GetMessageDeviceAsync()
    {
      int port = int.Parse(portInput);

      using var udp = new UdpClient(port);

      try
      {
        Task<UdpReceiveResult> receiveTask = udp.ReceiveAsync();

        for (int i = 1; i <= 3; i++)
        {
          AddConsoleLine($"Ожидание ответа: {i} сек...", Brushes.Khaki, replaceLast: true);

          var delay = Task.Delay(1000);

          if (await Task.WhenAny(receiveTask, delay) == receiveTask)
          {
            var result = await receiveTask;

            string msg = Encoding.UTF8.GetString(result.Buffer);

            ClearLastStatusLine();

            return "Ответ от устройства: " + msg;
          }
        }

        ClearLastStatusLine();
        return "Устройство не ответило за 3 секунды.";
      }
      catch (Exception ex)
      {
        return "Ошибка получения ответа: " + ex.Message;
      }
    }

    // Механизм визуального вывода
    private TextBlock lastStatusLine = null;

    private void AddConsoleLine(string text, Brush color, bool replaceLast = false)
    {
      Dispatcher.Invoke(() =>
      {
        if (replaceLast && lastStatusLine != null)
        {
          lastStatusLine.Text = text;
          return;
        }

        var line = new TextBlock
        {
          Text = text,
          Foreground = color,
          FontFamily = new FontFamily("Consolas"),
          FontSize = 14,
          Margin = new Thickness(0, 0, 0, 4)
        };

        ConsolePanel.Children.Add(line);

        lastStatusLine = line;

        ConsoleScroll.ScrollToEnd();
      });
    }

    private void ClearLastStatusLine()
    {
      Dispatcher.Invoke(() =>
      {
        if (lastStatusLine != null)
          lastStatusLine.Text = "";
      });
    }

    private void ShowHelp(string? device = null)
    {
      if (string.IsNullOrWhiteSpace(device))
      {
        AddConsoleLine("Доступные устройства:", Brushes.LightGray);

        foreach (var name in HelpRegistry.Keys)
          AddConsoleLine($"  • {name}", Brushes.LightGray);

        AddConsoleLine("Используй: help <DEVICE>", Brushes.Gray);
        return;
      }

      if (!HelpRegistry.TryGetValue(device, out var info))
      {
        AddConsoleLine($"Устройство '{device}' не найдено", Brushes.OrangeRed);
        return;
      }

      AddConsoleLine($"Команды устройства {info.DeviceName}", Brushes.LightSkyBlue);
      AddConsoleLine(new string('─', 40), Brushes.DarkGray);

      foreach (var cmd in info.Commands)
      {
        AddConsoleLine($"[{cmd.Id}] {cmd.Name}", Brushes.LightGreen);
        AddConsoleLine($"  Синтаксис   : {cmd.Syntax}", Brushes.Gray);

        if (!string.IsNullOrWhiteSpace(cmd.Variables) && cmd.Variables != "-")
        {
          AddConsoleLine($"  Переменные  :", Brushes.Gray);

          foreach (var line in cmd.Variables
                       .Split('\n', StringSplitOptions.RemoveEmptyEntries))
          {
            AddConsoleLine($"    {line.Trim()}", Brushes.Gray);
          }
        }

        if (!string.IsNullOrWhiteSpace(cmd.Response) && cmd.Response != "-")
        {
          AddConsoleLine($"  Ответ       :", Brushes.Gray);

          foreach (var line in cmd.Response
                       .Split('\n', StringSplitOptions.RemoveEmptyEntries))
          {
            AddConsoleLine($"    {line.Trim()}", Brushes.Gray);
          }
        }

        if (!string.IsNullOrWhiteSpace(cmd.ResponseExample) && cmd.ResponseExample != "-")
        {
          AddConsoleLine($"  Пример ответа:", Brushes.DeepSkyBlue);

          foreach (var line in cmd.ResponseExample
                       .Split('\n', StringSplitOptions.RemoveEmptyEntries))
          {
            AddConsoleLine($"    {line}", Brushes.LightSteelBlue);
          }
        }

        AddConsoleLine("", Brushes.Gray);
      }
    }

  }
}
