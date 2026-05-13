using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.HotkeysEnums;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Ask.UI.Controls.ProtocolNew;
using static Ask.Core.Services.EventCore.Adapters.ExecutionEventAdapter;

namespace UI.Components
{
  /// <summary>
  /// Логика взаимодействия для InputField.xaml.
  /// </summary>
  public partial class InputField : UserControl, IInputFieldAccessor, IInputHighlightService
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
    /// Свойство зависимости, определяющее, отображается ли BusSelector (AB1..AB4).
    /// </summary>
    public static readonly DependencyProperty IsBusGroupVisibleProperty =
        DependencyProperty.Register(nameof(IsBusGroupVisible), typeof(bool), typeof(InputField), new PropertyMetadata(false));

    public static readonly DependencyProperty IsModuleInputModeProperty =
    DependencyProperty.Register(
        nameof(IsModuleInputMode),
        typeof(bool),
        typeof(InputField),
        new PropertyMetadata(false, OnModeChanged));

    public bool IsModuleInputMode
    {
      get => (bool)GetValue(IsModuleInputModeProperty);
      set => SetValue(IsModuleInputModeProperty, value);
    }

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

    /// <summary>
    /// Показывает или скрывает BusSelector (AB1..AB4).
    /// </summary>
    public bool IsBusGroupVisible
    {
      get => (bool)GetValue(IsBusGroupVisibleProperty);
      set => SetValue(IsBusGroupVisibleProperty, value);
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
    /// Получает или задаёт номер проверяемого устройства в формате a.b.
    /// </summary>
    public string TestedNumber
    {
      get => TestedNumberBox.Text;
      set => TestedNumberBox.Text = value;
    }

    /// <summary>
    /// Получает или задаёт номер проверяющего устройства в формате a.b.
    /// </summary>
    public string TesterNumber
    {
      get => TesterNumberBox.Text;
      set => TesterNumberBox.Text = value;
    }

    /// <summary>
    /// Получает или задаёт диапазон проверки в формате списка чисел и диапазонов (например, "1-3,5").
    /// </summary>
    public string TestRange
    {
      get => TestRangeBox.Text;
      set => TestRangeBox.Text = value;
    }

    /// <summary>
    /// Только геттер для получения активной шины.
    /// </summary>
    public BusPoint ActiveBus { get; private set; }

    /// <summary>
    /// Активная группа шин (AB1..AB4).
    /// </summary>
    public SwitchingBusNew ActiveBusGroup { get; private set; } = SwitchingBusNew.AB1;

    #endregion

    /// <summary>
    /// Флаг, предотвращающий реакцию обработчиков Checked/Unchecked на изменения,
    /// выполненные программно (во время синхронизации чекбоксов с <see cref="ActiveBusGroup"/>).
    /// </summary>
    private bool _busGroupInternalChange;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="InputField"/>.
    /// </summary>
    public InputField()
    {
      InitializeComponent();

      _busGroupInternalChange = true;
      SetActiveBusGroup(SwitchingBusNew.AB1);
      _busGroupInternalChange = false;

      SubscribeToValidationEvents();
      ShinaACheckBox.IsChecked = true;
      PreviewKeyDown += HotkeyChecked;
      Unloaded += InputField_Unloaded;
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

    private void InputField_Unloaded(object sender, RoutedEventArgs e)
    {
      InputValidationEvents.OnInvalidFirstPoint -= HighlightFirstTextBox;
      InputValidationEvents.OnInvalidSecondPoint -= HighlightSecondTextBox;
      InputValidationEvents.OnInvalidElectricalParameter -= HighlightElectricalTextBox;
      InputValidationEvents.OnDuplicatePoints -= HighlightBothPoints;
      ActionExecutor.StartProcessing -= ActionExecutor_StartProcessing;
      PreviewKeyDown -= HotkeyChecked;
      Unloaded -= InputField_Unloaded;
    }

    /// <summary>
    /// Обрабатывает начало и окончание выполнения шага.
    /// Переключает режим отображения между полями ввода и сводной информацией,
    /// а также формирует текст заголовков с текущими значениями.
    /// </summary>
    /// <param name="obj">
    /// Флаг выполнения шага: true — шаг выполняется, false — режим редактирования.
    /// </param>
    private void ActionExecutor_StartProcessing(bool obj)
    {
      var firstBaseText = "Первая точка";
      var secondBaseText = "Вторая точка";
      var electricalBaseText = "Электрический параметр";
      var timeBaseText = "Время выполнения";
      var timeRampBaseText = "Время нарастания";
      var voltageBaseText = "Напряжение";
      var busBaseText = "Шина для проверки";
      var busGroupBaseText = "Группа шин";

      Visibility visibility = obj ? Visibility.Collapsed : Visibility.Visible;
      FirstTextBox.Visibility = visibility;
      SecondTextBox.Visibility = visibility;
      ElectricalTextBox.Visibility = visibility;
      TimeTextBox.Visibility = visibility;
      TimeRampTextBox.Visibility = visibility;
      VoltageTextBox.Visibility = visibility;
      BusBorder.Visibility = visibility;
      BusGroupBorder.Visibility = visibility;

      if (obj)
      {
        headerFirstData.Text = $"{firstBaseText}: {FirstTextBox.Text}";
        headerSecondData.Text = $"{secondBaseText}: {SecondTextBox.Text}";
        headerElectricalData.Text = $"{electricalBaseText}: {ElectricalTextBox.Text} {ElectricalTextBox.Unit}";
        headerTimeData.Text = $"{timeBaseText}: {TimeTextBox.Text} {TimeTextBox.Unit}";
        headerTimeRampData.Text = $"{timeRampBaseText}: {TimeRampTextBox.Text} {TimeRampTextBox.Unit}";
        headerVoltageData.Text = $"{voltageBaseText}: {VoltageTextBox.Text} {VoltageTextBox.Unit}";
        headerBusData.Text = $"{busBaseText}: {ActiveBus}";
        headerBusGroupData.Text = $"{busGroupBaseText}: {ActiveBusGroup}";
      }
      else
      {
        headerFirstData.Text = $"{firstBaseText}: вида a.b.c";
        headerSecondData.Text = $"{secondBaseText}: вида a.b.c";
        headerElectricalData.Text = $"{electricalBaseText}";
        headerTimeData.Text = $"{timeBaseText} в сек.";
        headerTimeRampData.Text = $"{timeRampBaseText} в сек.";
        headerVoltageData.Text = $"{voltageBaseText} в В.";
        headerBusData.Text = $"{busBaseText}";
        headerBusGroupData.Text = busGroupBaseText;
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

    /// <summary>
    /// Обрабатывает изменение режима ввода.
    /// Переключает отображение между режимом модульного ввода
    /// и режимом параметров шага.
    /// </summary>
    /// <param name="d">Объект, для которого изменилось свойство.</param>
    /// <param name="e">Данные изменения свойства.</param>
    private static void OnModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      var control = (InputField)d;

      bool isModuleInput = (bool)e.NewValue;

      control.TestInputGrid.Visibility = isModuleInput
          ? Visibility.Visible
          : Visibility.Collapsed;

      control.TestStepParametersGrid.Visibility = isModuleInput
          ? Visibility.Collapsed
          : Visibility.Visible;
    }

    /// <summary>
    /// Возвращает основные значения ввода в зависимости от активного режима.
    /// В режиме модульного ввода возвращает номера устройств и диапазон,
    /// в обычном режиме — точки и электрический параметр.
    /// </summary>
    /// <returns>
    /// Кортеж строковых значений, соответствующих текущему режиму ввода.
    /// </returns>
    public (string First, string Second, string Parameter) GetValues()
    {
      return InvokeSafe(() =>
          !IsModuleInputMode
              ? (FirstPoint, SecondPoint, ElectricalParameter)
              : (TestedNumber, TesterNumber, TestRange)
      );
    }

    /// <summary>
    /// Устанавливает активную группу шин и синхронизирует состояние чекбоксов AB1..AB4.
    /// </summary>
    /// <param name="bus">Новая активная группа шин.</param>
    private void SetActiveBusGroup(SwitchingBusNew bus)
    {
      ActiveBusGroup = bus;

      BusAB1CheckBox.IsChecked = bus == SwitchingBusNew.AB1;
      BusAB2CheckBox.IsChecked = bus == SwitchingBusNew.AB2;
      BusAB3CheckBox.IsChecked = bus == SwitchingBusNew.AB3;
      BusAB4CheckBox.IsChecked = bus == SwitchingBusNew.AB4;
    }

    /// <summary>
    /// Возвращает следующую группу шин по кругу: AB1 → AB2 → AB3 → AB4 → AB1.
    /// Используется, чтобы при снятии галочки с активной группы всегда оставалась выбранной какая-либо группа.
    /// </summary>
    private SwitchingBusNew NextBusGroup(SwitchingBusNew current) => current switch
    {
      SwitchingBusNew.AB1 => SwitchingBusNew.AB2,
      SwitchingBusNew.AB2 => SwitchingBusNew.AB3,
      SwitchingBusNew.AB3 => SwitchingBusNew.AB4,
      _ => SwitchingBusNew.AB1
    };

    /// <summary>
    /// Обработчик включения чекбокса группы шин (AB1..AB4).
    /// При выборе одной группы остальные автоматически снимаются.
    /// </summary>
    private void BusGroup_Checked(object sender, RoutedEventArgs e)
    {
      if (_busGroupInternalChange) return;
      if (sender is not CheckBox cb) return;

      _busGroupInternalChange = true;

      if (cb == BusAB1CheckBox) SetActiveBusGroup(SwitchingBusNew.AB1);
      else if (cb == BusAB2CheckBox) SetActiveBusGroup(SwitchingBusNew.AB2);
      else if (cb == BusAB3CheckBox) SetActiveBusGroup(SwitchingBusNew.AB3);
      else if (cb == BusAB4CheckBox) SetActiveBusGroup(SwitchingBusNew.AB4);

      _busGroupInternalChange = false;
    }

    /// <summary>
    /// Обработчик выключения чекбокса группы шин.
    /// Если пользователь пытается снять галочку с текущей активной группы, автоматически выбирается следующая группа,
    /// чтобы не допустить состояния "ничего не выбрано".
    /// </summary>
    private void BusGroup_Unchecked(object sender, RoutedEventArgs e)
    {
      if (_busGroupInternalChange) return;
      if (sender is not CheckBox cb) return;

      bool wasActive =
          (cb == BusAB1CheckBox && ActiveBusGroup == SwitchingBusNew.AB1) ||
          (cb == BusAB2CheckBox && ActiveBusGroup == SwitchingBusNew.AB2) ||
          (cb == BusAB3CheckBox && ActiveBusGroup == SwitchingBusNew.AB3) ||
          (cb == BusAB4CheckBox && ActiveBusGroup == SwitchingBusNew.AB4);

      _busGroupInternalChange = true;

      if (wasActive)
        SetActiveBusGroup(NextBusGroup(ActiveBusGroup));
      else
        SetActiveBusGroup(ActiveBusGroup);

      _busGroupInternalChange = false;
    }

    /// <summary>
    /// Возвращает значение времени выполнения теста.
    /// </summary>
    /// <returns>Строковое представление времени.</returns>
    public string GetTime() => InvokeSafe(() => Time);

    /// <summary>
    /// Возвращает значение времени нарастания,
    /// приводя разделитель дробной части к локальному формату.
    /// </summary>
    /// <returns>Строковое представление времени нарастания.</returns>
    public string GetTimeRamp() => InvokeSafe(() => TimeRamp.Replace('.', ','));

    /// <summary>
    /// Возвращает значение напряжения.
    /// </summary>
    /// <returns>Строковое представление напряжения.</returns>
    public string GetVoltage() => InvokeSafe(() => Voltage);

    /// <summary>
    /// Возвращает текущую активную шину.
    /// </summary>
    /// <returns>Значение активной шины.</returns>
    public BusPoint GetBus() => InvokeSafe(() => ActiveBus);

    /// <summary>
    /// Возвращает выбранную группу шин для парного подключения.
    /// </summary>
    /// <returns>Выбранная группа шин.</returns>
    public SwitchingBusNew GetPairBus() => InvokeSafe(() => ActiveBusGroup);

    /// <summary>
    /// Подсвечивает поле номера проверяемого устройства как содержащее ошибку.
    /// </summary>
    public void HighlightTestedNumber() => TestedNumberBox.DataError();

    /// <summary>
    /// Подсвечивает поле номера проверяющего устройства как содержащее ошибку.
    /// </summary>
    public void HighlightTesterNumber() => TesterNumberBox.DataError();

    /// <summary>
    /// Подсвечивает поле диапазона проверки как содержащее ошибку.
    /// </summary>
    public void HighlightTestRange() => TestRangeBox.DataError();

    /// <summary>
    /// Безопасно выполняет функцию в UI-потоке.
    /// Если вызов производится не из UI-потока,
    /// выполнение маршалится через Dispatcher.
    /// </summary>
    /// <typeparam name="T">Тип возвращаемого значения.</typeparam>
    /// <param name="func">Функция для выполнения.</param>
    /// <returns>Результат выполнения функции.</returns>
    private T InvokeSafe<T>(Func<T> func)
    {
      if (Dispatcher.CheckAccess())
        return func();

      return Dispatcher.Invoke(func);
    }

    private void HotkeyChecked(object sender, KeyEventArgs e)
    {
      switch (e.Key)
      {
        case Key.F5:
          ExecutionControlEventAdapter.Raise(ExecutionControlButton.Run);
          e.Handled = true;
          break;

        case Key.F10:
          ExecutionControlEventAdapter.Raise(ExecutionControlButton.StepOver);
          e.Handled = true;
          break;

        case Key.F11:
          ExecutionControlEventAdapter.Raise(ExecutionControlButton.StepInto);
          e.Handled = true;
          break;
      }
    }
  }
}
