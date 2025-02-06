using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Mode.Settings.SendCommand
{
  /// <summary>
  /// Логика взаимодействия для SendCommandControl.xaml
  /// </summary>
  public partial class SendCommandControl : UserControl
  {
    /// <summary>
    /// Перечисление, представляющее состояние ввода пользователя.
    /// </summary>
    private enum InputState
    {
      WaitingForIp,
      WaitingForCommand
    }

    /// <summary>
    /// Текущее состояние ввода.
    /// </summary>
    private InputState currentState;

    /// <summary>
    /// Текущий введенный IP-адрес.
    /// </summary>
    private string currentIp;

    /// <summary>
    /// Последний введенный корректный IP-адрес.
    /// </summary>
    private string lastEnteredIp;

    /// <summary>
    /// Последняя введенная корректная команда.
    /// </summary>
    private string lastEnteredCommand;

    /// <summary>
    /// Конструктор класса SendCommandControl.
    /// Инициализирует компоненты и устанавливает начальное состояние.
    /// </summary>
    public SendCommandControl()
    {
      InitializeComponent();
      currentState = InputState.WaitingForIp;
      UpdateInputPrompt();
    }

    /// <summary>
    /// Обработчик события нажатия клавиши в текстовом поле ввода.
    /// Проверяет введенные данные и обновляет состояние в зависимости от текущего режима ввода.
    /// </summary>
    /// <param name="sender">Объект, вызвавший событие.</param>
    /// <param name="e">Аргументы события клавиатуры.</param>
    private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Enter)
      {
        string text = InputTextBox.Text.Trim();

        if (!string.IsNullOrEmpty(text))
        {
          switch (currentState)
          {
            case InputState.WaitingForIp:
              if (IsValidIp(text))
              {
                currentIp = text;
                DisplayInfo($"IP: {currentIp}");
                currentState = InputState.WaitingForCommand;
                UpdateInputPrompt();
              }
              else
              {
                DisplayInfo("Error: Invalid IP address. Please enter a valid IP.");
              }
              break;
            case InputState.WaitingForCommand:
              if (IsValidCommand(text))
              {

              }
              else
              {
                DisplayInfo("Error: Invalid command syntax. Please enter a valid command (x.x.x.x).");
              }
              break;
          }
          InputTextBox.Clear();

          if (currentState == InputState.WaitingForIp)
          {
            if (!string.IsNullOrEmpty(lastEnteredIp))
              InputTextBox.Text = lastEnteredIp;
          }
          else if (currentState == InputState.WaitingForCommand)
          {
            if (!string.IsNullOrEmpty(lastEnteredCommand))
              InputTextBox.Text = lastEnteredCommand;
          }
        }

        e.Handled = true;
      }
    }

    /// <summary>
    /// Обновляет подсказки и метки в интерфейсе в зависимости от текущего состояния ввода.
    /// </summary>
    private void UpdateInputPrompt()
    {
      if (currentState == InputState.WaitingForIp)
      {
        InputLabel.Content = "IP:";
        InputTextBox.ToolTip = "Enter IP Address";

      }
      else if (currentState == InputState.WaitingForCommand)
      {
        InputLabel.Content = "Command:";
        InputTextBox.ToolTip = "Enter Command";


      }
    }

    /// <summary>
    /// Проверяет, является ли введенная строка корректным IP-адресом.
    /// </summary>
    /// <param name="ip">Строка для проверки.</param>
    /// <returns>true, если IP-адрес корректен; иначе false.</returns>
    private bool IsValidIp(string ip)
    {
      if (string.IsNullOrWhiteSpace(ip))
        return false;

      string[] parts = ip.Split('.');
      if (parts.Length != 4)
        return false;

      foreach (string part in parts)
      {
        if (!int.TryParse(part, out int num) || num < 0 || num > 255)
          return false;
      }

      lastEnteredIp = ip;
      return true;
    }

    /// <summary>
    /// Проверяет, является ли введенная строка корректной командой.
    /// </summary>
    /// <param name="command">Строка для проверки.</param>
    /// <returns>true, если команда корректна; иначе false.</returns>
    private bool IsValidCommand(string command)
    {
      if (string.IsNullOrWhiteSpace(command))
        return false;

      string[] parts = command.Split('.');
      if (parts.Length != 4 && parts.Length != 5)
        return false;

      foreach (string part in parts)
      {
        if (!int.TryParse(part, out int num) || num < 0)
          return false;
      }

      lastEnteredCommand = command;
      return true;
    }

    /// <summary>
    /// Отображает информационное сообщение в текстовом поле протокола.
    /// </summary>
    /// <param name="info">Информационное сообщение для отображения.</param>
    private void DisplayInfo(string info)
    {
      var paragraph = new Paragraph { LineHeight = 1 };
      paragraph.Inlines.Add(new Run(info) { Foreground = Brushes.White });
      ProtocolTextBox.Document.Blocks.Add(paragraph);
      ProtocolTextBox.ScrollToEnd();
    }
  }
}
