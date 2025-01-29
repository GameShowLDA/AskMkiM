using System.Windows.Media;

namespace Utilities.Models
{
  public class ShowMessageModel
  {
    /// <summary>
    /// Сообщение и цвет для успешного выполнения.
    /// </summary>
    static public Tuple<string, Color> SuccessMessage => Tuple.Create("OK", Color.FromArgb(255, 79, 205, 101));

    /// <summary>
    /// Сообщение и цвет для ошибки.
    /// </summary>
    static public Tuple<string, Color> ErrorMessage => Tuple.Create("NO", Color.FromArgb(255, 241, 48, 27));

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

    public ShowMessageModel()
    {
      ExecutionError = false;
      CanBeDeleted = false;
    }

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
    public ShowMessageModel(string header = null, Color? headerColor = null, string message = null, Color? messageColor = null) : this()
    {
      Header = header;
      HeaderColor = headerColor;
      Message = message;
      MessageColor = messageColor;
    }
  }
}
