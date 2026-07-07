namespace Ask.Engine.Tests.LegacyAsk;

/// <summary>
/// Хранит перечень тестов старой АСК в составе меню MKI: Prec, Serv, Relay и Time.
/// </summary>
public static class LegacyAskTestCatalog
{
  private static readonly IReadOnlyList<LegacyAskTestDescriptor> MeasurementAccuracyTests =
  [
    new(LegacyAskTestKind.MeasurementAccuracy, "E4TPGR", "Измерение R электронная 4-х точка", "Проверка погрешности измерения сопротивления электронной четырехпроводной схемой.", LegacyAskRequiredDevice.Voltmeter),
    new(LegacyAskTestKind.MeasurementAccuracy, "R4TPGR", "Измерение R релейная 4-х точка", "Проверка погрешности измерения сопротивления релейной четырехпроводной схемой.", LegacyAskRequiredDevice.Voltmeter),
    new(LegacyAskTestKind.MeasurementAccuracy, "R2TPGR", "Измерение R 2-х проводная схема", "Проверка погрешности измерения сопротивления двухпроводной схемой.", LegacyAskRequiredDevice.Voltmeter),
    new(LegacyAskTestKind.MeasurementAccuracy, "RV7PGR", "Измерение R в режиме омметра", "Проверка погрешности режима омметра цифрового вольтметра.", LegacyAskRequiredDevice.Voltmeter),
    new(LegacyAskTestKind.MeasurementAccuracy, "PKIPGR", "Измерение R с помощью ПКИ", "Проверка погрешности измерения сопротивления изоляции через ПКИ.", LegacyAskRequiredDevice.Pki),
    new(LegacyAskTestKind.MeasurementAccuracy, "UV7PGR", "Измерение Uпост вольтметром", "Проверка погрешности измерения постоянного напряжения цифровым вольтметром.", LegacyAskRequiredDevice.Voltmeter),
    new(LegacyAskTestKind.MeasurementAccuracy, "IV7PGR", "Измерение I ПИНТов", "Проверка погрешности тока ПИНТ по цифровому вольтметру.", LegacyAskRequiredDevice.Pint),
    new(LegacyAskTestKind.MeasurementAccuracy, "UACPPGR", "Измерение Uпост с помощью АЦП", "Проверка погрешности измерения постоянного напряжения через АЦП.", LegacyAskRequiredDevice.Adc),
    new(LegacyAskTestKind.MeasurementAccuracy, "RACPPGR", "Измерение R с помощью АЦП", "Проверка погрешности измерения сопротивления через АЦП.", LegacyAskRequiredDevice.Adc),
    new(LegacyAskTestKind.MeasurementAccuracy, "UPPUPGR", "Измерение Uппу", "Проверка погрешности напряжения пробойной установки.", LegacyAskRequiredDevice.Ppu),
    new(LegacyAskTestKind.MeasurementAccuracy, "VV7PGR", "Измерение U переменного", "Проверка погрешности измерения переменного напряжения цифровым вольтметром.", LegacyAskRequiredDevice.Voltmeter),
    new(LegacyAskTestKind.MeasurementAccuracy, "TIMEPGR", "Измерение интервалов времени", "Проверка погрешности измерения временных интервалов.", LegacyAskRequiredDevice.Timer),
    new(LegacyAskTestKind.MeasurementAccuracy, "EPREZ", "Порог срабатывания компаратора", "Проверка порога срабатывания электронного компаратора.", LegacyAskRequiredDevice.Commutator),
    new(LegacyAskTestKind.MeasurementAccuracy, "KUPGR", "Измерение Iут с помощью ПКИ", "Проверка погрешности измерения тока утечки ПКИ.", LegacyAskRequiredDevice.Pki),
    new(LegacyAskTestKind.MeasurementAccuracy, "IEPGR", "Измерение емкости", "Проверка погрешности измерения емкости.", LegacyAskRequiredDevice.LcMeter),
    new(LegacyAskTestKind.MeasurementAccuracy, "UPKIPGR", "Измерение Uпки", "Проверка погрешности напряжения ПКИ.", LegacyAskRequiredDevice.Pki)
  ];

  private static readonly IReadOnlyList<LegacyAskTestDescriptor> AdditionalServiceTests =
  [
    new(LegacyAskTestKind.AdditionalService, "VKLST", "Выключение СК", "Проверка включения и выключения питания СК.", LegacyAskRequiredDevice.Commutator),
    new(LegacyAskTestKind.AdditionalService, "EK1TM", "Подключение точек к ЭК", "Проверка подключения точек к шинам электронного коммутатора.", LegacyAskRequiredDevice.Commutator),
    new(LegacyAskTestKind.AdditionalService, "EK1LT", "Лишние ЭК-подключения", "Поиск лишних подключений к электронному коммутатору.", LegacyAskRequiredDevice.Commutator),
    new(LegacyAskTestKind.AdditionalService, "EKEPM", "КЗ шины ЭК", "Проверка короткого замыкания шины электронного коммутатора.", LegacyAskRequiredDevice.Commutator),
    new(LegacyAskTestKind.AdditionalService, "EKRGVM", "ЭК групповым методом", "Проверка подключения точек к ЭК групповым методом.", LegacyAskRequiredDevice.Commutator),
    new(LegacyAskTestKind.AdditionalService, "PORTS", "Порты и регистры", "Проверка записи и чтения регистров контроллера.", LegacyAskRequiredDevice.Controller),
    new(LegacyAskTestKind.AdditionalService, "PRKOMG", "Пробой коммутатора, группа", "Проверка прочности изоляции коммутатора групповым методом.", LegacyAskRequiredDevice.Ppu),
    new(LegacyAskTestKind.AdditionalService, "PRKOMU", "Пробой коммутатора, узел", "Проверка прочности изоляции коммутатора узловым методом.", LegacyAskRequiredDevice.Ppu),
    new(LegacyAskTestKind.AdditionalService, "RK1TM", "Подключение точек к РК", "Проверка подключения точек к релейному коммутатору.", LegacyAskRequiredDevice.Commutator),
    new(LegacyAskTestKind.AdditionalService, "RKRGVM", "РК групповым методом", "Проверка подключения точек к РК групповым методом.", LegacyAskRequiredDevice.Commutator),
    new(LegacyAskTestKind.AdditionalService, "SIKOMG", "Сопротивление изоляции, группа", "Проверка сопротивления изоляции коммутатора групповым методом.", LegacyAskRequiredDevice.Pki),
    new(LegacyAskTestKind.AdditionalService, "SIKOMU", "Сопротивление изоляции, узел", "Проверка сопротивления изоляции коммутатора узловым методом.", LegacyAskRequiredDevice.Pki),
    new(LegacyAskTestKind.AdditionalService, "TSTNLN", "Нелинейные элементы", "Контроль нелинейных элементов.", LegacyAskRequiredDevice.Commutator),
    new(LegacyAskTestKind.AdditionalService, "TSTROS", "Цепь непрерывного контроля", "Проверка цепи непрерывного контроля.", LegacyAskRequiredDevice.Commutator),
    new(LegacyAskTestKind.AdditionalService, "STATICA", "Снятие статики", "Снятие статического заряда со входов коммутатора.", LegacyAskRequiredDevice.Commutator),
    new(LegacyAskTestKind.AdditionalService, "KLBIZMR", "Калибровка измерения сопротивления", "Калибровка измерения сопротивления.", LegacyAskRequiredDevice.Voltmeter),
    new(LegacyAskTestKind.AdditionalService, "RKOMM", "Сопротивление контактов реле", "Контроль сопротивления контактов реле коммутатора.", LegacyAskRequiredDevice.Voltmeter),
    new(LegacyAskTestKind.AdditionalService, "CROSS", "Перекрестный тест коммутатора", "Перекрестная проверка коммутатора.", LegacyAskRequiredDevice.Commutator),
    new(LegacyAskTestKind.AdditionalService, "SYSDEV", "Тесты системных устройств", "Проверка системных устройств старой АСК.", LegacyAskRequiredDevice.Controller)
  ];

  private static readonly IReadOnlyList<LegacyAskTestDescriptor> RelayTrainingTests =
  [
    new(LegacyAskTestKind.RelayTraining, "TRENKR", "Тренировка контактов реле током", "Тренировка контактов реле под токовой нагрузкой.", LegacyAskRequiredDevice.Pint4),
    new(LegacyAskTestKind.RelayTraining, "TRENRSH", "Тренировка реле шин током", "Тренировка реле шин под токовой нагрузкой.", LegacyAskRequiredDevice.Pint4),
    new(LegacyAskTestKind.RelayTraining, "TSTGRR", "Тренировка групповых реле током", "Тренировка групповых реле под токовой нагрузкой.", LegacyAskRequiredDevice.Pint4)
  ];

  private static readonly IReadOnlyList<LegacyAskTestDescriptor> SwitchingTimeTests =
  [
    new(LegacyAskTestKind.SwitchingTime, "TIM_RK_POINT", "Время подключения точки к РК", "Измерение времени подключения точки к шинам РК.", LegacyAskRequiredDevice.Timer),
    new(LegacyAskTestKind.SwitchingTime, "TIM_EK_POINT", "Время подключения точки к ЭК", "Измерение времени подключения точки к шинам ЭК.", LegacyAskRequiredDevice.Timer),
    new(LegacyAskTestKind.SwitchingTime, "TIM_BK_BUS", "Время коммутации БК к ИШ", "Измерение времени коммутации БК к измерительным шинам.", LegacyAskRequiredDevice.Timer),
    new(LegacyAskTestKind.SwitchingTime, "TIM_GROUP_RELAY", "Время групповых реле", "Измерение времени коммутации групповых реле.", LegacyAskRequiredDevice.Timer),
    new(LegacyAskTestKind.SwitchingTime, "TIM_KEP", "Время КЭП", "Измерение времени коммутации КЭП.", LegacyAskRequiredDevice.Timer),
    new(LegacyAskTestKind.SwitchingTime, "TIM_KZSH", "Время КЗШ", "Измерение времени коммутации реле КЗШ.", LegacyAskRequiredDevice.Timer),
    new(LegacyAskTestKind.SwitchingTime, "TIM_PINT4", "Время ПИНТ4", "Измерение времени коммутации ПИНТ4 к шинам.", LegacyAskRequiredDevice.Timer),
    new(LegacyAskTestKind.SwitchingTime, "TIM_V7", "Время цифрового вольтметра", "Измерение времени коммутации вольтметра к шинам.", LegacyAskRequiredDevice.Timer),
    new(LegacyAskTestKind.SwitchingTime, "TIM_ADC", "Время АЦП", "Измерение времени коммутации АЦП к шинам.", LegacyAskRequiredDevice.Timer),
    new(LegacyAskTestKind.SwitchingTime, "TIM_PINT_MODE", "Время установки режима ПИНТ", "Измерение времени установки режима ПИНТ.", LegacyAskRequiredDevice.Timer)
  ];

  /// <summary>
  /// Возвращает список тестов для указанной группы меню старой АСК.
  /// </summary>
  /// <param name="kind">Группа тестов.</param>
  /// <returns>Список тестов группы.</returns>
  public static IReadOnlyList<LegacyAskTestDescriptor> GetTests(LegacyAskTestKind kind)
  {
    return kind switch
    {
      LegacyAskTestKind.MeasurementAccuracy => MeasurementAccuracyTests,
      LegacyAskTestKind.AdditionalService => AdditionalServiceTests,
      LegacyAskTestKind.RelayTraining => RelayTrainingTests,
      LegacyAskTestKind.SwitchingTime => SwitchingTimeTests,
      _ => []
    };
  }
}
