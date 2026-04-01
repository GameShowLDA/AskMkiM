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
using System.Windows.Shapes;

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
          Name = "Сброс коммутатора",
          Syntax = "2.1.0.0.",
          Response = """
                      JSON:
                        ModuleName       – тип модуля (MKR)
                        NumberDevice     – номер устройства
                        NumberChassis    – номер шасси
                        Answer           - результат выполнения (2.0.1)
                        NotDefaultState  – состояние устройства:
                                           false – устройство перезагрузилось (состояние по умолчанию)
                                           true  – начата работа с устройством
                      """,
          ResponseExample = "{\"ModuleName\":\"MKR\",\"NumberDevice\":6,\"NumberChassis\":1,\"Answer\":\"2.0.1\", \"NotDefaultState\": false}"
        },
        new()
        {
          Id = 4,
          Name = "Замкнуть или разомкнуть шину",
          Syntax = "4.a.b.c.",
          Variables = """
                      a – выбор шины A или/и B:
                          1 – шина A
                          2 – шина B
                          3 – шины A и B
                      b – номер шины:
                          1–4   – низковольтные
                          11–14 – высоковольтные
                      c – состояние:
                          1 – замкнуть
                          2 – разомкнуть
                      """,
          Response = """
                      JSON:
                        ModuleName       – тип модуля (MKR)
                        NumberDevice     – номер устройства
                        NumberChassis    – номер шасси
                        Answer           – подтверждение выполненной команды (4.a.b.c)
                      """,
          ResponseExample = "{\"ModuleName\":\"MKR\",\"NumberDevice\":6,\"NumberChassis\":1,\"Answer\":\"4.a.b.c\"}"
        },
        new()
        {
          Id = 5,
          Name = "Включить или отключить измеритель",
          Syntax = "5.a.0.0.",
          Variables = """
                      a – состояние измерителя:
                          1 – включить
                          2 – отключить
                      """,
          Response = """
                      JSON:
                        ModuleName       – тип модуля (MKR)
                        NumberDevice     – номер устройства
                        NumberChassis    – номер шасси
                        Answer           – подтверждение выполненной команды (5.a)
                      """,
          ResponseExample =
            "{\"ModuleName\":\"MKR\",\"NumberDevice\":6,\"NumberChassis\":1,\"Answer\":\"5.a\"}"
        },
        new()
        {
          Id = 6,
          Name = "Самоконтроль точки МКР",
          Syntax = "6.a.0.0.",
          Variables = """
                      a – номер точки МКР
                      """,
          Response = """
                      JSON с отчетом самоконтроля.
        
                      Поля отчета:
                        ModuleName     – тип модуля (MKR)
                        NumberDevice   – номер устройства
                        NumberChassis  – номер шасси
                        Status         – результат выполнения:
                                         success – самоконтроль выполнен успешно
                        NumberPoint    – номер проверяемой точки
                        ConnectPoint   – результат подключения точки
                        DisconnectBusA – результат отключения шины A
                        DisconnectBusB – результат отключения шины B
                        SelfControl    – общий результат самоконтроля
                      """,
          ResponseExample =
            "{\"ModuleName\":\"MKR\",\"NumberDevice\":6,\"NumberChassis\":1,\"Status\":\"success\",\"NumberPoint\":1,\"ConnectPoint\":true,\"DisconnectBusA\":true,\"DisconnectBusB\":true,\"SelfControl\":true}"
        },
        new()
        {
          Id = 7,
          Name = "Получить ответ от измерителя",
          Syntax = "7.0.0.0.",
          Variables = "-",
          Response = """
                      JSON:
        
                        ModuleName       – тип модуля (MKR)
                        NumberDevice     – номер устройства
                        NumberChassis    – номер шасси
                        Answer           – результат измерения:
                                           7.1 – есть напряжение
                                           7.2 – нет напряжения
                      """,
          ResponseExample =
            "{\"ModuleName\":\"MKR\",\"NumberDevice\":6,\"NumberChassis\":1,\"Answer\":\"7.1\"}"
        },
        new()
        {
          Id = 8,
          Name = "Подлючить или отключить точку",
          Syntax = "8.a.b.c.",
          Variables = """
                      a – номер точки
                      b – шина:
                          1 – A
                          2 – B
                          3 – A и B
                      c – состояние:
                          1 – подлючить
                          2 – отключить
                      """,
          Response = """
                      JSON:
                        ModuleName       – тип модуля (MKR)
                        NumberDevice     – номер устройства
                        NumberChassis    – номер шасси
                        Answer           – подтверждение выполненной команды (8.a.b.c)
                      """,
          ResponseExample =
            "{\"ModuleName\":\"MKR\",\"NumberDevice\":6,\"NumberChassis\":1,\"Answer\":\"8.a.b.c\"}"
        },
        new()
        {
          Id = 81,
          Name = "Переподлючить точку с одной шины на другую",
          Syntax = "81.a.b.0.",
          Variables = """
                      a – номер точки
                      b – шина, на которую необходимо подключить точку:
                          1 – шина A
                          2 – шина B
                      """,
          Response = """
                      JSON:
                        ModuleName       – тип модуля (MKR)
                        NumberDevice     – номер устройства
                        NumberChassis    – номер шасси
                        Answer           – подтверждение выполненной команды (81.a.b.0)
                      """,
          ResponseExample =
            "{\"ModuleName\":\"MKR\",\"NumberDevice\":6,\"NumberChassis\":1,\"Answer\":\"81.a.b.0\"}"
        },
        new()
        {
          Id = 82,
          Name = "Подключить/отключить точку с контролем подключения",
          Syntax = "82.a.b.с.",
          Variables = """
                      a – номер точки
                      b – шина, на которую необходимо подключить точку:
                          1 – шина A
                          2 – шина B
                      c – состояние:
                          1 – подлючить
                          2 – отключить
                      """,
          Response = """
                      JSON:
                        ModuleName       – тип модуля (MKR)
                        NumberDevice     – номер устройства
                        NumberChassis    – номер шасси
                        Answer           – подтверждение выполненной команды (82.a.b.c)
                        Checked         – результат проверки подключения:
                                           true  – точка успешно подключена/отключена
                                           false – не удалось подключить/отключить точку 
                      """,
          ResponseExample =
            "{\"ModuleName\":\"MKR\",\"NumberDevice\":6,\"NumberChassis\":1,\"Checked\":true,\"Answer\":\"82.a.b.c\"}"
        },
        new()
        {
          Id = 9,
          Name = "Подключить или отключить все точки к шине",
          Syntax = "9.a.b.0.",
          Variables = """
                      a – шина:
                          1 – A
                          2 – B
                      b – состояние:
                          1 – подключить
                          2 – отключить
                      """,
          Response = """
                      JSON:
                        ModuleName       – тип модуля (MKR)
                        NumberDevice     – номер устройства
                        NumberChassis    – номер шасси
                        Answer           – подтверждение выполненной команды (9.a.b)
                      """,
          ResponseExample =
            "{\"ModuleName\":\"MKR\",\"NumberDevice\":6,\"NumberChassis\":1,\"Answer\":\"9.a.b\"}"
        },
        new()
        {
          Id = 10,
          Name = "Самоконтроль внешних шин",
          Syntax = "10.a.0.0.",
          Variables = """
                      a – номер внешней шины (1–4)
                      """,
          Response = """
                      JSON с отчетом самоконтроля внешней шины.
        
                      Поля отчета:
                        ModuleName        – тип модуля (MKR)
                        NumberDevice      – номер устройства
                        NumberChassis     – номер шасси
                        NumberBus         – номер проверяемой шины
                        ProtectReleBusA   – номер защитного реле шины A
                        ProtectReleBusB   – номер защитного реле шины B
                        ConnectProtect    – состояние защитных реле:
                                             true  – подключены
                                             false – отключены
                        MainReleBusA      – номер главного реле шины A
                        MainReleBusB      – номер главного реле шины B
                        ConnectMain       – состояние главных реле:
                                             true  – подключены
                                             false – отключены
                        Error             – код ошибки:
                                             0 – ошибок не обнаружено
                      """,
          ResponseExample =
            "{\"ModuleName\":\"MKR\",\"NumberDevice\":6,\"NumberChassis\":1,\"NumberBus\":1,\"ProtectReleBusA\":101,\"ProtectReleBusB\":111,\"ConnectProtect\":false,\"MainReleBusA\":102,\"MainReleBusB\":112,\"ConnectMain\":false,\"Error\":0}"
        },
        new()
        {
          Id = 11,
          Name = "Подключить или отключить группу точек",
          Syntax = "11.a.b.c.",
          Variables = """
                      a – начальная точка диапазона
                      b – конечная точка диапазона
                      c – код операции:
                          первая цифра – шина:
                              1 – A
                              2 – B
                          вторая цифра – состояние:
                              1 – подключить
                              2 – отключить
                          пример:
                              21 – подключить точки к шине B
                      """,
          Response = """
                      JSON:
                        ModuleName       – тип модуля (MKR)
                        NumberDevice     – номер устройства
                        NumberChassis    – номер шасси
                        Answer           – подтверждение выполненной команды (11.a.b.c)
                      """,
          ResponseExample =
            "{\"ModuleName\":\"MKR\",\"NumberDevice\":6,\"NumberChassis\":1,\"Answer\":\"11.a.b.c\"}"
        }
      ]
    };
    private static readonly DeviceHelpInfo DBCHelp = new()
    {
      DeviceName = "DeviceBusCommutation",
      Commands =
      [
        new()
        {
          Id = 1,
          Name = "Инициализация",
          Syntax = "1.0.0.0.",
          Variables = "-",
          Response = """
                      JSON:
                        ModuleName       – тип модуля (DeviceBusCommutation)
                        NumberDevice     – номер устройства
                        NumberChassis    – номер шасси
                      """,
          ResponseExample =
            "{\"ModuleName\":\"DeviceBusCommutation\",\"NumberDevice\":20,\"NumberChassis\":1}"
        },

        new()
        {
          Id = 2,
          Name = "Сброс всех реле",
          Syntax = "2.1.0.0.",
          Variables = "-",
          Response = """
                      JSON:
                        ModuleName       – тип модуля (DeviceBusCommutation)
                        NumberDevice     – номер устройства
                        NumberChassis    – номер шасси
                        Answer           – результат выполнения (2.0.1)
                      """,
          ResponseExample =
            "{\"ModuleName\":\"DeviceBusCommutation\",\"NumberDevice\":20,\"NumberChassis\":1,\"Answer\":\"2.0.1\"}"
        },

        new()
        {
          Id = 4,
          Name = "Проверка цепочек для самоконтроля (мультиметр)",
          Syntax = "4.a.b.c.",
          Variables = """
                      a – что проверяем:
                          1 – блокировочные реле
                          2 – мультиметр
                          3 – АЦП
                          4 – АЦП с переполюсовкой
                          5 – ПИНТ
                          6 – шунт
                          7 – ППУ

                      b – выбор шины и контакта:
                          первая цифра – шина (1 – A, 2 – B)
                          вторая цифра – контакт (1–4)
                          при проверке шунта – одна цифра (1–2)

                      c – действие:
                          1 – замкнуть
                          2 – разомкнуть
                      """,
          Response = """
                      JSON:
                        ModuleName       – тип модуля (DeviceBusCommutation)
                        NumberDevice     – номер устройства
                        NumberChassis    – номер шасси
                        Answer           – подтверждение выполненной команды (4.a.b.c)
                      """,
          ResponseExample =
            "{\"ModuleName\":\"DeviceBusCommutation\",\"NumberDevice\":20,\"NumberChassis\":1,\"Answer\":\"4.2.21.1\"}"
        },

        new()
        {
          Id = 41,
          Name = "Проверка главных реле в цепочках",
          Syntax = "41.a.b.c.",
          Variables = """
                      a – тип цепочки (см. команду 4) + номер реле.
                          Если a = 0, устройство возвращает количество
                          главных реле в цепи

                      b – выбор шины и контакта (см. команду 4)

                      c – действие:
                          1 – замкнуть
                          2 – разомкнуть
                      """,
          Response = """
                      Ответ:
                        количество главных реле в цепи
                      """,
          ResponseExample = "Ответ от устройства: 1"
        },

        new()
        {
          Id = 5,
          Name = "Замыкание / размыкание устройств на шины A и B",
          Syntax = "5.a.b.c.",
          Variables = """
                      a – Что подключить (см. п. 4, без блокировочных реле и шунта)(Самоконтроль ППУ - 7)

                      b – контакт:
                          1. 1
                          2. 1-4
                          3. 1-4
                          4. 1-4
                          5. 2-3
                          6. Самоконтроль ППУ - 0

                      c – действие:
                          1 – замкнуть
                          2 – разомкнуть
                      """,
          Response = """
                      JSON:
                        ModuleName       – тип модуля (DeviceBusCommutation)
                        NumberDevice     – номер устройства
                        NumberChassis    – номер шасси
                        Answer           – подтверждение выполненной команды (5.a.b.c)
                      """,
          ResponseExample =
            "{\"ModuleName\":\"DeviceBusCommutation\",\"NumberDevice\":20,\"NumberChassis\":1,\"Answer\":\"5.2.21.1\"}"
        },

        new()
        {
          Id = 51,
          Name = "Получение текущей замкнутой цепочки",
          Syntax = "51.0.0.0.",
          Variables = "-",
          Response = """
                       JSON:
                         ModuleName       – тип модуля (DeviceBusCommutation)
                         NumberDevice     – номер устройства
                         NumberChassis    – номер шасси
                         Answer           – информация о текущей замкнутой цепочке (51.a.b.c)
                       """,
          ResponseExample =
            "{\"ModuleName\":\"DeviceBusCommutation\",\"NumberDevice\":20,\"NumberChassis\":1,\"Answer\":\"51.2.21.2\"}"
        },

        new()
        {
          Id = 6,
          Name = "Самоконтроль мультиметра",
          Syntax = "6.a.b.c.",
          Variables = """
                      a – тип элемента:
                          1 – резистор
                          2 – конденсатор

                      b – номер резистора или конденсатора

                      c – действие:
                          1 – замкнуть
                          2 – разомкнуть
                      """,
          Response = "-",
          ResponseExample = "Ответ отсутствует"
        }
      ]
    };
    private static readonly DeviceHelpInfo PowerSupplyModuleHelp = new()
    {
      DeviceName = "МШ",
      Commands =
      [
        // 1
        new()
        {
          Id = 1,
          Name = "Инициализация",
          Syntax = "1.0.0.0.",
          Variables = "-",
          Response = """
                      Answer:
                        1.0.1 – инициализация выполнена успешно
                      """,
          ResponseExample =
            "{\"Answer\":\"1.0.1\"}"
        },

        // 2.1.1
        new()
        {
          Id = 21,
          Name = "Включение источников питания 3/4",
          Syntax = "2.1.1.0.",
          Variables = """
                      a = 1 – включить
                      b = 1 – источники 3 и 4
                      """,
          Response = "-",
          ResponseExample = "-"
        },

        // 2.2.1
        new()
        {
          Id = 22,
          Name = "Выключение источников питания 3/4",
          Syntax = "2.2.1.0.",
          Variables = """
                      a = 2 – выключить
                      b = 1 – источники 3 и 4
                      """,
          Response = "-",
          ResponseExample = "-"
        },

        // 7
        new()
        {
          Id = 7,
          Name = "Проверка состояния питания",
          Syntax = "7.0.0.0.",
          Variables = "-",
          Response = """
                      Answer:
                        0 – питание отсутствует
                        1 – питание присутствует
                      """,
          ResponseExample =
            "{\"Answer\":\"1\"}"
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
        ["MKR"] = MkrHelp,
        ["DBC"] = DBCHelp,
        ["MS"] = PowerSupplyModuleHelp
      };

    // История команд
    private readonly List<string> _history = new();
    private int _historyIndex = -1;
    public SetCommand()
    {
      InitializeComponent();
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
        CommandInput.CaretIndex = CommandInput.Text.Length;
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

      if (e.Key != Key.Enter)
        return;

      var text = CommandInput.Text.Trim();
      if (string.IsNullOrWhiteSpace(text))
        return;

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
        if (input.StartsWith("clear", StringComparison.OrdinalIgnoreCase))
        {
          Dispatcher.Invoke(() => ConsolePanel.Children.Clear());
          return;
        }

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

      await socket.SendToAsync(new ArraySegment<byte>(buffer), SocketFlags.None, endpoint);
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
