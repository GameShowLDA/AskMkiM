using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis;
using Ask.DataBase.Engine.Static.Devices;
using Ask.Engine.Tests.LegacyAsk;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace UI.Controls.ExecutorControls.LegacyAsk;

/// <summary>
/// Контрол запуска тестов старой АСК, перенесенных в новую оболочку.
/// </summary>
public partial class LegacyAskTestControl : UserControl, ILegacyAskTestSelectionProvider, INotifyPropertyChanged
{
  private readonly LegacyAskTestKind _kind;
  private readonly string? _initialTestCode;
  private IChassisManager? _selectedChassis;
  private LegacyAskTestDescriptor? _selectedTest;
  private string _selectedInstruction = string.Empty;

  /// <inheritdoc />
  public event PropertyChangedEventHandler? PropertyChanged;

  /// <summary>
  /// Список стоек старой АСК для выбора в интерфейсе.
  /// </summary>
  public IReadOnlyList<IChassisManager> ChassisItems { get; private set; } = [];

  /// <summary>
  /// Список тестов выбранной группы.
  /// </summary>
  public IReadOnlyList<LegacyAskTestDescriptor> TestItems { get; private set; } = [];

  /// <summary>
  /// Список вводных параметров выбранного теста.
  /// </summary>
  public ObservableCollection<LegacyAskTestParameterItem> ParameterItems { get; } = [];

  /// <summary>
  /// Инструкция подключения для выбранного теста.
  /// </summary>
  public string SelectedInstruction
  {
    get => _selectedInstruction;
    private set
    {
      if (_selectedInstruction == value)
      {
        return;
      }

      _selectedInstruction = value;
      OnPropertyChanged();
    }
  }

  /// <summary>
  /// Выбранная стойка старой АСК.
  /// </summary>
  public IChassisManager? SelectedChassis
  {
    get => _selectedChassis;
    set
    {
      if (ReferenceEquals(_selectedChassis, value))
      {
        return;
      }

      _selectedChassis = value;
      OnPropertyChanged();
    }
  }

  /// <summary>
  /// Выбранный тест старой АСК.
  /// </summary>
  public LegacyAskTestDescriptor? SelectedTest
  {
    get => _selectedTest;
    set
    {
      if (ReferenceEquals(_selectedTest, value))
      {
        return;
      }

      _selectedTest = value;
      OnPropertyChanged();
      RebuildParameterItems();
    }
  }

  /// <summary>
  /// Инициализирует контрол тестов погрешности измерения АСК.
  /// </summary>
  public LegacyAskTestControl()
    : this(LegacyAskTestKind.MeasurementAccuracy, null)
  {
  }

  /// <summary>
  /// Инициализирует контрол для указанной группы legacy-тестов.
  /// </summary>
  /// <param name="kind">Группа тестов старой АСК.</param>
  public LegacyAskTestControl(LegacyAskTestKind kind)
    : this(kind, null)
  {
  }

  /// <summary>
  /// Инициализирует контрол для указанной группы legacy-тестов и заранее выбранного теста.
  /// </summary>
  /// <param name="kind">Группа тестов старой АСК.</param>
  /// <param name="initialTestCode">Код теста, который нужно выбрать после загрузки.</param>
  public LegacyAskTestControl(LegacyAskTestKind kind, string? initialTestCode)
  {
    _kind = kind;
    _initialTestCode = initialTestCode;
    InitializeComponent();
    LoadSelectors();
    new LegacyAskTestExecutor(this).InitializeSettings(ProtocolUI);
  }

  /// <inheritdoc />
  public IChassisManager? GetSelectedChassis()
  {
    return SelectedChassis;
  }

  /// <inheritdoc />
  public LegacyAskTestDescriptor? GetSelectedTest()
  {
    return SelectedTest;
  }

  /// <inheritdoc />
  public IReadOnlyDictionary<string, string> GetInputParameters()
  {
    return ParameterItems
      .Where(x => x.EditorKind != "Info")
      .ToDictionary(x => x.Key, x => x.EditorKind == "Bool" ? (x.BoolValue ? "1" : "0") : x.Value);
  }

  /// <summary>
  /// Загружает список стоек АСК и список тестов выбранной группы.
  /// </summary>
  private void LoadSelectors()
  {
    ChassisItems = ChassisManagers.GetAllAsync()
      .GetAwaiter()
      .GetResult()
      .Where(IsLegacyAskChassis)
      .OrderBy(x => x.Number)
      .ToList();

    SelectedChassis = ChassisItems.FirstOrDefault();

    TestItems = LegacyAskTestCatalog.GetTests(_kind);
    SelectedTest = TestItems.FirstOrDefault(x => string.Equals(x.Code, _initialTestCode, StringComparison.OrdinalIgnoreCase))
      ?? TestItems.FirstOrDefault();
  }

  /// <summary>
  /// Определяет, является ли стойка старым тестером АСК.
  /// </summary>
  /// <param name="chassis">Стойка из конфигурации оборудования.</param>
  /// <returns>Признак стойки АСК.</returns>
  private static bool IsLegacyAskChassis(IChassisManager chassis)
  {
    return string.Equals(chassis.Name, "Тестер АСК", StringComparison.OrdinalIgnoreCase)
      || (chassis.DeviceClass?.EndsWith(".ManagerASKMKI", StringComparison.Ordinal) ?? false);
  }

  /// <summary>
  /// Перестраивает список вводных параметров при смене теста.
  /// </summary>
  private void RebuildParameterItems()
  {
    ParameterItems.Clear();

    if (SelectedTest == null)
    {
      SelectedInstruction = "Выберите тест.";
      return;
    }

    foreach (var parameter in CreateParameters(SelectedTest.Code))
    {
      ParameterItems.Add(parameter);
    }

    SelectedInstruction = GetInstruction(SelectedTest.Code);
  }

  /// <summary>
  /// Создает набор полей ввода, соответствующий форме старой программы MKI.
  /// </summary>
  /// <param name="testCode">Код теста MKI.</param>
  /// <returns>Набор параметров выбранного теста.</returns>
  private static IReadOnlyList<LegacyAskTestParameterItem> CreateParameters(string testCode)
  {
    return testCode switch
    {
      "E4TPGR" => ResistanceUiParameters(false),
      "R4TPGR" => ResistanceUiParameters(true),
      "R2TPGR" => ResistanceUiParameters(false),
      "RV7PGR" => ResistanceOmmeterParameters(),
      "PKIPGR" => InsulationResistanceParameters(),
      "UV7PGR" or "UACPPGR" => DcVoltageParameters(),
      "IV7PGR" => PintCurrentParameters(),
      "RACPPGR" => ResistanceAdcParameters(),
      "UPPUPGR" => PpuVoltageParameters(),
      "VV7PGR" => AcVoltageParameters(),
      "TIMEPGR" => TimeIntervalParameters(),
      "EPREZ" => ComparatorThresholdParameters(),
      "KUPGR" => LeakageCurrentParameters(),
      "IEPGR" => CapacitanceParameters(),
      "UPKIPGR" => PkiVoltageParameters(),
      _ => CommonExecutionFlags()
    };
  }

  /// <summary>
  /// Создает поля для тестов E4TPGR, R4TPGR и R2TPGR.
  /// </summary>
  /// <param name="relayFourPoint">Признак релейного 4-проводного теста R4TPGR.</param>
  /// <returns>Набор параметров теста сопротивления.</returns>
  private static IReadOnlyList<LegacyAskTestParameterItem> ResistanceUiParameters(bool relayFourPoint)
  {
    string pointHint = relayFourPoint
      ? "Адрес точки в формате СК.БК.Точка. Номер БК должен быть нечетным."
      : "Адрес точки в формате СК.БК.Точка.";

    var items = new List<LegacyAskTestParameterItem>
    {
      Text("StartPoint", "Начальная точка", "1.1.1", pointHint),
      Text("EndPoint", "Конечная точка", "1.1.100", pointHint),
      Text("ResistanceOhm", "Rэт, Ом", "10.000000", "Значение, выставленное на магазине сопротивлений."),
      Text("PintCurrentMa", "Iпинт, мА", "10.000", "Ток, выставляемый на ПИНТ."),
      Text("PintVoltageV", "Uпинт, В", "5.000", "Напряжение, выставляемое на ПИНТ.")
    };

    items.AddRange(CommonExecutionFlags());

    return items;
  }

  /// <summary>
  /// Создает поля для теста RV7PGR.
  /// </summary>
  /// <returns>Набор параметров омметра.</returns>
  private static IReadOnlyList<LegacyAskTestParameterItem> ResistanceOmmeterParameters()
  {
    return
    [
      Text("StartPoint", "Начальная точка", "1.1.1", "Адрес точки в формате СК.БК.Точка."),
      Text("EndPoint", "Конечная точка", "1.1.100", "Адрес точки в формате СК.БК.Точка."),
      Text("ResistanceOhm", "Rэт, Ом", "10.000000", "Значение, выставленное на магазине сопротивлений."),
      .. CommonExecutionFlags()
    ];
  }

  /// <summary>
  /// Создает поля для тестов PKIPGR.
  /// </summary>
  /// <returns>Набор параметров сопротивления изоляции.</returns>
  private static IReadOnlyList<LegacyAskTestParameterItem> InsulationResistanceParameters()
  {
    return
    [
      Text("StartPoint", "Начальная точка", "1.1.1", "Адрес точки в формате СК.БК.Точка."),
      Text("EndPoint", "Конечная точка", "1.1.100", "Адрес точки в формате СК.БК.Точка."),
      Text("ResistanceMOhm", "Rэт, МОм", "100.000", "Значение магазина сопротивлений."),
      Choice("PkiVoltageRange", "DUпки", "1", ["1", "2", "3", "4", "5"], "Диапазон напряжения: 1(5В), 2(30В), 3(100В), 4(250В), 5(499В)."),
      Bool("UsePint4", "Использовать ПИНТ4", false, "[x] вместо БН использовать ПИНТ4."),
      Text("Pint4VoltageV", "Напряжение ПИНТ4, В", "5.000", "Используется только при включенном ПИНТ4."),
      .. CommonExecutionFlags()
    ];
  }

  /// <summary>
  /// Создает поля для тестов UV7PGR и UACPPGR.
  /// </summary>
  /// <returns>Набор параметров постоянного напряжения.</returns>
  private static IReadOnlyList<LegacyAskTestParameterItem> DcVoltageParameters()
  {
    return
    [
      Text("StartPoint", "Начальная точка", "1.1.1", "Адрес точки в формате СК.БК.Точка."),
      Text("EndPoint", "Конечная точка", "1.1.100", "Адрес точки в формате СК.БК.Точка."),
      Text("VoltageV", "Напряжение, В", "5.000", "Напряжение, выставляемое на ПИНТ4."),
      Bool("ExternalSource", "Внешний источник", false, "[x] измерять напряжение, поданное на A1/B1 от внешнего источника."),
      .. CommonExecutionFlags()
    ];
  }

  /// <summary>
  /// Создает поля для теста IV7PGR.
  /// </summary>
  /// <returns>Набор параметров тока ПИНТ.</returns>
  private static IReadOnlyList<LegacyAskTestParameterItem> PintCurrentParameters()
  {
    return
    [
      Text("StartPoint", "Начальная точка", "1.1.1", "Вход для подключения токового входа эталонного амперметра."),
      Text("EndPoint", "Конечная точка", "1.1.100", "Вход для подключения токового выхода эталонного амперметра."),
      Choice("PintNumber", "Nпинт", "4", ["3", "4"], "Номер используемого ПИНТ."),
      Text("PintCurrentMa", "Iпинт, мА", "10.000", "Ток, выставляемый на ПИНТ."),
      Text("PintVoltageV", "Uпинт, В", "5.000", "Напряжение, выставляемое на ПИНТ."),
      .. CommonExecutionFlags()
    ];
  }

  /// <summary>
  /// Создает поля для теста RACPPGR.
  /// </summary>
  /// <returns>Набор параметров измерения сопротивления через АЦП.</returns>
  private static IReadOnlyList<LegacyAskTestParameterItem> ResistanceAdcParameters()
  {
    return
    [
      Text("StartPoint", "Начальная точка", "1.1.1", "Адрес точки в формате СК.БК.Точка."),
      Text("EndPoint", "Конечная точка", "1.1.100", "Адрес точки в формате СК.БК.Точка."),
      Text("ResistanceOhm", "Rэт, Ом", "10.000000", "Значение, выставленное на магазине сопротивлений."),
      .. CommonExecutionFlags()
    ];
  }

  /// <summary>
  /// Создает поля для теста UPPUPGR.
  /// </summary>
  /// <returns>Набор параметров напряжения ППУ.</returns>
  private static IReadOnlyList<LegacyAskTestParameterItem> PpuVoltageParameters()
  {
    return
    [
      Text("StartPoint", "Начальная точка", "1.1.1", "Адрес точки подключения эталонного вольтметра."),
      Text("EndPoint", "Конечная точка", "1.1.100", "Адрес точки подключения эталонного вольтметра."),
      Text("PpuVoltageV", "Uппу, В", "500", "Напряжение ППУ, выставляемое на шинах."),
      .. CommonExecutionFlags()
    ];
  }

  /// <summary>
  /// Создает поля для теста VV7PGR.
  /// </summary>
  /// <returns>Набор параметров переменного напряжения.</returns>
  private static IReadOnlyList<LegacyAskTestParameterItem> AcVoltageParameters()
  {
    return
    [
      Text("StartPoint", "Начальная точка", "1.1.1", "Адрес точки подключения эталонного вольтметра."),
      Text("EndPoint", "Конечная точка", "1.1.100", "Адрес точки подключения эталонного вольтметра."),
      Text("AcVoltageV", "Uпеременное, В", "30", "Напряжение, выставляемое на шинах."),
      Choice("AcSource", "Источник Uперем", "ППУ", ["ППУ", "Внешний"], "Выбор источника: ППУ или внешний."),
      .. CommonExecutionFlags()
    ];
  }

  /// <summary>
  /// Создает поля для теста TIMEPGR.
  /// </summary>
  /// <returns>Набор параметров измерения времени.</returns>
  private static IReadOnlyList<LegacyAskTestParameterItem> TimeIntervalParameters()
  {
    return
    [
      Text("StartPoint", "Начальная точка (Выход +)", "1.1.1", "Вход АСК для подключения генератора импульсов."),
      Text("EndPoint", "Конечная точка (Общий -)", "1.1.100", "Вход АСК для общего провода генератора."),
      Choice("StartSignalSign", "Сигнал Старт", ">", [">", "<"], "Условие начала счета времени."),
      Text("StartSignalV", "Старт, В", "2.000", "Порог сигнала Старт."),
      Choice("StopSignalSign", "Сигнал Стоп", ">", [">", "<"], "Условие завершения счета времени."),
      Text("StopSignalV", "Стоп, В", "4.000", "Порог сигнала Стоп."),
      Text("PulseLengthSec", "Длительность импульса, с", "0.100000", "Длительность импульса, не менее 100 мкс."),
      .. CommonExecutionFlags()
    ];
  }

  /// <summary>
  /// Создает поля для теста EPREZ.
  /// </summary>
  /// <returns>Набор параметров порога компаратора.</returns>
  private static IReadOnlyList<LegacyAskTestParameterItem> ComparatorThresholdParameters()
  {
    return
    [
      Text("StartBk", "Начальный БК", "1.1", "БК для подключения кабеля: СК.БК."),
      Text("EndBk", "Конечный БК", "1.2", "БК для подключения кабеля: СК.БК."),
      .. CommonExecutionFlags()
    ];
  }

  /// <summary>
  /// Создает поля для теста KUPGR.
  /// </summary>
  /// <returns>Набор параметров тока утечки.</returns>
  private static IReadOnlyList<LegacyAskTestParameterItem> LeakageCurrentParameters()
  {
    return
    [
      Text("StartPoint", "Начальная точка", "1.1.1", "Катод диода подключается к начальной точке."),
      Text("EndPoint", "Конечная точка", "1.1.100", "Анод диода подключается к конечной точке."),
      Text("LeakageCurrentMkA", "Iутечки, мкА", "10.000", "Ток утечки."),
      Choice("PkiVoltageRange", "DUпки", "1", ["1", "2", "3", "4", "5"], "Диапазон напряжения: 1(5В), 2(30В), 3(100В), 4(250В), 5(499В)."),
      Bool("UsePint4", "Использовать ПИНТ4", false, "[x] вместо БН использовать ПИНТ4."),
      Text("Pint4VoltageV", "Напряжение ПИНТ4, В", "5.000", "Используется только при включенном ПИНТ4."),
      .. CommonExecutionFlags()
    ];
  }

  /// <summary>
  /// Создает поля для теста IEPGR.
  /// </summary>
  /// <returns>Набор параметров измерения емкости.</returns>
  private static IReadOnlyList<LegacyAskTestParameterItem> CapacitanceParameters()
  {
    return
    [
      Text("StartPoint", "Начальная точка", "1.1.1", "Вход АСК для подключения емкости."),
      Text("EndPoint", "Конечная точка", "1.1.100", "Вход АСК для подключения емкости."),
      Text("LcBk", "БК для LC-метра", "1.2", "БК для подключения LC-метра, должен отличаться от БК точек."),
      Text("CapacitanceMkF", "Cэт, мкФ", "1.000", "Значение эталонной емкости."),
      .. CommonExecutionFlags()
    ];
  }

  /// <summary>
  /// Создает поля для теста UPKIPGR.
  /// </summary>
  /// <returns>Набор параметров напряжения ПКИ.</returns>
  private static IReadOnlyList<LegacyAskTestParameterItem> PkiVoltageParameters()
  {
    return
    [
      Text("StartPoint", "Начальная точка", "1.1.1", "Адрес точки подключения внешнего эталонного вольтметра."),
      Text("EndPoint", "Конечная точка", "1.1.100", "Адрес точки подключения внешнего эталонного вольтметра."),
      Choice("PkiVoltageRange", "Uпки", "1", ["1", "2", "3", "4", "5"], "Напряжение ПКИ задается диапазоном из конфигурации аппаратуры."),
      .. CommonExecutionFlags()
    ];
  }

  /// <summary>
  /// Создает общие флаги выполнения теста.
  /// </summary>
  /// <returns>Флаги остановки по ошибке и повтора измерения.</returns>
  private static IReadOnlyList<LegacyAskTestParameterItem> CommonExecutionFlags()
  {
    return
    [
      Bool("StopOnError", "Останов по ошибке", true, "Делать останов при обнаружении ошибки при прогоне теста."),
      Bool("RepeatMeasurement", "Повтор измерения", false, "Фиксация состояния аппаратуры по ошибке.")
    ];
  }

  /// <summary>
  /// Создает текстовое поле.
  /// </summary>
  private static LegacyAskTestParameterItem Text(string key, string label, string value, string hint)
  {
    return new LegacyAskTestParameterItem(key, label, "Text", value, hint);
  }

  /// <summary>
  /// Создает поле выбора из списка.
  /// </summary>
  private static LegacyAskTestParameterItem Choice(string key, string label, string value, IReadOnlyList<string> options, string hint)
  {
    return new LegacyAskTestParameterItem(key, label, "Choice", value, hint, options);
  }

  /// <summary>
  /// Создает поле-флаг.
  /// </summary>
  private static LegacyAskTestParameterItem Bool(string key, string label, bool value, string hint)
  {
    return new LegacyAskTestParameterItem(key, label, "Bool", label, hint) { BoolValue = value };
  }

  /// <summary>
  /// Создает информационную строку без ввода.
  /// </summary>
  private static LegacyAskTestParameterItem Info(string key, string text)
  {
    return new LegacyAskTestParameterItem(key, text, "Info", string.Empty, string.Empty);
  }

  /// <summary>
  /// Возвращает инструкцию подключения для выбранного теста.
  /// </summary>
  /// <param name="testCode">Код теста MKI.</param>
  /// <returns>Текст инструкции подключения.</returns>
  private static string GetInstruction(string testCode)
  {
    return testCode switch
    {
      "R4TPGR" => "Подключите магазин сопротивлений к указанным точкам и параллельно к одноименным входам в последующем четном БК. Нажмите <Enter>.",
      "E4TPGR" or "R2TPGR" or "RV7PGR" or "RACPPGR" => "Подключите магазин сопротивлений к указанным точкам и нажмите <Enter>.",
      "PKIPGR" => "Подключите магазин сопротивлений к указанным точкам и нажмите <Enter>.",
      "UV7PGR" or "UACPPGR" => "Подключите эталонный вольтметр к указанным точкам.",
      "IV7PGR" => "Подключите эталонный амперметр к указанным точкам.",
      "UPPUPGR" or "VV7PGR" => "Подключите к заданным точкам эталонный вольтметр.",
      "TIMEPGR" => "Подключите генератор импульсов: выход к начальной точке, общий к конечной. Установите параметры импульса и нажмите <Enter>.",
      "EPREZ" => "С помощью кабелей-заглушек подключите магазин сопротивлений к разъемам заданных БК. Нажмите <Enter>.",
      "KUPGR" => "Подключите диод: катод к начальной точке, анод к конечной точке. Нажмите <Enter>.",
      "IEPGR" => "Подключите емкость к указанным точкам. Подключите LC-метр к шинам A и B выбранного БК и нажмите <Enter>.",
      "UPKIPGR" => "Подключите к заданным точкам внешний эталонный вольтметр.",
      _ => "Проверьте подключение и нажмите <Enter>."
    };
  }

  /// <summary>
  /// Уведомляет привязки WPF об изменении свойства.
  /// </summary>
  /// <param name="propertyName">Имя измененного свойства.</param>
  private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
}

/// <summary>
/// Описывает одно поле формы запуска legacy-теста АСК.
/// </summary>
public sealed class LegacyAskTestParameterItem : INotifyPropertyChanged
{
  private string _value;
  private bool _boolValue;

  /// <summary>
  /// Инициализирует поле формы запуска.
  /// </summary>
  /// <param name="key">Внутренний ключ параметра.</param>
  /// <param name="label">Название поля.</param>
  /// <param name="editorKind">Тип редактора: Text, Choice, Bool или Info.</param>
  /// <param name="value">Текущее значение.</param>
  /// <param name="hint">Подсказка из старой программы.</param>
  /// <param name="options">Варианты выбора для выпадающего списка.</param>
  public LegacyAskTestParameterItem(
    string key,
    string label,
    string editorKind,
    string value,
    string hint,
    IReadOnlyList<string>? options = null)
  {
    Key = key;
    Label = label;
    EditorKind = editorKind;
    _value = value;
    Hint = hint;
    Options = options ?? [];
  }

  /// <inheritdoc />
  public event PropertyChangedEventHandler? PropertyChanged;

  /// <summary>
  /// Внутренний ключ параметра.
  /// </summary>
  public string Key { get; }

  /// <summary>
  /// Название поля.
  /// </summary>
  public string Label { get; }

  /// <summary>
  /// Тип редактора.
  /// </summary>
  public string EditorKind { get; }

  /// <summary>
  /// Подсказка из старой программы.
  /// </summary>
  public string Hint { get; }

  /// <summary>
  /// Варианты выбора.
  /// </summary>
  public IReadOnlyList<string> Options { get; }

  /// <summary>
  /// Текстовое значение поля.
  /// </summary>
  public string Value
  {
    get => _value;
    set
    {
      if (_value == value)
      {
        return;
      }

      _value = value;
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
    }
  }

  /// <summary>
  /// Логическое значение поля-флага.
  /// </summary>
  public bool BoolValue
  {
    get => _boolValue;
    set
    {
      if (_boolValue == value)
      {
        return;
      }

      _boolValue = value;
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BoolValue)));
    }
  }
}
