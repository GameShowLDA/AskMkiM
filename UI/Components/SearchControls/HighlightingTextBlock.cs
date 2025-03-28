using AppConfig;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using UI.Controls.Search;
using static Utilities.LoggerUtility;

namespace UI.Components.SearchControls
{
  public class HighlightingTextBlock : TextBlock
  {

    public static readonly DependencyProperty IsCaseSensitiveProperty =
        DependencyProperty.Register(
            "IsCaseSensitive",
            typeof(bool),
            typeof(HighlightingTextBlock),
            new PropertyMetadata(false));

    public bool IsCaseSensitive
    {
      get { return (bool)GetValue(IsCaseSensitiveProperty); }
      set { SetValue(IsCaseSensitiveProperty, value); }
    }
    public bool IsWholeWord
    {
      get { return (bool)GetValue(IsWholeWordProperty); }
      set { SetValue(IsWholeWordProperty, value); }
    }

    public static readonly DependencyProperty IsWholeWordProperty =
        DependencyProperty.Register(
            "IsWholeWord",
            typeof(bool),
            typeof(HighlightingTextBlock),
            new PropertyMetadata(false));


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

      RegexOptions options = IsCaseSensitive == true ? RegexOptions.None : RegexOptions.IgnoreCase;

      string pattern = IsWholeWord ? $@"(?<!\w){Regex.Escape(HighlightText)}(?!\w)" : Regex.Escape(HighlightText);

      Regex regex = new Regex(pattern, options);
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
