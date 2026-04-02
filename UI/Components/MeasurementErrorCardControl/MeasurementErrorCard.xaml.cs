using System.Windows.Controls;
using UI.Helpers;

namespace UI.Components.MeasurementErrorCardControl
{
  /// <summary>
  /// Карточка для настройки погрешностей измерений.
  /// </summary>
  public partial class MeasurementErrorCard : UserControl
  {
    private FocusNavigationManager _focusManager;
    public MeasurementErrorCard()
    {
      InitializeComponent();
      // Loaded += MeasurementErrorCard_Loaded;
    }

    //private void MeasurementErrorCard_Loaded(object sender, RoutedEventArgs e)
    //{
    //  UpdateDisplay();

    //  PercentageInput.InputBox.PreviewTextInput += InputBox_PreviewTextInput;
    //  NumericInput.InputBox.PreviewTextInput += InputBox_PreviewTextInput;

    //  DataObject.AddPastingHandler(PercentageInput.InputBox, OnPasteHandler);
    //  DataObject.AddPastingHandler(NumericInput.InputBox, OnPasteHandler);

    //  _focusManager = new FocusNavigationManager(this);
    //  _focusManager.AddRange(new Control[] { PercentageInput.InputBox, NumericInput.InputBox, SaveButton });
    //}

    //public static readonly DependencyProperty TypeCommandProperty = DependencyProperty.Register(
    //    nameof(TypeCommand),
    //    typeof(TypeCommand),
    //    typeof(MeasurementErrorCard),
    //    new PropertyMetadata(TypeCommand.KC, OnTypeCommandChanged));

    ///// <summary>
    ///// Тип команды, определяющий режим метрологии.
    ///// </summary>
    //public TypeCommand TypeCommand
    //{
    //  get => (TypeCommand)GetValue(TypeCommandProperty);
    //  set => SetValue(TypeCommandProperty, value);
    //}

    ///// <summary>
    ///// Событие вызывается при нажатии на кнопку "Сохранить".
    ///// </summary>
    //public event EventHandler<MeasurementErrorCardEventArgs>? SaveButtonClicked;

    ///// <summary>
    ///// Устанавливает или возвращает значение процентов в поле ввода.
    ///// </summary>
    //public double PercentageValue
    //{
    //  get
    //  {
    //    if (double.TryParse(PercentageInput.InputBox.Text.Replace('.', ','), out var value))
    //      return value;
    //    return 0;
    //  }
    //  set
    //  {
    //    PercentageInput.InputBox.Text = value.ToString("F2").Replace('.', ',');
    //  }
    //}

    ///// <summary>
    ///// Устанавливает или возвращает значение числовой погрешности в поле ввода.
    ///// </summary>
    //public double NumericValue
    //{
    //  get
    //  {
    //    if (double.TryParse(NumericInput.InputBox.Text.Replace('.', ','), out var value))
    //      return value;
    //    return 0;
    //  }
    //  set
    //  {
    //    NumericInput.InputBox.Text = value.ToString("F2").Replace('.', ',');
    //  }
    //}



    //private static void OnTypeCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    //{
    //  if (d is MeasurementErrorCard card)
    //  {
    //    card.UpdateDisplay();
    //  }
    //}

    //private void UpdateDisplay()
    //{
    //  var (title, unit) = GetCommandInfo(TypeCommand);
    //  TitleTextBlock.Text = title;
    //  UnitTextBlock.Text = unit;
    //}

    //private static (string DisplayName, string Unit) GetCommandInfo(TypeCommand type)
    //{
    //  var memberInfo = typeof(TypeCommand).GetMember(type.ToString()).FirstOrDefault();
    //  var attribute = memberInfo?.GetCustomAttributes(typeof(CommandInfoAttribute), false)
    //      .FirstOrDefault() as CommandInfoAttribute;

    //  return attribute != null
    //      ? (attribute.DisplayName, attribute.Unit)
    //      : (type.ToString(), string.Empty);
    //}

    //private void InputBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    //{
    //  if (sender is TextBox textBox)
    //  {
    //    // Заменяем точку на запятую
    //    if (e.Text == ".")
    //    {
    //      e.Handled = true;
    //      var caretIndex = textBox.CaretIndex;
    //      textBox.Text = textBox.Text.Insert(caretIndex, ",");
    //      textBox.CaretIndex = caretIndex + 1;
    //    }
    //    else
    //    {
    //      // Разрешаем только цифры и запятую
    //      e.Handled = !char.IsDigit(e.Text, 0) && e.Text != ",";
    //    }
    //  }
    //}

    //private void OnPasteHandler(object sender, DataObjectPastingEventArgs e)
    //{
    //  if (e.DataObject.GetDataPresent(typeof(string)))
    //  {
    //    var text = (string)e.DataObject.GetData(typeof(string)) ?? "";
    //    text = text.Replace('.', ',');
    //    if (!text.All(c => char.IsDigit(c) || c == ','))
    //    {
    //      e.CancelCommand();
    //    }
    //  }
    //  else
    //  {
    //    e.CancelCommand();
    //  }
    //}

    //private void SaveButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    //{
    //  SaveButtonClicked?.Invoke(this, new MeasurementErrorCardEventArgs(TypeCommand, PercentageValue, NumericValue));
    //}

    //public async void ShowResult(bool isSuccess)
    //{
    //  SuccessIcon.Visibility = isSuccess ? Visibility.Visible : Visibility.Collapsed;
    //  ErrorIcon.Visibility = isSuccess ? Visibility.Collapsed : Visibility.Visible;

    //  await Task.Delay(2000);

    //  SuccessIcon.Visibility = Visibility.Collapsed;
    //  ErrorIcon.Visibility = Visibility.Collapsed;
    //}
  }
}
