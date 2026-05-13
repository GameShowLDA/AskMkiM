using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using System.Windows;
using System.Windows.Media;

namespace Ask.UI.Controls.TextEditorControl
{
  /// <summary>
  /// Интерфейс для текстового маркера, предоставляющий свойства для подсветки текста.
  /// </summary>
  public interface ITextMarker
  {
    /// <summary>Начальный индекс (позиция) маркера.</summary>
    int StartOffset { get; }

    /// <summary>Длина выделяемого текста.</summary>
    int Length { get; }

    /// <summary>Дополнительная информация, прикреплённая к маркеру.</summary>
    object Tag { get; set; }

    /// <summary>Цвет фона подсветки.</summary>
    Color? BackgroundColor { get; set; }

    /// <summary>Цвет текста.</summary>
    Color? ForegroundColor { get; set; }

    /// <summary>Жирность шрифта (если нужно).</summary>
    FontWeight? FontWeight { get; set; }

    /// <summary>Удаляет маркер.</summary>
    void Delete();
  }

  /// <summary>
  /// Сервис подсветки текста для AvalonEdit. Отвечает за визуальное выделение фрагментов текста.
  /// </summary>
  public sealed class TextMarkerService : IBackgroundRenderer
  {
    private TextSegmentCollection<TextMarker> markers;
    private readonly ICSharpCode.AvalonEdit.TextEditor editor;
    private TextMarkerColorizer? _colorizer;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="TextMarkerService"/>.
    /// </summary>
    /// <param name="editor">Экземпляр <see cref="TextEditor"/>, для которого работает сервис.</param>
    public TextMarkerService(ICSharpCode.AvalonEdit.TextEditor editor)
    {
      this.editor = editor ?? throw new ArgumentNullException(nameof(editor));

      RebindToCurrentDocument();

      editor.DocumentChanged += (_, __) =>
      {
        RebindToCurrentDocument();
        editor.TextArea.TextView.InvalidateLayer(KnownLayer.Selection);
      };
    }

    /// <summary>
    /// Уровень слоя рендера. Используется AvalonEdit для наложения визуальных эффектов.
    /// </summary>
    public KnownLayer Layer => KnownLayer.Selection;

    /// <summary>
    /// Отрисовывает подсветку на фоне для всех активных маркеров.
    /// </summary>
    /// <param name="textView">Объект текстового отображения.</param>
    /// <param name="drawingContext">Контекст рисования WPF.</param>
    public void Draw(TextView textView, DrawingContext drawingContext)
    {
      if (editor.Document == null || !textView.VisualLinesValid)
      {
        Console.WriteLine("⚠ Draw: Документ не готов или VisualLines не валидны");
        return;
      }

      foreach (var marker in markers)
      {
        if (!marker.IsBackground)
          continue;

        foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, marker))
        {
          var brush = new SolidColorBrush(marker.BackgroundColor ?? Colors.Yellow);
          brush.Freeze();
          drawingContext.DrawRectangle(brush, null, new Rect(rect.Location, new Size(rect.Width, rect.Height)));
        }
      }
    }

    /// <summary>
    /// Перепривязывает внутреннее хранилище маркеров к текущему экземпляру <see cref="TextDocument"/> редактора.
    /// </summary>
    private void RebindToCurrentDocument()
    {
      if (editor.Document == null)
        return;

      markers = new TextSegmentCollection<TextMarker>(editor.Document);

      if (_colorizer != null)
        editor.TextArea.TextView.LineTransformers.Remove(_colorizer);

      _colorizer = new TextMarkerColorizer(markers);
      editor.TextArea.TextView.LineTransformers.Add(_colorizer);
      EnsureColorizerIsLast();
    }

    /// <summary>
    /// Очищает все существующие маркеры подсветки.
    /// </summary>
    public void ClearAllMarkers()
    {
      markers?.Clear();
      editor.TextArea.TextView.InvalidateLayer(KnownLayer.Selection);
      Console.WriteLine("Все маркеры удалены.");
    }

    /// <summary>
    /// Добавляет новый маркер подсветки.
    /// </summary>
    /// <param name="startOffset">Начальный индекс в тексте.</param>
    /// <param name="length">Длина выделения.</param>
    /// <param name="backgroundColor">Цвет фона подсветки.</param>
    public void AddMarker(int startOffset, int length, Color backgroundColor)
    {
      if (!TryNormalizeRange(ref startOffset, ref length))
        return;

      var marker = new TextMarker(startOffset, length)
      {
        BackgroundColor = backgroundColor,
      };

      markers.Add(marker);
      EnsureColorizerIsLast();
      editor.TextArea.TextView.InvalidateLayer(KnownLayer.Selection);
    }

    public void AddStyledMarker(int startOffset, int length, Color? foreground, FontWeight? weight = null)
    {
      if (!TryNormalizeRange(ref startOffset, ref length))
        return;

      var marker = new TextMarker(startOffset, length)
      {
        ForegroundColor = foreground,
        FontWeight = weight,
        IsBackground = false
      };

      markers.Add(marker);
      EnsureColorizerIsLast();
      editor.TextArea.TextView.InvalidateLayer(KnownLayer.Selection);
    }

    private bool TryNormalizeRange(ref int startOffset, ref int length)
    {
      if (editor.Document == null || length <= 0)
        return false;

      if (startOffset < 0 || startOffset >= editor.Document.TextLength)
        return false;

      int maxLength = editor.Document.TextLength - startOffset;
      if (maxLength <= 0)
        return false;

      if (length > maxLength)
        length = maxLength;

      return length > 0;
    }

    /// <summary>
    /// Держит colorizer маркеров последним в конвейере,
    /// чтобы стилевые маркеры могли перекрывать синтаксическую раскраску.
    /// </summary>
    private void EnsureColorizerIsLast()
    {
      if (_colorizer == null)
        return;

      var transformers = editor.TextArea.TextView.LineTransformers;
      if (transformers.Contains(_colorizer))
        transformers.Remove(_colorizer);

      transformers.Add(_colorizer);
    }


    /// <summary>
    /// Реализация текстового маркера.
    /// </summary>
    private sealed class TextMarker : TextSegment, ITextMarker
    {
      /// <summary>
      /// Создаёт новый текстовый маркер.
      /// </summary>
      /// <param name="startOffset">Начальная позиция.</param>
      /// <param name="length">Длина сегмента.</param>
      public TextMarker(int startOffset, int length)
      {
        StartOffset = startOffset;
        Length = length;
      }
      public bool IsBackground { get; set; } = true;

      /// <inheritdoc/>
      public object Tag { get; set; }

      /// <inheritdoc/>
      public Color? BackgroundColor { get; set; }

      /// <inheritdoc/>
      public Color? ForegroundColor { get; set; }

      /// <inheritdoc/>
      public FontWeight? FontWeight { get; set; }

      /// <inheritdoc/>
      public void Delete()
      {
        Length = 0;
      }
    }

    private sealed class TextMarkerColorizer : DocumentColorizingTransformer
    {
      private readonly TextSegmentCollection<TextMarker> _markers;

      public TextMarkerColorizer(TextSegmentCollection<TextMarker> markers)
      {
        _markers = markers;
      }

      protected override void ColorizeLine(DocumentLine line)
      {
        foreach (var marker in _markers.FindOverlappingSegments(line.Offset, line.Length))
        {
          int start = Math.Max(marker.StartOffset, line.Offset);
          int end = Math.Min(marker.EndOffset, line.EndOffset);
          if (end <= start)
            continue;

          ChangeLinePart(start, end, element =>
          {
            if (marker.ForegroundColor.HasValue)
            {
              element.TextRunProperties.SetForegroundBrush(new SolidColorBrush(marker.ForegroundColor.Value));
            }

            if (marker.FontWeight.HasValue)
            {
              var tf = element.TextRunProperties.Typeface;
              element.TextRunProperties.SetTypeface(new Typeface(
                tf.FontFamily,
                tf.Style,
                marker.FontWeight.Value,
                tf.Stretch));
            }
          });
        }
      }
    }
  }
}
