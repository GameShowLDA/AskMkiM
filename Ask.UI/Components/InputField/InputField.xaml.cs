using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Input;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.HotkeysEnums;
using Ask.Core.Shared.Metadata.Static;
using Ask.UI.Features.ProtocolNew.Execution;
using Ask.UI.Features.ProtocolNew.Hotkeys;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Ask.Core.Services.EventCore.Adapters.ExecutionEventAdapter;

namespace Ask.UI.Components.InputField
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
    public string FirstPoint => FirstPointTextBox.Text;

    /// <summary>
    /// Вторая точка.
    /// </summary>
    public string SecondPoint => LastPointTextBox.Text;

    /// <summary>
    /// Электрический параметр.
    /// </summary>
    public string ElectricalParameter => ElectricalTextBox.Text;

    /// <summary>
    /// Время выполнения теста.
    /// </summary>
    public string Time => TimeTextBox.Text;

    /// <summary>
    /// Время нарастания напряжения.
    /// </summary>
    public string TimeRamp => TimeRampTextBox.Text;

    /// <summary>
    /// Напряжение.
    /// </summary>
    public string Voltage => VoltageTextBox.Text;

    /// <summary>
    /// Получает или задаёт номер проверяемого устройства в формате a.b.
    /// </summary>
    public string TestedNumber => TestedNumberBox.Text;

    /// <summary>
    /// Получает или задаёт номер проверяющего устройства в формате a.b.
    /// </summary>
    public string TesterNumber => TesterNumberBox.Text;

    /// <summary>
    /// Получает или задаёт диапазон проверки в формате списка чисел и диапазонов (например, "1-3,5").
    /// </summary>
    public string TestRange => TestRangeBox.Text;

    /// <summary>
    /// Только геттер для получения активной шины.
    /// </summary>
    public BusPoint ActiveBus => BusSelector.SelectedBus;

    /// <summary>
    /// Активная группа шин (AB1..AB4).
    /// </summary>
    public SwitchingBusNew ActiveBusGroup => BusGroupSelector.SelectedBusGroup;

    #endregion

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="InputField"/>.
    /// </summary>
    public InputField()
    {
      InitializeComponent();

      SubscribeToValidationEvents();
      PreviewKeyDown += HotkeyChecked;
      Unloaded += InputField_Unloaded;

      SetBaseData();
    }

    private void SetBaseData()
    {
      var data = MeasurementTestData.GetData();

      if (data.FirstPoint != null)
      {
        FirstPointTextBox.Text = data.FirstPoint.ToString();
      }

      if (data.SecondPoint != null)
      {
        LastPointTextBox.Text = data.SecondPoint.ToString();
      }

      if (data.Param != 0)
      {
        ElectricalTextBox.Text = data.Param.ToString();
      }

      if (data.Time != 0)
      {
        TimeTextBox.Text = data.Time.ToString();
      }

      if (data.RampTime != 0)
      {
        TimeRampTextBox.Text = data.RampTime.ToString();
      }

      if (data.Voltage != 0)
      {
        VoltageTextBox.Text = data.Voltage.ToString();
      }

      if (data.ActiveBus != default)
      {
        BusSelector.SelectedBus = data.ActiveBus;
      }

      if (data.ActivePairBus != default)
      {
        BusGroupSelector.SelectedBusGroup = data.ActivePairBus;
      }

      if (!string.IsNullOrEmpty(data.TestedNumber))
      {
        TestedNumberBox.Text = data.TestedNumber;
      }

      if (!string.IsNullOrEmpty(data.TesterNumber))
      {
        TesterNumberBox.Text = data.TesterNumber;
      }

      if (!string.IsNullOrEmpty(data.TestRange))
      {
        TestRangeBox.Text = data.TestRange;
      }
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
      FirstPointTextBox.IsExecuting = obj;
      LastPointTextBox.IsExecuting = obj;
      TimeTextBox.IsExecuting = obj;
      TimeRampTextBox.IsExecuting = obj;
      ElectricalTextBox.IsExecuting = obj;
      VoltageTextBox.IsExecuting = obj;
      BusSelector.IsExecuting = obj;
      BusGroupSelector.IsExecuting = obj;
      TestedNumberBox.IsExecuting = obj;
      TesterNumberBox.IsExecuting = obj;
    }

    /// <summary>
    /// Подсветка поля первой точки.
    /// </summary>
    private void HighlightFirstTextBox() =>
      FirstPointTextBox.DataError();

    /// <summary>
    /// Подсветка поля второй точки.
    /// </summary>
    private void HighlightSecondTextBox() =>
      LastPointTextBox.DataError();

    /// <summary>
    /// Подсветка поля параметра.
    /// </summary>
    private void HighlightElectricalTextBox() =>
      ElectricalTextBox.DataError();

    /// <summary>
    /// Подсветка обоих точек при совпадении.
    /// </summary>
    private void HighlightBothPoints()
    {
      FirstPointTextBox.DataError();
      LastPointTextBox.DataError();
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

    /// <inheritdoc />
    public InputValidationResult ValidatePoints()
    {
      return InvokeSafe(() =>
      {
        var errors = new[]
        {
          FirstPointTextBox.Validate(),
          LastPointTextBox.Validate()
        }
        .Where(error => error != null)
        .Cast<Ask.Core.Services.Errors.Models.ErrorItem>();

        return new InputValidationResult(errors);
      });
    }

    /// <inheritdoc />
    public InputValidationResult ValidateElectricalParameters()
    {
      return InvokeSafe(() =>
      {
        if (IsModuleInputMode)
          return new InputValidationResult(Array.Empty<Ask.Core.Services.Errors.Models.ErrorItem>());

        var errors = new List<Ask.Core.Services.Errors.Models.ErrorItem>();
        var parameterError = ElectricalTextBox.Validate();
        if (parameterError != null)
          errors.Add(parameterError);

        if (IsVoltageVisible)
        {
          var voltageError = VoltageTextBox.Validate();
          if (voltageError != null)
            errors.Add(voltageError);
        }

        return new InputValidationResult(errors);
      });
    }

    /// <inheritdoc />
    public InputValidationResult ValidateTimeParameters()
    {
      return InvokeSafe(() =>
      {
        if (IsModuleInputMode)
          return new InputValidationResult(Array.Empty<Ask.Core.Services.Errors.Models.ErrorItem>());

        var errors = new List<Ask.Core.Services.Errors.Models.ErrorItem>();
        if (IsTimeVisible)
        {
          var timeError = TimeTextBox.Validate();
          if (timeError != null)
            errors.Add(timeError);
        }

        if (IsTimeRampVisible)
        {
          var rampError = TimeRampTextBox.Validate();
          if (rampError != null)
            errors.Add(rampError);
        }

        return new InputValidationResult(errors);
      });
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
          KeyboardManager.OnRunOrPausePressed?.Invoke();
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
