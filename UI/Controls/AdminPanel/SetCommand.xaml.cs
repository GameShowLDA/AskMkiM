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
    private string portInput = "8800";
    private string portOutput = "8888";
    private readonly Socket socket;

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
        // ожидается формат:   ip command
        // пример: 192.168.1.20 POWER_ON
        var parts = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 2)
        {
          AddConsoleLine("Неверный формат. Используй:  IP COMMAND", Brushes.OrangeRed);
          return;
        }

        string ip = parts[0];
        string command = parts[1];

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
  }
}
