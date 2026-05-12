using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Ask.UI.Features.Archive.Services
{
  public static class ArchivePrintDialogService
  {
    public static void ShowAndPrint(
      FrameworkElement ownerElement,
      string documentName,
      Func<double, double, double, double, FixedDocument> documentFactory)
    {
      ArgumentNullException.ThrowIfNull(ownerElement);
      ArgumentNullException.ThrowIfNull(documentFactory);

      var printDialog = new PrintDialog();
      var owner = Window.GetWindow(ownerElement);

      var showResult = owner != null
        ? printDialog.ShowDialog() == true
        : printDialog.ShowDialog() == true;

      if (!showResult)
      {
        return;
      }

      var printCapabilities = printDialog.PrintQueue.GetPrintCapabilities(printDialog.PrintTicket);
      var pageImageableArea = printCapabilities.PageImageableArea;
      var hardMarginX = pageImageableArea?.OriginWidth ?? 0;
      var hardMarginY = pageImageableArea?.OriginHeight ?? 0;

      var document = documentFactory(
        hardMarginX,
        hardMarginY,
        printDialog.PrintableAreaWidth,
        printDialog.PrintableAreaHeight);

      IDocumentPaginatorSource paginatorSource = document;
      printDialog.PrintDocument(paginatorSource.DocumentPaginator, documentName);
    }
  }
}
