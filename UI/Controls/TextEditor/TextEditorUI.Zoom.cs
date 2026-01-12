namespace UI.Controls.TextEditor
{
  /// <summary>
  /// Часть класса <see cref="TextEditorUI"/>, отвечающая за управление масштабом текста
  /// в редакторе: увеличение, уменьшение и сброс к значению по умолчанию.
  /// Логика вынесена в отдельный partial-файл для улучшения читаемости и структурирования.
  /// </summary>
  public partial class TextEditorUI
  {
    /// <summary>
    /// Минимально допустимый размер шрифта в редакторе.
    /// </summary>
    private const double _minFontSize = 12.0;

    /// <summary>
    /// Максимально допустимый размер шрифта в редакторе.
    /// </summary>
    private const double _maxFontSize = 48.0;

    /// <summary>
    /// Величина шага изменения шрифта при масштабировании.
    /// </summary>
    private const double _zoomStep = 1.0;

    /// <summary>
    /// Исходный размер шрифта, используемый для сброса масштаба.
    /// Значение устанавливается при создании текстового редактора.
    /// </summary>
    private double _defaultFontSize;

    /// <summary>
    /// Изменяет масштаб текста в редакторе:
    /// увеличивает или уменьшает размер шрифта на величину шага.
    /// Применяет ограничение по минимальному и максимальному размерам.
    /// </summary>
    /// <param name="zoom">
    /// true — увеличить масштаб,
    /// false — уменьшить.
    /// </param>
    private void Zoom(bool zoom)
    {
      double value = zoom ? textEditor.FontSize + _zoomStep : textEditor.FontSize - _zoomStep;
      SetFontSize(Clamp(value, _minFontSize, _maxFontSize));
    }

    /// <summary>
    /// Сбрасывает масштаб шрифта редактора к исходному значению,
    /// заданному при загрузке компонента.
    /// </summary>
    private void ResetZoom()
    {
      SetFontSize(_defaultFontSize);
    }

    /// <summary>
    /// Устанавливает новый размер шрифта для текстового редактора.
    /// </summary>
    /// <param name="size">Размер шрифта, который необходимо установить.</param>
    private void SetFontSize(double size)
    {
      textEditor.FontSize = size;
    }

    /// <summary>
    /// Ограничивает значение указанными минимальной и максимальной границами.
    /// </summary>
    /// <param name="value">Число, которое нужно ограничить.</param>
    /// <param name="min">Минимально допустимое значение.</param>
    /// <param name="max">Максимально допустимое значение.</param>
    /// <returns>Значение, приведённое к указанному диапазону.</returns>
    private double Clamp(double value, double min, double max)
       => Math.Max(min, Math.Min(max, value));
  }
}
