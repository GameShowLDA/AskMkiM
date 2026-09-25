using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Ask.UI.Components.ProtocolListBox;

/// <summary>
/// Отображает журнал ROOT с цветными уровнями и маркерами записей.
/// </summary>
public sealed class ProtocolServiceLogsBox : RichTextBox
{
  private static readonly Regex EntryPrefix = new(
    @"(?m)^(\[ЛОГ ROOT\] [^\r\n\[]*)\[([^\]\r\n]+)\]",
    RegexOptions.Compiled);
  private bool _documentDirty = true;

  public static readonly DependencyProperty LogTextProperty = DependencyProperty.Register(
    nameof(LogText), typeof(string), typeof(ProtocolServiceLogsBox),
    new PropertyMetadata(string.Empty, OnLogTextChanged));

  /// <summary>Текст служебного журнала.</summary>
  public string LogText
  {
    get => (string)GetValue(LogTextProperty);
    set => SetValue(LogTextProperty, value);
  }

  public ProtocolServiceLogsBox()
  {
    IsReadOnly = true;
    IsUndoEnabled = false;
    IsDocumentEnabled = false;
    Document.PagePadding = new Thickness(0);
    IsVisibleChanged += (_, _) => RefreshDocument();
  }

  private static void OnLogTextChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
  {
    var control = (ProtocolServiceLogsBox)sender;
    control._documentDirty = true;
    control.RefreshDocument();
  }

  private void RefreshDocument()
  {
    // Свёрнутые блоки не создают форматированный документ до первого раскрытия.
    if (!IsVisible || !_documentDirty)
      return;

    Document.Blocks.Clear();
    Document.Blocks.Add(CreateParagraph(LogText ?? string.Empty));
    _documentDirty = false;
  }

  internal static Paragraph CreateParagraph(string text)
  {
    var paragraph = new Paragraph { Margin = new Thickness(0) };
    int position = 0;
    foreach (Match match in EntryPrefix.Matches(text))
    {
      paragraph.Inlines.Add(new Run(text[position..match.Index]));
      var color = GetLevelBrush(match.Groups[2].Value);
      paragraph.Inlines.Add(new Run("● ") { Foreground = color });
      paragraph.Inlines.Add(new Run(match.Groups[1].Value));
      paragraph.Inlines.Add(new Run($"[{match.Groups[2].Value}]") { Foreground = color });
      position = match.Index + match.Length;
    }
    paragraph.Inlines.Add(new Run(text[position..]));
    return paragraph;
  }

  private static Brush GetLevelBrush(string level) => level.ToUpperInvariant() switch
  {
    "ERROR" or "EXCEPTION" => Brushes.OrangeRed,
    "WARN" or "WARNING" => Brushes.Goldenrod,
    "DEBUG" => Brushes.Gray,
    "INFO" => Brushes.White,
    _ => Brushes.LightGray
  };
}
