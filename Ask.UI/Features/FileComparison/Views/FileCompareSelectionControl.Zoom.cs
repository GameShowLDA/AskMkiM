using System.Windows;
using System.Windows.Input;

namespace Ask.UI.Features.FileComparison.Views
{
  /// <summary>
  /// Управляет общим масштабом текста и строк панелей сравнения.
  /// </summary>
  public partial class FileCompareSelectionControl
  {
    /// <summary>
    /// Минимальный размер текста, совпадающий с ограничением текстового редактора.
    /// </summary>
    private const double MinDiffFontSize = 12;

    /// <summary>
    /// Максимальный размер текста, совпадающий с ограничением текстового редактора.
    /// </summary>
    private const double MaxDiffFontSize = 48;

    /// <summary>
    /// Исходный размер текста сравнения.
    /// </summary>
    private const double DefaultDiffFontSize = 14;

    /// <summary>
    /// Шаг изменения размера текста при вращении колеса мыши.
    /// </summary>
    private const double DiffZoomStep = 1;

    /// <summary>
    /// Текущий размер текста обеих панелей сравнения.
    /// </summary>
    private double _diffFontSize = DefaultDiffFontSize;

    /// <summary>
    /// Размер текста в обеих панелях сравнения.
    /// </summary>
    public double DiffFontSize
    {
      get => _diffFontSize;
      private set
      {
        if (_diffFontSize == value)
        {
          return;
        }

        _diffFontSize = value;
        OnPropertyChanged();
        OnPropertyChanged(nameof(DiffLineNumberFontSize));
        OnPropertyChanged(nameof(DiffRowHeight));
        OnPropertyChanged(nameof(DiffMarkerColumnWidth));
        OnPropertyChanged(nameof(DiffLineNumberColumnWidth));
        OnPropertyChanged(nameof(DiffGutterWidth));
      }
    }

    /// <summary>
    /// Размер номеров строк в обеих панелях сравнения.
    /// </summary>
    public double DiffLineNumberFontSize => DiffFontSize - 1;

    /// <summary>
    /// Высота одной визуальной строки текста и минимальная высота пары сравнения.
    /// </summary>
    public double DiffRowHeight => Math.Ceiling(22 * DiffFontSize / DefaultDiffFontSize);

    /// <summary>
    /// Ширина колонки признака добавления или удаления строки.
    /// </summary>
    public GridLength DiffMarkerColumnWidth => new(Math.Ceiling(24 * DiffFontSize / DefaultDiffFontSize));

    /// <summary>
    /// Ширина колонки номеров строк.
    /// </summary>
    public GridLength DiffLineNumberColumnWidth => new(Math.Ceiling(58 * DiffFontSize / DefaultDiffFontSize));

    /// <summary>
    /// Ширина фона колонки номеров с учётом маркера и левой границы строки.
    /// </summary>
    public double DiffGutterWidth => DiffMarkerColumnWidth.Value + DiffLineNumberColumnWidth.Value + 2;

    /// <summary>
    /// Изменяет общий размер текста на один пункт в диапазоне 12–48 пунктов.
    /// </summary>
    /// <param name="increase">Признак увеличения размера текста.</param>
    internal void ZoomDiffText(bool increase)
    {
      DiffFontSize = Math.Clamp(
        DiffFontSize + (increase ? DiffZoomStep : -DiffZoomStep),
        MinDiffFontSize,
        MaxDiffFontSize);
    }

    /// <summary>
    /// Масштабирует текст при вращении колеса с нажатой клавишей Ctrl.
    /// </summary>
    /// <param name="sender">Панель сравнения, получившая событие.</param>
    /// <param name="e">Параметры вращения колеса мыши.</param>
    private void DiffLines_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
      if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
      {
        return;
      }

      if (e.Delta != 0)
      {
        ZoomDiffText(e.Delta > 0);
      }

      e.Handled = true;
    }
  }
}
