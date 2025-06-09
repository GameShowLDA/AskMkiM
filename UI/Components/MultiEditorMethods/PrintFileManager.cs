using AppConfiguration.Interface;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using UI.Components.Invoke;
using UI.Controls.ProtocolNew;
using UI.Controls.TextEditor;
using static Utilities.LoggerUtility;

namespace UI.Components.MultiEditorMethods
{
  /// <summary>
  /// Класс для работы с печатью документов.
  /// </summary>
  public static class PrintFileManager
  {
    /// <summary>
    /// Выполняет печать содержимого активной вкладки в редакторе текста.
    /// </summary>
    /// <param name="openPages">Список вкладок, представляющих открытые страницы.</param>
    /// <param name="userControls">Список пользовательских контролов, ассоциированных с открытыми страницами.</param>
    public static void PrintFile(List<OpenFileButton> openPages, List<UserControl> userControls)
    {
      var activeTab = openPages.FirstOrDefault(page =>
        page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);

      if (activeTab == null)
        return;

      PrintDialog printDialog = new PrintDialog();
      FlowDocument flowDocument = new FlowDocument
      {
        PageWidth = printDialog.PrintableAreaWidth,
        FontFamily = new FontFamily("Consolas"), // Моноширинный для табуляций
        FontSize = 12,
        TextAlignment = TextAlignment.Left,
        PagePadding = new Thickness(40)
      };

      if (printDialog.ShowDialog() == true)
      {
        int index = openPages.IndexOf(activeTab);
        if (index < 0 || index >= userControls.Count)
          return;

        ITextAdapter textEditorContainer;

        if (userControls[index] is ITextAdapter adapter)
        {
          textEditorContainer = adapter;
        }
        else if (userControls[index] is IExecution exec)
        {
          textEditorContainer = exec.GetControl();
        }
        else
        {
          return;
        }

        string fullText = textEditorContainer.GetText();

        // Разделение по строкам
        var lines = fullText.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

        foreach (var line in lines)
        {
          // Визуально корректный способ — всё строкой, табы как есть
          var run = new Run(line.Replace("\t", "    "));
          var paragraph = new Paragraph(run)
          {
            Margin = new Thickness(0)
          };

          flowDocument.Blocks.Add(paragraph);
        }

        flowDocument.PageWidth = printDialog.PrintableAreaWidth;
        flowDocument.PageHeight = printDialog.PrintableAreaHeight;
        flowDocument.ColumnWidth = printDialog.PrintableAreaWidth; // <=== ключевая строка
        flowDocument.PagePadding = new Thickness(40); // если нужно немного отступов

        IDocumentPaginatorSource idocument = flowDocument;
        printDialog.PrintDocument(idocument.DocumentPaginator, "Печать документа");
      }
    }
  }
}
