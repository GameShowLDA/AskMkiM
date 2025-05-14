using System.Windows;
using System.Windows.Controls;
using UI.Controls.ProtocolNew;
using Utilities.Events;
using static NewCore.Enum.DeviceEnum;

namespace UI.Components
{
  /// <summary>
  /// Логика взаимодействия для InputField.xaml.
  /// </summary>
  public partial class InputField : UserControl
  {
    #region Свойства отображения элементов.

    /// <summary>
    /// Свойство зависимости, определяющее, отображается ли поле времени.
    /// </summary>
    public static readonly DependencyProperty IsTimeVisibleProperty =
        DependencyProperty.Register(nameof(IsTimeVisible), typeof(bool), typeof(InputField), new PropertyMetadata(false));

    /// <summary>
    /// Свойство зависимости, определяющее, отображается ли поле напряжения.
    /// </summary>
    public static readonly DependencyProperty IsVoltageVisibleProperty =
        DependencyProperty.Register(nameof(IsVoltageVisible), typeof(bool), typeof(InputField), new PropertyMetadata(false));

    /// <summary>
    /// Свойство зависимости, определяющее, отображается ли поле времени нарастания.
    /// </summary>
    public static readonly DependencyProperty IsTimeRampVisibleProperty =
        DependencyProperty.Register(nameof(IsTimeRampVisible), typeof(bool), typeof(InputField), new PropertyMetadata(false));

    /// <summary>
    /// Свойство зависимости, определяющее, отображается ли выбор шины.
    /// </summary>
    public static readonly DependencyProperty IsBusVisibleProperty =
        DependencyProperty.Register(nameof(IsBusVisible), typeof(bool), typeof(InputField), new PropertyMetadata(false));

    /// <summary>
    /// Свойство зависимости для единицы измерения, отображаемой рядом с полем ввода.
    /// </summary>
    public static readonly DependencyProperty UnitElectricalProperty =
        DependencyProperty.Register(nameof(UnitElectrical), typeof(string), typeof(InputField), new PropertyMetadata(string.Empty));

    /// <summary>
    /// Показывает или скрывает поле времени.
    /// </summary>
    public bool IsTimeVisible
    {
      get => (bool)GetValue(IsTimeVisibleProperty);
      set => SetValue(IsTimeVisibleProperty, value);
    }

    /// <summary>
    /// Показывает или скрывает поле напряжения.
    /// </summary>
    public bool IsVoltageVisible
    {
      get => (bool)GetValue(IsVoltageVisibleProperty);
      set => SetValue(IsVoltageVisibleProperty, value);
    }

    /// <summary>
    /// Показывает или скрывает поле времени нарастания.
    /// </summary>
    public bool IsTimeRampVisible
    {
      get => (bool)GetValue(IsTimeRampVisibleProperty);
      set => SetValue(IsTimeRampVisibleProperty, value);
    }

    /// <summary>
    /// Показывает или скрывает поле времени нарастания.
    /// </summary>
    public bool IsBusVisible
    {
      get => (bool)GetValue(IsBusVisibleProperty);
      set => SetValue(IsBusVisibleProperty, value);
    }

    /// <summary>
    /// Устанавливает единицу измерения электрического параметра.
    /// </summary>
    public string UnitElectrical
    {
      get => (string)GetValue(UnitElectricalProperty);
      set => SetValue(UnitElectricalProperty, value);
    }

    #endregion

    #region Св-ва получения данных

    /// <summary>
    /// Первая точка.
    /// </summary>
    public string FirstPoint
    {
      get => FirstTextBox.Text;
      set => FirstTextBox.Text = value;
    }

    /// <summary>
    /// Вторая точка.
    /// </summary>
    public string SecondPoint
    {
      get => SecondTextBox.Text;
      set => SecondTextBox.Text = value;
    }

    /// <summary>
    /// Электрический параметр.
    /// </summary>
    public string ElectricalParameter
    {
      get => ElectricalTextBox.Text;
      set => ElectricalTextBox.Text = value;
    }

    /// <summary>
    /// Время выполнения теста.
    /// </summary>
    public string Time
    {
      get => TimeTextBox.Text;
      set => TimeTextBox.Text = value;
    }

    /// <summary>
    /// Время выполнения теста.
    /// </summary>
    public string TimeRamp
    {
      get => TimeRampTextBox.Text;
      set => TimeRampTextBox.Text = value;
    }

    /// <summary>
    /// Напряжение.
    /// </summary>
    public string Voltage
    {
      get => VoltageTextBox.Text;
      set => VoltageTextBox.Text = value;
    }

    /// <summary>
    /// Только геттер для получения активной шины.
    /// </summary>
    public BusPoint ActiveBus { get; private set; }

    #endregion

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="InputField"/>.
    /// </summary>
    public InputField()
    {
      InitializeComponent();
      SubscribeToValidationEvents();
      ShinaACheckBox.IsChecked = true;
    }

    /// <summary>
    /// Подписка на глобальные события валидации.
    /// </summary>
    private void SubscribeToValidationEvents()
    {
      InputValidationEvents.OnInvalidFirstPoint += HighlightFirstTextBox;
      InputValidationEvents.OnInvalidSecondPoint += HighlightSecondTextBox;
      InputValidationEvents.OnInvalidElectricalParameter += HighlightElectricalTextBox;
      InputValidationEvents.OnDuplicatePoints += HighlightBothPoints;
      ActionExecutor.StartProcessing += ActionExecutor_StartProcessing;
    }

    private void ActionExecutor_StartProcessing(bool obj)
    {
      var firstBaseText = "Первая точка";
      var secondBaseText = "Вторая точка";
      var electricalBaseText = "Электрический параметр";
      var timeBaseText = "Время выполнения";
      var timeRampBaseText = "Время нарастания";
      var voltageBaseText = "Напряжение";
      var BusBaseText = "Шина для проверки";

      Visibility visibility = obj ? Visibility.Collapsed : Visibility.Visible;
      FirstTextBox.Visibility = visibility;
      SecondTextBox.Visibility = visibility;
      ElectricalTextBox.Visibility = visibility;
      TimeTextBox.Visibility = visibility;
      TimeRampTextBox.Visibility = visibility;
      VoltageTextBox.Visibility = visibility;
      BusBorder.Visibility = visibility;

      if (obj)
      {
        headerFirstData.Text = $"{firstBaseText}: {FirstTextBox.Text}";
        headerSecondData.Text = $"{secondBaseText}: {SecondTextBox.Text}";
        headerElectricalData.Text = $"{electricalBaseText}: {ElectricalTextBox.Text} {ElectricalTextBox.Unit}";
        headerTimeData.Text = $"{timeBaseText}: {TimeTextBox.Text} {TimeTextBox.Unit}";
        headerTimeRampData.Text = $"{timeRampBaseText}: {TimeRampTextBox.Text} {TimeRampTextBox.Unit}";
        headerVoltageData.Text = $"{voltageBaseText}: {VoltageTextBox.Text} {VoltageTextBox.Unit}";
        headerBusData.Text = $"{BusBaseText}: {ActiveBus}";
      }
      else
      {
        headerFirstData.Text = $"{firstBaseText}: вида a.b.c";
        headerSecondData.Text = $"{secondBaseText}: вида a.b.c";
        headerElectricalData.Text = $"{electricalBaseText}";
        headerTimeData.Text = $"{timeBaseText} в сек.";
        headerTimeRampData.Text = $"{timeRampBaseText} в сек.";
        headerVoltageData.Text = $"{voltageBaseText} в В.";
        headerBusData.Text = $"{BusBaseText}";
      }
    }

    /// <summary>
    /// Подсветка поля первой точки.
    /// </summary>
    private void HighlightFirstTextBox()
    {
      FirstTextBox.DataError();
    }

    /// <summary>
    /// Подсветка поля второй точки.
    /// </summary>
    private void HighlightSecondTextBox()
    {
      SecondTextBox.DataError();
    }

    /// <summary>
    /// Подсветка поля параметра.
    /// </summary>
    private void HighlightElectricalTextBox()
    {
      ElectricalTextBox.DataError();
    }

    /// <summary>
    /// Подсветка обоих точек при совпадении.
    /// </summary>
    private void HighlightBothPoints()
    {
      SecondTextBox.DataError();
    }

    /// <summary>
    /// Обрабатывает включение шины. Если одна шина включена, другая автоматически выключается.
    /// </summary>
    /// <param name="sender">Источник события (чекбокс).</param>
    /// <param name="e">Параметры события.</param>
    private void Switch_Checked(object sender, RoutedEventArgs e)
    {
      var checkbox = sender as CheckBox;

      if (checkbox == ShinaACheckBox && ShinaACheckBox.IsChecked == true)
      {
        ShinaBCheckBox.IsChecked = false;
        ActiveBus = BusPoint.A;
      }

      if (checkbox == ShinaBCheckBox && ShinaBCheckBox.IsChecked == true)
      {
        ShinaACheckBox.IsChecked = false;
        ActiveBus = BusPoint.B;
      }
    }

    /// <summary>
    /// Обрабатывает выключение шины. Если одна шина выключена, автоматически включается другая.
    /// </summary>
    /// <param name="sender">Источник события (чекбокс).</param>
    /// <param name="e">Параметры события.</param>
    private void Switch_Unchecked(object sender, RoutedEventArgs e)
    {
      var checkbox = sender as CheckBox;

      if (checkbox == ShinaACheckBox && ShinaACheckBox.IsChecked == false)
      {
        ShinaBCheckBox.IsChecked = true;
        ActiveBus = BusPoint.B;
      }

      if (checkbox == ShinaBCheckBox && ShinaBCheckBox.IsChecked == false)
      {
        ShinaACheckBox.IsChecked = true;
        ActiveBus = BusPoint.A;
      }
    }
  }
}
