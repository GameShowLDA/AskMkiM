using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Mode.Settings.ConfigSettings.CustomControls
{
  internal class DefaultConfigControls
  {
    /// <summary>
    /// Устанавливает описание устройства.
    /// </summary>
    /// <param name="header">Заголовок (имя устройства).</param>
    /// <param name="description">Описание.</param>
    /// <param name="headerColor">Цвет заголовка.</param>
    static internal void ShowMessage(RichTextBox InfoDevice, string header, string description, Color headerColor)
    {
      Run headerRun = new Run(header)
      {
        FontWeight = FontWeights.Bold,
        FontSize = 15
      };

      Run descriptionRun = new Run(description)
      {
        FontSize = 15
      };

      Paragraph paragraph = new Paragraph
      {
        Margin = new Thickness(0),
        Inlines =
        {
            headerRun,
            new Run(":  "),
            descriptionRun,
            new Run(".")
        }
      };

      headerRun.Foreground = new SolidColorBrush(headerColor);
      paragraph.Foreground = new SolidColorBrush(Colors.White);

      FlowDocument document = new FlowDocument
      {
        PagePadding = new Thickness(0),
        Blocks = { paragraph }
      };

      InfoDevice.Document.Blocks.Clear();
      InfoDevice.Padding = new Thickness(5);
      InfoDevice.Document = document;
    }
  }
}
