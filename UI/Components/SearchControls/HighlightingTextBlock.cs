using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using static Utilities.LoggerUtility;

namespace UI.Components.SearchControls
{
  public class HighlightingTextBlock : TextBlock
  {
    // Основной текст для отображения
    public static readonly DependencyProperty MainTextProperty =
        DependencyProperty.Register(nameof(MainText), typeof(string), typeof(HighlightingTextBlock),
            new PropertyMetadata(string.Empty, OnPropertiesChanged));

    public string MainText
    {
      get { return (string)GetValue(MainTextProperty); }
      set { SetValue(MainTextProperty, value); }
    }

    // Текст, который нужно выделить
    public static readonly DependencyProperty HighlightTextProperty =
        DependencyProperty.Register(nameof(HighlightText), typeof(string), typeof(HighlightingTextBlock),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender, OnPropertiesChanged));

    public string HighlightText
    {
      get { return (string)GetValue(HighlightTextProperty); }
      set { SetValue(HighlightTextProperty, value); }
    }

    // Кисть для выделения
    public static readonly DependencyProperty HighlightBrushProperty =
        DependencyProperty.Register(nameof(HighlightBrush), typeof(Brush), typeof(HighlightingTextBlock),
            new FrameworkPropertyMetadata(Brushes.Red, FrameworkPropertyMetadataOptions.AffectsRender, OnPropertiesChanged));

    public Brush HighlightBrush
    {
      get { return (Brush)GetValue(HighlightBrushProperty); }
      set { SetValue(HighlightBrushProperty, value); }
    }

    private static void OnPropertiesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      var control = (HighlightingTextBlock)d;
      control?.UpdateInlines();
    }


    private void UpdateInlines()
    {
      Inlines.Clear();

      string text = MainText;
      if (string.IsNullOrEmpty(text))
        return;

      if (string.IsNullOrEmpty(HighlightText))
      {
        Inlines.Add(new Run(text));
        return;
      }

      Regex regex = new Regex(Regex.Escape(HighlightText), RegexOptions.IgnoreCase);
      int lastIndex = 0;
      foreach (Match match in regex.Matches(text))
      {
        if (match.Index > lastIndex)
        {
          Inlines.Add(new Run(text.Substring(lastIndex, match.Index - lastIndex)));
        }
        Inlines.Add(new Run(text.Substring(match.Index, match.Length))
        {
          Foreground = HighlightBrush,
          FontWeight = FontWeights.Bold
        });
        lastIndex = match.Index + match.Length;
      }
      if (lastIndex < text.Length)
      {
        Inlines.Add(new Run(text.Substring(lastIndex)));
      }
    }
  }
}
