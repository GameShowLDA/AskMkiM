using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using UI.Components.Invoke;
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
      var activeTab = openPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
      // TODO: починить печать файлов
      PrintDialog printDialog = new PrintDialog();
      FlowDocument flowDocument = new FlowDocument();

      if (printDialog.ShowDialog() == true)
      {
        int index = openPages.IndexOf(activeTab);

        if (userControls[index] is TextEditorUI) // TODO: берем активную вкладку из TextEditorControl
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
