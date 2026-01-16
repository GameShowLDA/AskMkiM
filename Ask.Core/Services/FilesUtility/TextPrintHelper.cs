using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Ask.Core.Services.FilesUtility
{
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
      if (string.IsNullOrWhiteSpace(text))
      {
        MessageBox.Show("Текст для печати пустой.", "Печать", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }

      try
      {
        var pd = new PrintDialog();
        if (pd.ShowDialog() == true)
        {
          FlowDocument doc = new FlowDocument(new Paragraph(new Run(text)))
          {
            FontFamily = new FontFamily("Consolas"),
            FontSize = 10,
            PagePadding = new Thickness(50),
            TextAlignment = TextAlignment.Left,
            ColumnWidth = double.PositiveInfinity
          };

          IDocumentPaginatorSource paginator = doc;
          pd.PrintDocument(paginator.DocumentPaginator, title);
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка при печати: {ex.Message}", "Ошибка печати", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }
  }
}
