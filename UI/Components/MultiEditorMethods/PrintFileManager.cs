using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using UI.Components.Invoke;
using UI.Controls.TextEditor;
using static Utilities.LoggerUtility;

namespace UI.Components.MultiEditorMethods
{
  public static class PrintFileManager
  {
    public static void PrintFile(List<OpenFileButton> openPages, List<UserControl> userControls)
    {
      var activeTab = openPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
      PrintDialog printDialog = new PrintDialog();
      FlowDocument flowDocument = new FlowDocument();


      if (printDialog.ShowDialog() == true)
      {
        int index = openPages.IndexOf(activeTab);

        if (userControls[index] is TextEditorUI)
        {
          var textEditor = userControls[index] as TextEditorUI;
          flowDocument.Blocks.Add(new Paragraph(new Run(textEditor.Text)));
          IDocumentPaginatorSource idocument = flowDocument;
          printDialog.PrintDocument(idocument.DocumentPaginator, "Печать документа");
          LogInformation($"Файл {activeTab.Text} отправлен на печать");
        }
      }
    }
  }
}
