using System.Windows;
using System.Windows.Media;

namespace Ask.Core.Shared.DTO.Protocol
{
  /// <summary>
  /// Модель сообщения.
  /// </summary>
  public class ShowMessageModel
  {
    /// <summary>
    /// Перечисление, определяющее тип сообщения для вывода или логирования.
    /// Используется для обозначения информационных сообщений, успешных операций и ошибок.
    /// </summary>
    public enum MessageType
    {
      /// <summary>
      /// Информационное сообщение, отображающее одиночную команду из программы контроля.
      /// </summary>
      Command,

      /// <summary>
      /// Информационное сообщение, отображающее блок команд из программы контроля.
      /// </summary>
      CommandBlock,

      /// <summary>
      /// Информационное сообщение, не содержащее ошибок или предупреждений.
      /// Используется для отображения общих сведений о ходе работы программы.
      /// </summary>
      Info,

      /// <summary>
      /// Сообщение об успешном завершении операции.
      /// Используется для уведомления пользователя о том, что действие выполнено корректно.
      /// </summary>
      Success,

      /// <summary>
      /// Сообщение об ошибке, возникшей в процессе выполнения.
      /// Используется для информирования пользователя о возникновении проблем или некорректных действий.
      /// </summary>
      Error
    }

    /// <summary>
    /// Сообщение и цвет для успешного выполнения.
    /// </summary>
    static public (string Title, Color TitleColor) SuccessMessage => ("НОРМА", ((SolidColorBrush)Application.Current.Resources["TestsProtocolMessageSuccesForeground"]).Color);

    /// <summary>
    /// Сообщение и цвет для ошибки.
    /// </summary>
    static public (string Title, Color TitleColor) ErrorMessage => ("БРАК", ((SolidColorBrush)Application.Current.Resources["TestsProtocolMessageErrorForeground"]).Color);

    /// <summary>
    /// Получает или задает заголовок сообщения.
    /// </summary>
    public string Header { get; set; }

    /// <summary>
    /// Получает или задает текст сообщения.
    /// </summary>
    public string Message { get; set; }

    public string Time { get; set; }
    public string Debug { get; set; }

    /// <summary>
    /// Получает или задает цвет заголовка сообщения.
    /// </summary>
    public Color? HeaderColor { get; set; }

    /// <summary>
    /// Получает или задает цвет текста сообщения.
    /// </summary>
    public Color? MessageColor { get; set; }

    /// <summary>
    /// Цвет отображения времени сообщения.
    /// </summary>
    public Color? TimeColor { get; set; }

    /// <summary>
    /// Получает или задает значение, указывающее, является ли сообщение ошибкой выполнения.
    /// </summary>
    public bool ExecutionError { get; set; }

    /// <summary>
    /// Получает или задает значение, указывающее, можно ли удалять сообщение.
    /// </summary>
    public bool CanBeDeleted { get; set; }

    /// <summary>
    /// Показывает, связано ли сообщение с устройством.
    /// True — сообщение от устройства или касается его работы; 
    /// False — общее информационное сообщение.
    /// </summary>
    public bool IsDeviceMessage { get; set; }

    /// <summary>
    /// Размер табуляции перед строкой.
    /// </summary>
    public int IndentLevel { get; set; }

    private MessageType? status;
    public MessageType? Status
    {
      get
      {
        return status;
      }
      set
      {
        status = value;
        switch (status)
        {
          case MessageType.Success:
            MessageColor ??= SuccessMessage.TitleColor;
            break;

          case MessageType.Error:
            MessageColor ??= ErrorMessage.TitleColor;
            break;

          case MessageType.Command:
            try
            {
              if (System.Windows.Application.Current != null)
              {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                  if (System.Windows.Application.Current?.Resources["YellowColorSolidColorBrush"] is SolidColorBrush brush)
                  {
                    MessageColor = brush.Color;
                  }
                });
              }
            }
            catch (Exception)
            {

            }
            break;
        }
      }
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ShowMessageModel"/>.
    /// </summary>
    public ShowMessageModel()
    {
      ExecutionError = false;
      CanBeDeleted = false;
      IsDeviceMessage = false;
      IndentLevel = 0;

      try
      {
        if (System.Windows.Application.Current != null)
        {
          System.Windows.Application.Current.Dispatcher.Invoke(() =>
          {
            HeaderColor = ((SolidColorBrush)Application.Current.Resources["TestsProtocolHeaderForeground"]).Color;
            MessageColor = ((SolidColorBrush)Application.Current.Resources["TestsProtocolMessageForeground"]).Color;
            TimeColor = ((SolidColorBrush)Application.Current.Resources["TestsProtocolTimeForeground"]).Color;
          });
        }
      }
      catch (Exception)
      {

      }
    }

    /// <summary>
    /// Возвращает сообщение вида "Заголовок: Сообщение".
    /// </summary>
    /// <returns>Строковое представление объекта.</returns>
    public override string ToString()
    {
      if (!string.IsNullOrEmpty(Header) && !string.IsNullOrEmpty(Message))
      {
        return Header + ": " + Message;
      }
      else if (!string.IsNullOrEmpty(Header))
      {
        return Header;
      }
      else if (!string.IsNullOrEmpty(Message))
      {
        return Message;
      }
      else
      {
        return null;
      }
    }

    public string GetQualityPrefix()
    {
      if (Status == MessageType.Success)
      {
        return $"[{SuccessMessage.Title}]";
      }
      else if (Status == MessageType.Error)
      {
        return $"[{ErrorMessage.Title}]";
      }
      else
      {
        return string.Empty;
      }
    }

    public Color? GetColorMessage()
    {
      if (Status == MessageType.Success)
      {
        return SuccessMessage.TitleColor;
      }
      else if (Status == MessageType.Error)
      {
        return ErrorMessage.TitleColor;
      }
      else if (Status == MessageType.Command)
      {
        Color? color = null;
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
          if (System.Windows.Application.Current?.Resources["YellowColorSolidColorBrush"] is SolidColorBrush brush)
          {
            color = brush.Color;
          }
        });

        return color;
      }
      else if (Status == MessageType.CommandBlock)
      {
        Color? color = null;
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
          if (System.Windows.Application.Current?.Resources["LightBlueColorSolidColorBrush"] is SolidColorBrush brush)
          {
            color = brush.Color;
          }
        });

        return color;
      }

      return null;
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ShowMessageModel"/> с заданными параметрами.
    /// </summary>
    /// <param name="header">Текст заголовка сообщения (по умолчанию null).</param>
    /// <param name="headerColor">Цвет заголовка сообщения (по умолчанию null).</param>
    /// <param name="message">Основной текст сообщения (по умолчанию null).</param>
    /// <param name="messageColor">Цвет основного текста сообщения (по умолчанию null).</param>
    public ShowMessageModel(string header = null, Color? headerColor = null, string message = null, string debug = null, Color? messageColor = null, MessageType? type = MessageType.Info) : this()
    {
      if (headerColor != null)
      {
        HeaderColor = headerColor;
      }

      if (messageColor != null)
      {
        MessageColor = messageColor;
      }

      Header = header;
      Message = message;
      Status = type;
    }
  }
}
