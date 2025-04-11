using System.Windows;
using System.Windows.Media;

namespace Utilities.Models
{
  /// <summary>
  /// Модель сообщения.
  /// </summary>
  public class ShowMessageModel
  {
    /// <summary>
    /// Сообщение и цвет для успешного выполнения.
    /// </summary>
    static public (string Title, Color TitleColor) SuccessMessage => ("НОРМА", Color.FromArgb(255, 79, 205, 101));

    /// <summary>
    /// Сообщение и цвет для ошибки.
    /// </summary>
    static public (string Title, Color TitleColor) ErrorMessage => ("БРАК", Color.FromArgb(255, 241, 48, 27));

    /// <summary>
    /// Получает или задает заголовок сообщения.
    /// </summary>
    public string Header { get; set; }

    /// <summary>
    /// Получает или задает текст сообщения.
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Получает или задает цвет заголовка сообщения.
    /// </summary>
    public Color? HeaderColor { get; set; }

    /// <summary>
    /// Получает или задает цвет текста сообщения.
    /// </summary>
    public Color? MessageColor { get; set; }

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


    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ShowMessageModel"/>.
    /// </summary>
    public ShowMessageModel()
    {
      ExecutionError = false;
      CanBeDeleted = false;
      IsDeviceMessage = false;
      IndentLevel = 0;

      Application.Current.Dispatcher.Invoke(() =>
      {
        if (Application.Current?.Resources["ForegroundSolidColorBrush"] is SolidColorBrush brush)
        {
          HeaderColor = brush.Color;
          MessageColor = brush.Color;
        }
      });
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

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ShowMessageModel"/> с заданными параметрами.
    /// </summary>
    /// <param name="header">Текст заголовка сообщения (по умолчанию null).</param>
    /// <param name="headerColor">Цвет заголовка сообщения (по умолчанию null).</param>
    /// <param name="message">Основной текст сообщения (по умолчанию null).</param>
    /// <param name="messageColor">Цвет основного текста сообщения (по умолчанию null).</param>
    public ShowMessageModel(string header = null, Color? headerColor = null, string message = null, Color? messageColor = null) : this()
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
    }
  }
}
