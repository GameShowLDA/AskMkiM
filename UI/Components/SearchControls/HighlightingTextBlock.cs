using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace UI.Components.SearchControls
{
  /// <summary>
  /// Класс, представляющий текстовый блок с возможностью выделения текста.
  /// Поддерживает настройку чувствительности к регистру, поиск целых слов, а также возможность задавать цвет выделения.
  /// </summary>
  public class HighlightingTextBlock : TextBlock
  {
    /// <summary>
    /// Зависимое свойство, указывающее, следует ли учитывать регистр при выделении текста.
    /// </summary>
    public static readonly DependencyProperty IsCaseSensitiveProperty =
        DependencyProperty.Register(
            "IsCaseSensitive",
            typeof(bool),
            typeof(HighlightingTextBlock),
            new PropertyMetadata(false));

    /// <summary>
    /// Указывает, следует ли учитывать регистр при выделении текста.
    /// </summary>
    public bool IsCaseSensitive
    {
      get { return (bool)GetValue(IsCaseSensitiveProperty); }
      set { SetValue(IsCaseSensitiveProperty, value); }
    }

    /// <summary>
    /// Зависимое свойство, указывающее, следует ли выделять только целые слова при поиске.
    /// </summary>
    public static readonly DependencyProperty IsWholeWordProperty =
        DependencyProperty.Register(
            "IsWholeWord",
            typeof(bool),
            typeof(HighlightingTextBlock),
            new PropertyMetadata(false));

    /// <summary>
    /// Указывает, следует ли выделять только целые слова при поиске текста.
    /// </summary>
    public bool IsWholeWord
    {
      get { return (bool)GetValue(IsWholeWordProperty); }
      set { SetValue(IsWholeWordProperty, value); }
    }

    /// <summary>
    /// Зависимое свойство для основного текста, который будет отображаться в блоке.
    /// </summary>
    public static readonly DependencyProperty MainTextProperty =
        DependencyProperty.Register(nameof(MainText), typeof(string), typeof(HighlightingTextBlock),
            new PropertyMetadata(string.Empty, OnPropertiesChanged));

    /// <summary>
    /// Основной текст, который будет отображаться в блоке.
    /// </summary>
    public string MainText
    {
      get { return (string)GetValue(MainTextProperty); }
      set { SetValue(MainTextProperty, value); }
    }

    /// <summary>
    /// Зависимое свойство для текста, который нужно выделить.
    /// </summary>
    public static readonly DependencyProperty HighlightTextProperty =
        DependencyProperty.Register(nameof(HighlightText), typeof(string), typeof(HighlightingTextBlock),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender, OnPropertiesChanged));

    /// <summary>
    /// Текст, который нужно выделить.
    /// </summary>
    public string HighlightText
    {
      get { return (string)GetValue(HighlightTextProperty); }
      set { SetValue(HighlightTextProperty, value); }
    }

    /// <summary>
    /// Зависимое свойство для кисти, используемой для выделения текста.
    /// </summary>
    public static readonly DependencyProperty HighlightBrushProperty =
        DependencyProperty.Register(nameof(HighlightBrush), typeof(Brush), typeof(HighlightingTextBlock),
            new FrameworkPropertyMetadata(Brushes.Red, FrameworkPropertyMetadataOptions.AffectsRender, OnPropertiesChanged));

    /// <summary>
    /// Кисть, используемая для выделения текста.
    /// </summary>
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
      {
        return;
      }

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
          FontWeight = FontWeights.Bold,
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
