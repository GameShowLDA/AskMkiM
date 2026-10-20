using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Ask.UI.Features.FileComparison.Views;

/// <summary>
/// Фрагмент строки с признаком изменённых символов.
/// </summary>
/// <param name="Text">Текст фрагмента.</param>
/// <param name="IsChanged">Признак изменения фрагмента относительно второй стороны.</param>
public sealed record DiffTextSegment(string Text, bool IsChanged);

/// <summary>
/// Отображает фрагменты строки сравнения в TextBlock с выделением изменённых символов.
/// </summary>
public static class DiffTextHighlight
{
  /// <summary>
  /// Строка сравнения, отображаемая набором текстовых фрагментов.
  /// </summary>
  public static readonly DependencyProperty LineProperty = DependencyProperty.RegisterAttached(
    "Line", typeof(DiffLineViewModel), typeof(DiffTextHighlight), new PropertyMetadata(null, OnLineChanged));

  /// <summary>
  /// Возвращает отображаемую строку сравнения.
  /// </summary>
  /// <param name="element">Текстовый элемент.</param>
  /// <returns>Модель строки сравнения.</returns>
  public static DiffLineViewModel? GetLine(DependencyObject element) =>
    (DiffLineViewModel?)element.GetValue(LineProperty);

  /// <summary>
  /// Задаёт отображаемую строку сравнения.
  /// </summary>
  /// <param name="element">Текстовый элемент.</param>
  /// <param name="value">Модель строки сравнения.</param>
  public static void SetLine(DependencyObject element, DiffLineViewModel? value) =>
    element.SetValue(LineProperty, value);

  /// <summary>
  /// Пересоздаёт фрагменты при смене строки, включая повторное использование контейнера.
  /// </summary>
  /// <param name="element">Текстовый элемент.</param>
  /// <param name="args">Предыдущее и новое значения строки.</param>
  private static void OnLineChanged(DependencyObject element, DependencyPropertyChangedEventArgs args)
  {
    if (element is not TextBlock textBlock)
    {
      return;
    }

    textBlock.Inlines.Clear();
    if (args.NewValue is not DiffLineViewModel line)
    {
      return;
    }

    foreach (var segment in line.Segments)
    {
      var run = new Run(segment.Text);
      if (segment.IsChanged)
      {
        run.Background = DiffBrushes.ChangedCharacterBackground;
        run.FontWeight = FontWeights.Bold;
      }

      textBlock.Inlines.Add(run);
    }
  }
}
