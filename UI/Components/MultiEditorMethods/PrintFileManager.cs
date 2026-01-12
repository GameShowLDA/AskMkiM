using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using UI.Components.Invoke;

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
    public static void PrintFile(ObservableCollection<OpenFileButton> openPages, ObservableCollection<UserControl> userControls)
    {
      var activeTab = openPages.FirstOrDefault(page =>
        page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);

      if (activeTab == null)
        return;

      PrintDialog printDialog = new PrintDialog();
      FlowDocument flowDocument = new FlowDocument
      {
        PageWidth = printDialog.PrintableAreaWidth,
        FontFamily = new FontFamily("Consolas"),
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

        if (string.IsNullOrEmpty(fullText))
        {
          return;
        }

        var lines = fullText.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

        foreach (var line in lines)
        {
          var run = new Run(line.Replace("\t", "    "));
          var paragraph = new Paragraph(run)
          {
            Margin = new Thickness(0)
          };

          flowDocument.Blocks.Add(paragraph);
        }

        flowDocument.PageWidth = printDialog.PrintableAreaWidth;
        flowDocument.PageHeight = printDialog.PrintableAreaHeight;
        flowDocument.ColumnWidth = printDialog.PrintableAreaWidth;
        flowDocument.PagePadding = new Thickness(40);

        IDocumentPaginatorSource idocument = flowDocument;
        printDialog.PrintDocument(idocument.DocumentPaginator, "Печать документа");
      }
    }
  }
}
