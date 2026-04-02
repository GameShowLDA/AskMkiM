using Ask.Core.Shared.DTO.Executor.MeasurementError;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace UI.Components.MeasurementErrorCardControl
{
  /// <summary>
  /// Элемент управления для отображения и редактирования одного диапазона погрешностей.
  /// Поднимает событие ValueChanged при изменении любого поля ввода.
  /// </summary>
  public partial class MeasurementErrorRangeView : UserControl
  {
    /// <summary>
    /// Модель данных диапазона погрешности.
    /// </summary>
    public MeasurementErrorRangeEntity Range { get; private set; } = new();

    /// <summary>
    /// Событие, возникающее при изменении любого значения пользователем.
    /// </summary>
    public event EventHandler? ValueChanged;

    public MeasurementErrorRangeView()
    {
      InitializeComponent();
      Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
      UpdateUI();

      // подписка на изменения — реагируем на любое изменение текста
      MinInput.InputBox.TextChanged += OnAnyInputChanged;
      MaxInput.InputBox.TextChanged += OnAnyInputChanged;
      PercentageInput.InputBox.TextChanged += OnAnyInputChanged;
      NumericInput.InputBox.TextChanged += OnAnyInputChanged;
    }

    private void OnAnyInputChanged(object sender, TextChangedEventArgs e)
    {
      ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Устанавливает диапазон для редактирования.
    /// </summary>
    public void SetRange(MeasurementErrorRangeEntity range)
    {
      Range = range;
      UpdateUI();
    }

    /// <summary>
    /// Единица измерения (например, "Ом", "МОм", "В").
    /// </summary>
    public string Unit
    {
      get => (string)GetValue(UnitProperty);
      set => SetValue(UnitProperty, value);
    }

    public static readonly DependencyProperty UnitProperty =
        DependencyProperty.Register(
            nameof(Unit),
            typeof(string),
            typeof(MeasurementErrorRangeView),
            new PropertyMetadata(string.Empty, OnUnitChanged));

    private static void OnUnitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is MeasurementErrorRangeView control && control.UnitTextBlock != null)
        control.UnitTextBlock.Text = e.NewValue?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Обновляет UI по данным модели.
    /// </summary>
    private void UpdateUI()
    {
      RangeHeaderText.Text = $"Диапазон {Range.MinValue} – {Range.MaxValue?.ToString() ?? "∞"}";
      MinInput.InputBox.Text = Range.MinValue.ToString(CultureInfo.InvariantCulture);
      MaxInput.InputBox.Text = Range.MaxValue?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
      PercentageInput.InputBox.Text = Range.PercentageError.ToString("F3", CultureInfo.InvariantCulture);
      NumericInput.InputBox.Text = Range.NumericError.ToString("F3", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Применяет изменения из UI обратно в модель.
    /// </summary>
    public void ApplyChanges()
    {
      if (double.TryParse(MinInput.InputBox.Text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var min))
        Range.MinValue = min;

      if (double.TryParse(MaxInput.InputBox.Text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var max))
        Range.MaxValue = max;
      else
        Range.MaxValue = null;

      if (double.TryParse(PercentageInput.InputBox.Text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var percent))
        Range.PercentageError = percent;

      if (double.TryParse(NumericInput.InputBox.Text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var numeric))
        Range.NumericError = numeric;
    }
  }
}
