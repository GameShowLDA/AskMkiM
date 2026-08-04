using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Ask.UI.Shared.Controls
{
  /// <summary>
  /// Предоставляет редактируемый выпадающий список для выбора числового значения.
  /// </summary>
  public sealed class NumericComboBox : ComboBox
  {
    /// <summary>
    /// Зависимое свойство <see cref="Value"/>.
    /// </summary>
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
      nameof(Value),
      typeof(double),
      typeof(NumericComboBox),
      new FrameworkPropertyMetadata(
        0.0,
        FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
        OnValueChanged));

    /// <summary>
    /// Зависимое свойство <see cref="Minimum"/>.
    /// </summary>
    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
      nameof(Minimum),
      typeof(double),
      typeof(NumericComboBox),
      new PropertyMetadata(0.0, OnRangeChanged));

    /// <summary>
    /// Зависимое свойство <see cref="Maximum"/>.
    /// </summary>
    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
      nameof(Maximum),
      typeof(double),
      typeof(NumericComboBox),
      new PropertyMetadata(100.0, OnRangeChanged));

    /// <summary>
    /// Зависимое свойство <see cref="Increment"/>.
    /// </summary>
    public static readonly DependencyProperty IncrementProperty = DependencyProperty.Register(
      nameof(Increment),
      typeof(double),
      typeof(NumericComboBox),
      new PropertyMetadata(1.0, OnRangeChanged));

    /// <summary>
    /// Зависимое свойство <see cref="DecimalPlaces"/>.
    /// </summary>
    public static readonly DependencyProperty DecimalPlacesProperty = DependencyProperty.Register(
      nameof(DecimalPlaces),
      typeof(int),
      typeof(NumericComboBox),
      new PropertyMetadata(0, OnRangeChanged));

    /// <summary>
    /// Зависимое свойство <see cref="Unit"/>.
    /// </summary>
    public static readonly DependencyProperty UnitProperty = DependencyProperty.Register(
      nameof(Unit),
      typeof(string),
      typeof(NumericComboBox),
      new PropertyMetadata(string.Empty, OnRangeChanged));

    private bool isInternalUpdate;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="NumericComboBox"/>.
    /// </summary>
    public NumericComboBox()
    {
      IsEditable = true;
      IsTextSearchEnabled = false;
      Loaded += NumericComboBox_Loaded;
      DropDownOpened += NumericComboBox_DropDownOpened;
    }

    /// <summary>
    /// Возникает при изменении числового значения.
    /// </summary>
    public event RoutedPropertyChangedEventHandler<double>? ValueChanged;

    /// <summary>
    /// Выбранное числовое значение.
    /// </summary>
    public double Value
    {
      get => (double)GetValue(ValueProperty);
      set => SetValue(ValueProperty, value);
    }

    /// <summary>
    /// Минимальное допустимое значение.
    /// </summary>
    public double Minimum
    {
      get => (double)GetValue(MinimumProperty);
      set => SetValue(MinimumProperty, value);
    }

    /// <summary>
    /// Максимальное допустимое значение.
    /// </summary>
    public double Maximum
    {
      get => (double)GetValue(MaximumProperty);
      set => SetValue(MaximumProperty, value);
    }

    /// <summary>
    /// Шаг изменения значения.
    /// </summary>
    public double Increment
    {
      get => (double)GetValue(IncrementProperty);
      set => SetValue(IncrementProperty, value);
    }

    /// <summary>
    /// Количество знаков после десятичного разделителя.
    /// </summary>
    public int DecimalPlaces
    {
      get => (int)GetValue(DecimalPlacesProperty);
      set => SetValue(DecimalPlacesProperty, value);
    }

    /// <summary>
    /// Единица измерения, отображаемая рядом со значением.
    /// </summary>
    public string Unit
    {
      get => (string)GetValue(UnitProperty);
      set => SetValue(UnitProperty, value);
    }

    /// <inheritdoc />
    protected override void OnSelectionChanged(SelectionChangedEventArgs e)
    {
      base.OnSelectionChanged(e);
      if (!isInternalUpdate && SelectedItem is NumericOption option)
      {
        Value = option.Value;
      }
    }

    /// <inheritdoc />
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
      base.OnPreviewKeyDown(e);
      if (e.Key == Key.Enter)
      {
        CommitText();
        e.Handled = true;
      }
    }

    /// <inheritdoc />
    protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
      CommitText();
      base.OnLostKeyboardFocus(e);
    }

    private static void OnValueChanged(
      DependencyObject dependencyObject,
      DependencyPropertyChangedEventArgs e)
    {
      var control = (NumericComboBox)dependencyObject;
      double oldValue = (double)e.OldValue;
      double normalizedValue = control.Normalize((double)e.NewValue);

      if (!AreEqual(normalizedValue, (double)e.NewValue))
      {
        control.SetCurrentValue(ValueProperty, normalizedValue);
        return;
      }

      control.UpdateText();
      control.ValueChanged?.Invoke(
        control,
        new RoutedPropertyChangedEventArgs<double>(oldValue, normalizedValue));
    }

    private static void OnRangeChanged(
      DependencyObject dependencyObject,
      DependencyPropertyChangedEventArgs e)
    {
      var control = (NumericComboBox)dependencyObject;
      if (control.IsLoaded)
      {
        control.PopulateOptions();
        control.SetCurrentValue(ValueProperty, control.Normalize(control.Value));
        control.UpdateText();
      }
    }

    private static bool AreEqual(double first, double second)
    {
      return Math.Abs(first - second) < 0.0000001;
    }

    private void NumericComboBox_Loaded(object sender, RoutedEventArgs e)
    {
      PopulateOptions();
      SetCurrentValue(ValueProperty, Normalize(Value));
      UpdateText();
    }

    private void NumericComboBox_DropDownOpened(object? sender, EventArgs e)
    {
      PopulateOptions();
    }

    private void PopulateOptions()
    {
      double increment = Increment > 0 ? Increment : 1;
      double range = Math.Max(0, Maximum - Minimum);
      int requestedCount = (int)Math.Floor(range / increment) + 1;
      int stride = Math.Max(1, (int)Math.Ceiling(requestedCount / 160.0));
      double visibleIncrement = increment * stride;
      var options = new List<NumericOption>();

      for (double value = Minimum;
           value <= Maximum + (increment / 2) && options.Count < 162;
           value += visibleIncrement)
      {
        double normalized = Normalize(value);
        options.Add(new NumericOption(normalized, Format(normalized)));
      }

      if (options.Count == 0 || !AreEqual(options[^1].Value, Maximum))
      {
        double maximum = Normalize(Maximum);
        options.Add(new NumericOption(maximum, Format(maximum)));
      }

      if (!options.Any(option => AreEqual(option.Value, Value)))
      {
        double current = Normalize(Value);
        options.Add(new NumericOption(current, Format(current)));
        options.Sort((left, right) => left.Value.CompareTo(right.Value));
      }

      isInternalUpdate = true;
      try
      {
        ItemsSource = options;
        SelectedItem = options.FirstOrDefault(option => AreEqual(option.Value, Value));
      }
      finally
      {
        isInternalUpdate = false;
      }
    }

    private void CommitText()
    {
      string valueText = Text?.Trim() ?? string.Empty;
      if (!string.IsNullOrWhiteSpace(Unit)
          && valueText.EndsWith(Unit, StringComparison.OrdinalIgnoreCase))
      {
        valueText = valueText[..^Unit.Length].Trim();
      }

      valueText = valueText.Replace(',', '.');
      if (double.TryParse(
            valueText,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out double parsedValue))
      {
        Value = Normalize(parsedValue);
        PopulateOptions();
      }

      UpdateText();
    }

    private double Normalize(double value)
    {
      double minimum = Math.Min(Minimum, Maximum);
      double maximum = Math.Max(Minimum, Maximum);
      double increment = Increment > 0 ? Increment : 1;
      double clamped = Math.Clamp(value, minimum, maximum);
      double steps = Math.Round((clamped - minimum) / increment);
      return Math.Round(
        minimum + (steps * increment),
        Math.Clamp(DecimalPlaces, 0, 10));
    }

    private void UpdateText()
    {
      isInternalUpdate = true;
      try
      {
        Text = Format(Value);
      }
      finally
      {
        isInternalUpdate = false;
      }
    }

    private string Format(double value)
    {
      string number = value.ToString(
        $"F{Math.Clamp(DecimalPlaces, 0, 10)}",
        CultureInfo.CurrentCulture);
      return string.IsNullOrWhiteSpace(Unit) ? number : $"{number} {Unit}";
    }

    private sealed record NumericOption(double Value, string Display)
    {
      /// <inheritdoc />
      public override string ToString() => Display;
    }
  }
}
