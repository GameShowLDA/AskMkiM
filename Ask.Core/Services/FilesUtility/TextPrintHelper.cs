using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace Ask.Core.Services.FilesUtility
{
  /// <summary>
  /// Определяет результат выполнения печати текста.
  /// </summary>
  public enum TextPrintStatus
  {
    /// <summary>
    /// Печать успешно завершена.
    /// </summary>
    Completed,

    /// <summary>
    /// Печать была отменена пользователем или системой.
    /// </summary>
    Canceled,

    /// <summary>
    /// Во время печати произошла ошибка.
    /// </summary>
    Failed,
  }

  /// <summary>
  /// Представляет результат выполнения операции печати текста.
  /// </summary>
  /// <param name="Status">Статус выполнения операции.</param>
  /// <param name="ErrorMessage">
  /// Сообщение об ошибке, если операция завершилась неуспешно.
  /// </param>
  public sealed record TextPrintResult(
      TextPrintStatus Status,
      string? ErrorMessage = null)
  {
    /// <summary>
    /// Результат успешного завершения печати.
    /// </summary>
    public static TextPrintResult Completed { get; } = new(TextPrintStatus.Completed);

    /// <summary>
    /// Результат отменённой операции печати.
    /// </summary>
    public static TextPrintResult Canceled { get; } = new(TextPrintStatus.Canceled);

    /// <summary>
    /// Создаёт результат с признаком ошибки.
    /// </summary>
    /// <param name="errorMessage">Описание возникшей ошибки.</param>
    /// <returns>Результат операции печати с ошибкой.</returns>
    public static TextPrintResult Failed(string errorMessage)
    {
      return new TextPrintResult(TextPrintStatus.Failed, errorMessage);
    }
  }

  /// <summary>
  /// Утилитарный класс для печати текста через PrintDialog.
  /// </summary>
  public static class TextPrintHelper
  {
    /// <summary>
    /// Отправляет переданный текст на печать через стандартный диалог печати.
    /// </summary>
    /// <param name="text">Текст для печати.</param>
    /// <param name="title">Заголовок задания печати (отображается в очереди печати).</param>
    public static void PrintText(string text, string title = "Печать текста")
    {
      var result = PrintTextAsync(text, title).GetAwaiter().GetResult();

      if (result.Status == TextPrintStatus.Canceled)
      {
        return;
      }

      if (result.Status == TextPrintStatus.Failed)
      {
        MessageBox.Show(
          $"Ошибка при печати: {result.ErrorMessage}",
          "Ошибка печати",
          MessageBoxButton.OK,
          MessageBoxImage.Error);
      }
    }

    /// <summary>
    /// Открывает стандартный диалог печати и выполняет печать указанного текста.
    /// </summary>
    /// <param name="text">Текст, предназначенный для печати.</param>
    /// <param name="title">Заголовок документа, отображаемый в очереди печати.</param>
    /// <returns>
    /// Результат выполнения операции печати.
    /// </returns>
    public static Task<TextPrintResult> PrintTextAsync(string text, string title = "Печать текста")
    {
      if (string.IsNullOrWhiteSpace(text))
      {
        return Task.FromResult(TextPrintResult.Failed("Текст для печати пустой."));
      }

      var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
      if (dispatcher.CheckAccess())
      {
        return Task.FromResult(PrintTextOnCurrentThread(text, title));
      }

      return dispatcher.InvokeAsync(() => PrintTextOnCurrentThread(text, title)).Task;
    }

    /// <summary>
    /// Выполняет печать текста в текущем STA-потоке.
    /// </summary>
    /// <param name="text">Текст, предназначенный для печати.</param>
    /// <param name="title">Заголовок документа, отображаемый в очереди печати.</param>
    /// <returns>Результат выполнения операции печати.</returns>
    private static TextPrintResult PrintTextOnCurrentThread(string text, string title)
    {
      try
      {
        var pd = new PrintDialog();
        if (pd.ShowDialog() != true)
        {
          return TextPrintResult.Canceled;
        }

        FlowDocument doc = new FlowDocument(new Paragraph(new Run(text)))
        {
          PagePadding = new Thickness(50),
          TextAlignment = TextAlignment.Left,
          ColumnWidth = double.PositiveInfinity
        };
        PrintSettingsService.ApplyTo(doc);

        IDocumentPaginatorSource paginator = doc;
        pd.PrintDocument(paginator.DocumentPaginator, title);
        return TextPrintResult.Completed;
      }
      catch (Exception ex)
      {
        return TextPrintResult.Failed(ex.Message);
      }
    }
  }
}
